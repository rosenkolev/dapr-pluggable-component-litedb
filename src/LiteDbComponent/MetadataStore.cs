using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Logging;

namespace LiteDbComponent;

public sealed class MetadataStore
{
    public const string DataDirectoryPath = "/.db";
    public static readonly string MetadataFilePath =
        Path.Combine(DataDirectoryPath, "metadata.json");

    private readonly ILogger<MetadataStore> _logger;
    private readonly ConcurrentDictionary<string, Metadata> _store = new();

    public MetadataStore(ILogger<MetadataStore> logger)
    {
        _logger = logger;

        Directory.CreateDirectory(DataDirectoryPath);
        if (File.Exists(MetadataFilePath))
        {
            ReadStore();
        }
    }

    public void Set(string key, IReadOnlyDictionary<string, string> properties)
    {
        var metadata = new Metadata(
            ConnectionString(properties.GetValueOrDefault("databaseName", "state")),
            properties.GetValueOrDefault("collectionName", "default"),
            properties.GetValueOrDefault("indexes", string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries));

        _logger.LogDebug($"{key}: set metadata.");
        _store.TryAdd(key, metadata);
        SaveStore();
    }

    public Metadata Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key, "The state name is null");
        if (!_store.ContainsKey(key))
        {
            _logger.LogError(
                $"The state '{key}' was never initialized. This can happen if the container is start after Dapr was un and running.");

            throw new InvalidOperationException($"Metadata '{key}' not found.");
        }

        return _store[key];
    }

    private static string ConnectionString(string databaseName) =>
        $"Filename={Path.Combine(DataDirectoryPath, databaseName + ".db")}; Connection=Direct;";

    private void SaveStore()
    {
        _logger.LogDebug($"Update config {MetadataFilePath}.");
        File.WriteAllText(
            MetadataFilePath,
            JsonSerializer.Serialize(_store.ToDictionary(x => x.Key, x => x.Value)),
            Encoding.UTF8);
    }

    private void ReadStore()
    {
        _logger.LogDebug($"Read config {MetadataFilePath}.");
        var json = File.ReadAllText(MetadataFilePath, Encoding.UTF8);
        var data = JsonSerializer.Deserialize<Dictionary<string, Metadata>>(json) ?? [];
        foreach (var item in data)
        {
            _store.TryAdd(item.Key, item.Value);
        }

        _logger.LogDebug($"{data.Count} metadata read.");
    }

    public sealed record Metadata(
        string ConnectionString,
        string CollectionName,
        string[] Indexes);
}
