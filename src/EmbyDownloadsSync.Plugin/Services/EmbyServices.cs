using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbyDownloadsSync.Core.Domain;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Serialization;

namespace EmbyDownloadsSync.Plugin.Services;

public sealed class DeviceDescriptor
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTime? LastActivityUtc { get; set; }
}

internal interface IDeviceCatalog
{
    Task<IList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken);
}

internal sealed class EmbyDeviceCatalog : IDeviceCatalog
{
    private readonly IDeviceManager deviceManager;

    public EmbyDeviceCatalog(IDeviceManager deviceManager) => this.deviceManager = deviceManager;

    public Task<IList<DeviceDescriptor>> GetDevicesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = deviceManager.GetDevices(new DeviceQuery { SupportsSync = true });
        IList<DeviceDescriptor> devices = (result.Items ?? Array.Empty<DeviceInfo>())
            .Where(device => !string.IsNullOrWhiteSpace(device.ReportedDeviceId))
            .Select(device => new DeviceDescriptor
            {
                Id = device.ReportedDeviceId,
                Name = device.Name ?? device.ReportedDeviceId,
                LastActivityUtc = device.DateLastActivity.UtcDateTime,
            })
            .ToList();
        return Task.FromResult(devices);
    }
}

internal interface ISyncJobGateway : IDisposable
{
    Task<IList<DownloadJob>> GetJobsAsync(string apiKey, CancellationToken cancellationToken);

    Task<long?> CreateJobAsync(string apiKey, DownloadJob job, CancellationToken cancellationToken);
}

internal sealed class EmbySyncJobGateway : ISyncJobGateway
{
    private readonly Func<CancellationToken, Task<string>> localApiUrlProvider;
    private readonly IJsonSerializer serializer;
    private readonly HttpClient httpClient;
    private readonly Func<TimeSpan> timeoutProvider;

    public EmbySyncJobGateway(
        IServerApplicationHost applicationHost,
        IJsonSerializer serializer,
        Func<TimeSpan> timeoutProvider,
        HttpMessageHandler? handler = null)
        : this(applicationHost.GetLocalApiUrl, serializer, timeoutProvider, handler)
    {
    }

    internal EmbySyncJobGateway(
        Func<CancellationToken, Task<string>> localApiUrlProvider,
        IJsonSerializer serializer,
        Func<TimeSpan> timeoutProvider,
        HttpMessageHandler? handler = null)
    {
        this.localApiUrlProvider = localApiUrlProvider;
        this.serializer = serializer;
        this.timeoutProvider = timeoutProvider;
        httpClient = handler == null ? new HttpClient() : new HttpClient(handler, false);
        httpClient.Timeout = Timeout.InfiniteTimeSpan;
    }

    public async Task<IList<DownloadJob>> GetJobsAsync(string apiKey, CancellationToken cancellationToken)
    {
        using var response = await SendWithRetryAsync(HttpMethod.Get, apiKey, null, cancellationToken).ConfigureAwait(false);
        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        EnsureSuccess(response, json);
        var result = serializer.DeserializeFromString<SyncJobQueryPayload>(json) ?? new SyncJobQueryPayload();
        return (result.Items ?? new List<SyncJobPayload>()).Select(SyncJobMapper.ToDomain).ToList();
    }

    public async Task<long?> CreateJobAsync(string apiKey, DownloadJob job, CancellationToken cancellationToken)
    {
        var requestBody = SyncJobMapper.ToRequest(job);
        var json = serializer.SerializeToString(requestBody);
        using var response = await SendWithRetryAsync(HttpMethod.Post, apiKey, json, cancellationToken).ConfigureAwait(false);
        var responseJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        EnsureSuccess(response, responseJson);
        return serializer.DeserializeFromString<SyncJobCreationPayload>(responseJson)?.Job?.Id;
    }

    public void Dispose() => httpClient.Dispose();

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpMethod method, string apiKey, string? json, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(apiKey)) throw new InvalidOperationException("An Emby administrator API key is required.");
        var url = (await localApiUrlProvider(cancellationToken).ConfigureAwait(false)).TrimEnd('/') + "/Sync/Jobs";
        var maximumAttempts = MaximumAttempts(method);
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(method, url);
            request.Headers.TryAddWithoutValidation("X-Emby-Token", apiKey);
            request.Headers.TryAddWithoutValidation("X-Emby-Authorization", "MediaBrowser Client=\"Emby Downloads Sync\", Device=\"Emby Server\", DeviceId=\"emby-downloads-sync\", Version=\"1.0\"");
            if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(GetTimeout());
            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, timeout.Token).ConfigureAwait(false);
            }
            catch (HttpRequestException) when (attempt < maximumAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested && attempt < maximumAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken).ConfigureAwait(false);
                continue;
            }
            if (attempt >= maximumAttempts || !IsTransient(response.StatusCode)) return response;
            response.Dispose();
            await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken).ConfigureAwait(false);
        }
    }

    internal static int MaximumAttempts(HttpMethod method) => method == HttpMethod.Get ? 3 : 1;

    private TimeSpan GetTimeout()
    {
        var value = timeoutProvider();
        return value > TimeSpan.Zero ? value : TimeSpan.FromSeconds(30);
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode == HttpStatusCode.RequestTimeout || (int)statusCode == 429 || (int)statusCode >= 500;

    private static void EnsureSuccess(HttpResponseMessage response, string responseBody)
    {
        if (response.IsSuccessStatusCode) return;
        var safeBody = responseBody.Length > 500 ? responseBody.Substring(0, 500) : responseBody;
        throw new InvalidOperationException($"Emby sync API returned {(int)response.StatusCode} {response.ReasonPhrase}: {safeBody}");
    }

}
