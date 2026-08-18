using System;
using System.IO;
using MediaBrowser.Model.Serialization;

namespace EmbyDownloadsSync.Plugin.Configuration;

internal sealed class JsonFileStore<T> where T : class, new()
{
    private readonly object sync = new object();
    private readonly IJsonSerializer serializer;
    private readonly string filePath;
    private T? cached;

    public JsonFileStore(IJsonSerializer serializer, string filePath)
    {
        this.serializer = serializer;
        this.filePath = filePath;
    }

    public T Get()
    {
        lock (sync)
        {
            if (cached != null) return cached;
            if (!File.Exists(filePath)) return cached = new T();
            using var stream = File.OpenRead(filePath);
            return cached = serializer.DeserializeFromStream<T>(stream) ?? new T();
        }
    }

    public void Set(T value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        lock (sync)
        {
            var directory = Path.GetDirectoryName(filePath)!;
            Directory.CreateDirectory(directory);
            var temporaryPath = filePath + ".tmp";
            using (var stream = File.Create(temporaryPath))
                serializer.SerializeToStream(value, stream, new JsonSerializerOptions { Indent = true });
            if (File.Exists(filePath)) File.Replace(temporaryPath, filePath, null);
            else File.Move(temporaryPath, filePath);
            cached = value;
        }
    }
}
