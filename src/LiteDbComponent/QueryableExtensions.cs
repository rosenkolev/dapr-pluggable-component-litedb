using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

using Dapr.PluggableComponents.Components.StateStore;

using DaprLiteDbComponent;

using Google.Protobuf.WellKnownTypes;

using LiteDB;

namespace LiteDbComponent;

public static class QueryableExtensions
{
    internal static ILiteQueryable<BsonDocument> Filter(
        this ILiteQueryable<BsonDocument> queryable,
        IReadOnlyDictionary<string, Any> filter)
    {
        if (filter != null && filter.Count > 0)
        {
            var kv = filter.First();
            var exp = QueryExpression(kv.Key, kv.Value);
            queryable = queryable.Where(exp);
        }

        return queryable;
    }

    internal static ILiteQueryable<BsonDocument> Sort(
        this ILiteQueryable<BsonDocument> queryable,
        StateStoreQuerySorting[] sorting)
    {
        if (sorting != null)
        {
            foreach (var sort in sorting)
            {
                queryable = queryable.OrderBy(
                    $"$.{sort.Key}",
                    sort.Order == StateStoreQuerySortingOrder.Ascending ? 1 : -1);
            }
        }

        return queryable;
    }

    internal static IEnumerable<BsonDocument> Pagination(
        this ILiteQueryable<BsonDocument> queryable,
        StateStoreQueryPagination? pagination,
        out string? token)
    {
        BsonDocument[] data;
        if (pagination != null)
        {
            var startAt = string.IsNullOrEmpty(pagination.Token) ? 0 : int.Parse(pagination.Token);
            var limit = pagination.Limit <= 0 ? 10000 : (int)pagination.Limit;
            data = queryable.Skip(startAt).Limit(limit).ToArray();
            token = data.Length == limit
                ? (startAt + limit).ToString()
                : null;
        }
        else
        {
            data = queryable.ToArray();
            token = null;
        }

        return data;
    }

    internal static BsonExpression QueryExpression(string key, Any value) =>
        key switch
        {
            "AND" => Query.And(Helper.ListToSingleProtoBufValue(value, QueryExpression)),
            "OR" => Query.Or(Helper.ListToSingleProtoBufValue(value, QueryExpression)),
            _ => ExpandProtoAnyAsKeyValuePair(key, value, out var prop) switch
            {
                "EQ" => Query.EQ(prop.Key, prop.Value),
                "NEQ" => Query.Not(prop.Key, prop.Value),
                "GT" => Query.GT(key, prop.Value),
                "GTE" => Query.GTE(key, prop.Value),
                "LT" => Query.LT(key, prop.Value),
                "LTE" => Query.LTE(key, prop.Value),
                "IN" => Query.In(key, prop.Value as BsonArray),
                _ => throw new InvalidOperationException("Invalid filter operator")
            },
        };

    internal static string ExpandProtoAnyAsKeyValuePair(
        string key,
        Any @any,
        out KeyValuePair<string, BsonValue> prop)
    {
        prop = ProtoAnyToKeyValuePair(@any);
        return key;
    }

    internal static KeyValuePair<string, BsonValue> ProtoAnyToKeyValuePair(Any @any)
    {
        var json = Encoding.UTF8.GetString(any.Value.Span);
        var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        return data?.ToDictionary(it => it.Key, it => JsonElementToBsonValue(it.Value)).First()
            ?? throw new InvalidOperationException("The filter json is invalid!");
    }

    internal static BsonValue JsonElementToBsonValue(JsonElement value) =>
        value.ValueKind switch
        {
            JsonValueKind.String => new BsonValue(value.GetString()),
            JsonValueKind.Number => new BsonValue(value.GetDouble()),
            JsonValueKind.True => new BsonValue(true),
            JsonValueKind.False => new BsonValue(false),
            JsonValueKind.Null => BsonValue.Null,
            JsonValueKind.Undefined => BsonValue.Null,
            JsonValueKind.Array => new BsonArray(
                value.EnumerateArray().Select(JsonElementToBsonValue)),
            _ => throw new InvalidOperationException("Invalid value type")
        };
}
