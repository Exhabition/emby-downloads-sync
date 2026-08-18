using System.Net;
using System.Text;
using System.Text.Json;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Services;
using MediaBrowser.Model.Serialization;
using EmbyJsonOptions = MediaBrowser.Model.Serialization.JsonSerializerOptions;

namespace EmbyDownloadsSync.Tests;

public sealed class SyncJobGatewayTests
{
    [Fact]
    public async Task GetRetriesTransientResponsesAndReadsTimeoutForEveryAttempt()
    {
        var requests = 0;
        var timeoutReads = 0;
        using var gateway = Gateway(new DelegateHandler(_ =>
        {
            requests++;
            return requests < 3
                ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                : JsonResponse("{\"Items\":[]}");
        }), () =>
        {
            timeoutReads++;
            return TimeSpan.FromSeconds(5);
        });

        var jobs = await gateway.GetJobsAsync("key", CancellationToken.None);

        Assert.Empty(jobs);
        Assert.Equal(3, requests);
        Assert.Equal(3, timeoutReads);
    }

    [Fact]
    public async Task CreateNeverRetriesAnAmbiguousFailure()
    {
        var requests = 0;
        using var gateway = Gateway(new DelegateHandler(_ =>
        {
            requests++;
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        }));

        await Assert.ThrowsAsync<InvalidOperationException>(() => gateway.CreateJobAsync("key", new DownloadJob
        {
            TargetId = "tablet",
            Name = "Movie",
            RequestedItemIds = [1],
        }, CancellationToken.None));

        Assert.Equal(1, requests);
    }

    private static EmbySyncJobGateway Gateway(HttpMessageHandler handler, Func<TimeSpan>? timeout = null) => new EmbySyncJobGateway(
        _ => Task.FromResult("http://localhost:8096"),
        new TestJsonSerializer(),
        timeout ?? (() => TimeSpan.FromSeconds(5)),
        handler);

    private static HttpResponseMessage JsonResponse(string json) => new HttpResponseMessage(HttpStatusCode.OK)
    {
        Content = new StringContent(json, Encoding.UTF8, "application/json"),
    };

    private sealed class DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(send(request));
    }

    internal sealed class TestJsonSerializer : IJsonSerializer
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public void SerializeToStream(object obj, Stream stream) => JsonSerializer.Serialize(stream, obj, obj.GetType(), Options);

        public void SerializeToStream(object obj, Stream stream, EmbyJsonOptions options) => SerializeToStream(obj, stream);

        public void SerializeToFile(object obj, string file)
        {
            using var stream = File.Create(file);
            SerializeToStream(obj, stream);
        }

        public void SerializeToFile(object obj, string file, EmbyJsonOptions options) => SerializeToFile(obj, file);

        public async Task<object> DeserializeFromFileAsync(Type type, string file)
        {
            await using var stream = File.OpenRead(file);
            return (await JsonSerializer.DeserializeAsync(stream, type, Options))!;
        }

        public async Task<T> DeserializeFromFileAsync<T>(string file) where T : class
        {
            await using var stream = File.OpenRead(file);
            return (await JsonSerializer.DeserializeAsync<T>(stream, Options))!;
        }

        public T DeserializeFromFile<T>(string file) where T : class
        {
            using var stream = File.OpenRead(file);
            return DeserializeFromStream<T>(stream);
        }

        public async Task<T> DeserializeFromStreamAsync<T>(Stream stream) =>
            (await JsonSerializer.DeserializeAsync<T>(stream, Options))!;

        public async Task<object> DeserializeFromStreamAsync(Stream stream, Type type) =>
            (await JsonSerializer.DeserializeAsync(stream, type, Options))!;

        public T DeserializeFromStream<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, Options)!;

        public T DeserializeFromString<T>(string text) => JsonSerializer.Deserialize<T>(text, Options)!;

        public object DeserializeFromStream(Stream stream, Type type) => JsonSerializer.Deserialize(stream, type, Options)!;

        public object DeserializeFromString(string json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;

        public string SerializeToString(object obj) => JsonSerializer.Serialize(obj, obj.GetType(), Options);

        public string SerializeToString(object obj, EmbyJsonOptions options) => SerializeToString(obj);

        public ReadOnlySpan<char> SerializeToSpan(object obj) => SerializeToString(obj).AsSpan();

        public T DeserializeFromSpan<T>(ReadOnlySpan<char> json) => JsonSerializer.Deserialize<T>(json, Options)!;

        public object DeserializeFromSpan(ReadOnlySpan<char> json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;

        public T DeserializeFromBytes<T>(ReadOnlySpan<byte> json) => JsonSerializer.Deserialize<T>(json, Options)!;

        public object DeserializeFromBytes(ReadOnlySpan<byte> json, Type type) => JsonSerializer.Deserialize(json, type, Options)!;

        public void DeserializePartialJsonInto(string json, object target) => throw new NotSupportedException();
    }
}
