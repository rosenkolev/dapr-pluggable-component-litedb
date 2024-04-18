using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dapr.PluggableComponents.Components;
using Dapr.PluggableComponents.Components.StateStore;

using LiteDB;

using LiteDbComponent;

using Microsoft.Extensions.Logging;

namespace DaprLiteDbComponent;

internal sealed class LiteDbStateStore(
    string id,
    ILogger logger,
    MetadataStore store)
    : IStateStore
    , IQueryableStateStore
    , ITransactionalStateStore
    , IBulkStateStore
{
    private ILiteCollection<BsonDocument> _collection = null!;
    private static readonly ConcurrentDictionary<string, ILiteDatabase> _dbs = new();

    public Task InitAsync(MetadataRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogDebug($"{id}: init");
        store.Set(id, request.Properties);
        Connect();
        return Task.CompletedTask;
    }

    public Task DeleteAsync(StateStoreDeleteRequest request, CancellationToken cancellationToken = default) =>
        RunConnectedSyncAction(() => Delete(request.Key));

    public Task<StateStoreGetResponse?> GetAsync(StateStoreGetRequest request, CancellationToken cancellationToken = default) =>
        RunConnectedSyncFunc<StateStoreGetResponse?>(() =>
        {
            var body = Get(request.Key);
            return body is null
                ? null
                : new() { ContentType = MediaTypeNames.Application.Json, Data = body };
        });

    public Task SetAsync(StateStoreSetRequest request, CancellationToken cancellationToken = default) =>
        RunConnectedSyncAction(() => Set(request.Key, request.Value));

    public Task TransactAsync(
        StateStoreTransactRequest request,
        CancellationToken cancellationToken = default) =>
        RunConnectedSyncAction(() =>
            Array.ForEach(
                request.Operations,
                op => op.Visit(
                    del => Delete(del.Key),
                    set => Set(set.Key, set.Value))));

    public Task BulkDeleteAsync(StateStoreDeleteRequest[] requests, CancellationToken cancellationToken = default) =>
        RunConnectedSyncAction(() =>
            Array.ForEach(requests, r => Delete(r.Key)));

    public Task<StateStoreBulkStateItem[]> BulkGetAsync(StateStoreGetRequest[] requests, CancellationToken cancellationToken = default) =>
        RunConnectedSyncFunc<StateStoreBulkStateItem[]>(() => requests
            .Select<StateStoreGetRequest, StateStoreBulkStateItem>(it =>
            {
                var data = Get(it.Key);
                return data is null
                    ? new (it.Key) { Error = "Not Found", ContentType = MediaTypeNames.Application.Json }
                    : new (it.Key) { Data = data, ContentType = MediaTypeNames.Application.Json};
            })
            .ToArray());

    public Task BulkSetAsync(StateStoreSetRequest[] requests, CancellationToken cancellationToken = default)
    {
        logger.LogDebug($"{id}: bulk set {requests.Length}");
        return RunConnectedSyncAction(() =>
          Array.ForEach(requests, r => Set(r.Key, r.Value)));
    }

    public Task<StateStoreQueryResponse> QueryAsync(StateStoreQueryRequest request, CancellationToken cancellationToken = default) =>
        RunConnectedSyncFunc<StateStoreQueryResponse>(() =>
        {
            var data = request.Query == null
                ? _collection.FindAll()
                : _collection.Query()
                    .Filter(request.Query.Filter)
                    .Sort(request.Query.Sorting)
                    .Pagination(request.Query.Pagination, out var token);

            return new StateStoreQueryResponse
            {
                Items = data.Select(x =>
                    new StateStoreQueryItem(x["_id"].AsString)
                    {
                        ContentType = MediaTypeNames.Application.Json,
                        Data = Helper.SerializeBsonDocument(x),
                    }).ToArray()
            };
        });

    internal void Connect()
    {
        if (_collection is null)
        {
            var data = store.Get(id);
            InitCollection(data.ConnectionString, data.CollectionName);
            InitCollectionIndexes(data.Indexes);
        }
    }

    internal void Set(string key, ReadOnlyMemory<byte> body)
    {
        logger.LogDebug($"{id}: set key '{key}' in '{_collection.Name}'");
        _collection.Upsert(key, Helper.ReadJsonAsBsonDocument(body.Span));
    }

    internal void Delete(string key) =>
        _collection.Delete(key);

    internal byte[]? Get(string key)
    {
        var item = _collection.FindById(key);
        return item is null ? null : Helper.SerializeBsonDocument(item);
    }

    private void InitCollection(string connectionString, string collectionName)
    {
        var db = _dbs.GetOrAdd(connectionString, conn => new LiteDatabase(conn));
        _collection = db.GetCollection(collectionName);
        logger.LogDebug($"{id}: collection '{collectionName}' initialized at '{connectionString}'.");
    }

    private void InitCollectionIndexes(string[] indexes)
    {
        foreach (var index in indexes)
        {
            if (_collection.EnsureIndex(index))
            {
                logger.LogDebug($"Index initialized '{index}'");
            }
        }
    }

    private Task RunConnectedSyncAction(Action action)
    {
        Connect();
        action();
        return Task.CompletedTask;
    }

    private Task<TResult> RunConnectedSyncFunc<TResult>(Func<TResult> action)
    {
        Connect();
        return Task.FromResult<TResult>(action());
    }
}
