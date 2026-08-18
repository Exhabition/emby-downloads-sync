using System.Collections.Generic;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Persistence;
using EmbyDownloadsSync.Plugin.Services;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Services;

namespace EmbyDownloadsSync.Plugin.Api;

[Route("/EmbyDownloadsSync/Routes", "GET", Summary = "Lists configured download synchronization routes")]
[Authenticated(Roles = "Admin")]
public sealed class GetSyncRoutes : IReturn<RouteCollection>
{
}

[Route("/EmbyDownloadsSync/Routes", "POST", Summary = "Creates or updates a synchronization route")]
[Authenticated(Roles = "Admin")]
public sealed class SaveSyncRoute : IReturn<SyncRoute>
{
    public SyncRoute Route { get; set; } = new SyncRoute();
}

[Route("/EmbyDownloadsSync/Routes/{RouteId}", "DELETE", Summary = "Deletes a synchronization route")]
[Authenticated(Roles = "Admin")]
public sealed class DeleteSyncRoute : IReturn<SyncOperationResponse>
{
    public string RouteId { get; set; } = string.Empty;
}

[Route("/EmbyDownloadsSync/Devices", "GET", Summary = "Lists Emby devices that support synchronization")]
[Authenticated(Roles = "Admin")]
public sealed class GetSyncDevices : IReturn<List<DeviceDescriptor>>
{
}

[Route("/EmbyDownloadsSync/Runs", "GET", Summary = "Gets recent synchronization runs")]
[Authenticated(Roles = "Admin")]
public sealed class GetSyncRuns : IReturn<List<SyncRunSummary>>
{
}

[Route("/EmbyDownloadsSync/Preview", "POST", Summary = "Builds a synchronization plan without applying it")]
[Authenticated(Roles = "Admin")]
public sealed class PreviewSynchronization : IReturn<SyncRunSummary>
{
    public string? RouteId { get; set; }
}

[Route("/EmbyDownloadsSync/Runs", "POST", Summary = "Runs synchronization immediately")]
[Authenticated(Roles = "Admin")]
public sealed class RunSynchronization : IReturn<SyncRunSummary>
{
    public string? RouteId { get; set; }
}

public sealed class SyncOperationResponse
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;
}
