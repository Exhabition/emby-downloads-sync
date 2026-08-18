using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Services;

namespace EmbyDownloadsSync.Tests;

public sealed class SynchronizationPolicyTests
{
    [Fact]
    public void ReadRequestsRetryButCreateRequestsAreSingleAttempt()
    {
        Assert.Equal(3, EmbySyncJobGateway.MaximumAttempts(HttpMethod.Get));
        Assert.Equal(1, EmbySyncJobGateway.MaximumAttempts(HttpMethod.Post));
    }

    [Fact]
    public void ManualPreviewsDoNotAdvanceRouteSchedules()
    {
        var routes = new[] { Route("one", false), Route("two", true) };

        Assert.Empty(SynchronizationService.RouteIdsToAdvance(true, routes));
        Assert.Equal(["one", "two"], SynchronizationService.RouteIdsToAdvance(false, routes));
    }

    [Fact]
    public void AllRouteDryRunsAreReportedAsPreview()
    {
        Assert.True(SynchronizationService.IsEffectivePreview(false, false, [Route("one", true)]));
        Assert.False(SynchronizationService.IsEffectivePreview(false, false, [Route("one", false), Route("two", true)]));
    }

    private static SyncRoute Route(string id, bool dryRun) => new SyncRoute
    {
        Id = id,
        Safety = new RouteSafety { DryRun = dryRun },
    };
}
