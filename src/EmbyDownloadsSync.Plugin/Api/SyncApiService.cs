using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Core.Planning;
using EmbyDownloadsSync.Plugin.Persistence;
using EmbyDownloadsSync.Plugin.Services;
using MediaBrowser.Model.Services;

namespace EmbyDownloadsSync.Plugin.Api;

public sealed class SyncApiService : IService
{
    private readonly PluginRuntime runtime = Plugin.Instance.Runtime;

    public RouteCollection Get(GetSyncRoutes request) => new RouteCollection { Routes = runtime.Repository.GetRoutes() };

    public SyncRoute Post(SaveSyncRoute request)
    {
        if (request.Route == null) throw new ArgumentNullException(nameof(request.Route));
        var errors = new RouteValidator().Validate(request.Route);
        if (errors.Count > 0) throw new ArgumentException(string.Join(" ", errors));
        runtime.Repository.SaveRoute(request.Route);
        return request.Route;
    }

    public SyncOperationResponse Delete(DeleteSyncRoute request)
    {
        var removed = runtime.Repository.DeleteRoute(request.RouteId);
        return new SyncOperationResponse { Success = removed, Message = removed ? "Route deleted." : "Route not found." };
    }

    public async Task<List<DeviceDescriptor>> Get(GetSyncDevices request) =>
        (await runtime.DeviceCatalog.GetDevicesAsync(CancellationToken.None).ConfigureAwait(false)).ToList();

    public List<SyncRunSummary> Get(GetSyncRuns request) => runtime.Repository.GetState().Runs.ToList();

    public Task<SyncRunSummary> Post(PreviewSynchronization request) =>
        runtime.Synchronizer.RunAsync(true, true, request.RouteId, null, CancellationToken.None);

    public Task<SyncRunSummary> Post(RunSynchronization request) =>
        runtime.Synchronizer.RunAsync(false, true, request.RouteId, null, CancellationToken.None);
}
