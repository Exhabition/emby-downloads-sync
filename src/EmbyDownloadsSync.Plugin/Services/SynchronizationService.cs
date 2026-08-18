using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Core.Planning;
using EmbyDownloadsSync.Plugin.Configuration;
using EmbyDownloadsSync.Plugin.Persistence;
using MediaBrowser.Model.Logging;

namespace EmbyDownloadsSync.Plugin.Services;

internal sealed class SynchronizationService : IDisposable
{
    private readonly SemaphoreSlim executionLock = new SemaphoreSlim(1, 1);
    private readonly PluginOptionsStore optionsStore;
    private readonly PluginRepository repository;
    private readonly IDeviceCatalog deviceCatalog;
    private readonly ISyncJobGateway gateway;
    private readonly ILogger logger;
    private bool disposed;

    public SynchronizationService(
        PluginOptionsStore optionsStore,
        PluginRepository repository,
        IDeviceCatalog deviceCatalog,
        ISyncJobGateway gateway,
        ILogger logger)
    {
        this.optionsStore = optionsStore;
        this.repository = repository;
        this.deviceCatalog = deviceCatalog;
        this.gateway = gateway;
        this.logger = logger;
    }

    public async Task<SyncRunSummary> RunAsync(bool preview, bool force, string? routeId, IProgress<double>? progress, CancellationToken cancellationToken)
    {
        if (!await executionLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            throw new InvalidOperationException("A download synchronization run is already active.");
        var started = DateTime.UtcNow;
        IList<SyncRoute> routes = new List<SyncRoute>();
        var recorded = false;
        try
        {
            var options = optionsStore.Get();
            if (!force && !options.Enabled) throw new InvalidOperationException("Scheduled synchronization is disabled.");
            routes = SelectRoutes(repository.GetRoutes(), repository.GetState(), routeId, force, started);
            if (!string.IsNullOrWhiteSpace(routeId) && routes.Count == 0)
                throw new InvalidOperationException($"Synchronization route '{routeId}' was not found.");
            if (routes.Count == 0)
            {
                var noOp = new SyncRunSummary
                {
                    StartedUtc = started,
                    FinishedUtc = DateTime.UtcNow,
                    Preview = preview || options.DryRun,
                    Success = true,
                    Messages = new List<string> { "No synchronization routes are due." },
                };
                repository.RecordRun(noOp, Array.Empty<string>(), options.RetainedRunCount);
                recorded = true;
                progress?.Report(100);
                return noOp;
            }
            var devices = await deviceCatalog.GetDevicesAsync(cancellationToken).ConfigureAwait(false);
            progress?.Report(10);
            var jobs = await gateway.GetJobsAsync(options.ApiKey, cancellationToken).ConfigureAwait(false);
            progress?.Report(25);
            var plan = new SyncPlanner().Build(new PlanningSnapshot
            {
                CapturedUtc = started,
                KnownDeviceIds = devices.Select(device => device.Id).ToList(),
                Jobs = jobs,
            }, routes);
            var run = new SyncRunSummary
            {
                StartedUtc = started,
                Preview = IsEffectivePreview(preview, options.DryRun, routes),
                PlannedCreates = plan.CreateCount,
                Skipped = plan.SkippedCount,
                Conflicts = plan.ConflictCount,
                Plan = plan,
            };
            foreach (var warning in plan.Warnings) run.Messages.Add(warning);
            foreach (var dryRoute in routes.Where(route => route.Safety.DryRun))
                run.Messages.Add($"{dryRoute.Name}: route dry-run mode prevented job creation.");
            if (!preview && !options.DryRun)
                await ApplyAsync(run, plan, routes, options.ApiKey, progress, cancellationToken).ConfigureAwait(false);
            run.FinishedUtc = DateTime.UtcNow;
            run.Success = run.Failed == 0 && plan.Warnings.Count == 0;
            repository.RecordRun(
                run,
                RouteIdsToAdvance(preview, routes),
                options.RetainedRunCount);
            recorded = true;
            progress?.Report(100);
            return run;
        }
        catch (Exception exception) when (!recorded)
        {
            try
            {
                repository.RecordRun(new SyncRunSummary
                {
                    StartedUtc = started,
                    FinishedUtc = DateTime.UtcNow,
                    Preview = preview,
                    Success = false,
                    Failed = 1,
                    Messages = new List<string> { exception.Message },
                }, RouteIdsToAdvance(preview, routes), optionsStore.Get().RetainedRunCount);
            }
            catch (Exception persistenceException)
            {
                logger.ErrorException("Unable to record failed download synchronization run", persistenceException);
            }

            throw;
        }
        finally
        {
            executionLock.Release();
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        gateway.Dispose();
        executionLock.Dispose();
        disposed = true;
    }

    private async Task ApplyAsync(
        SyncRunSummary run,
        SyncPlan plan,
        IList<SyncRoute> routes,
        string apiKey,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        var creates = plan.Actions.Where(action => action.Type == PlanActionType.Create && action.Job != null).ToList();
        for (var index = 0; index < creates.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var action = creates[index];
            var route = routes.First(value => value.Id == action.RouteId);
            if (route.Safety.DryRun)
            {
                run.DryRunSkipped++;
                progress?.Report(25 + (75d * (index + 1) / Math.Max(creates.Count, 1)));
                continue;
            }
            try
            {
                var jobId = await gateway.CreateJobAsync(apiKey, action.Job!, cancellationToken).ConfigureAwait(false);
                repository.RecordManagedJob(new ManagedJobRecord
                {
                    RouteId = route.Id,
                    TargetDeviceId = action.TargetDeviceId,
                    JobId = jobId,
                    Fingerprint = JobIdentity.Fingerprint(action.Job!, JobMatchMode.Exact),
                    CreatedUtc = DateTime.UtcNow,
                });
                run.Created++;
            }
            catch (Exception exception)
            {
                run.Failed++;
                run.Messages.Add($"{route.Name}: failed to create '{action.Job!.Name}' on {action.TargetDeviceId}: {exception.Message}");
                logger.ErrorException("Unable to create download job {0} on target {1}", exception, action.Job!.Name, action.TargetDeviceId);
                if (!route.Safety.ContinueOnError) break;
            }

            progress?.Report(25 + (75d * (index + 1) / Math.Max(creates.Count, 1)));
        }
    }

    private static IList<SyncRoute> SelectRoutes(IList<SyncRoute> routes, PluginState state, string? routeId, bool force, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(routeId))
            return routes.Where(route => string.Equals(route.Id, routeId, StringComparison.OrdinalIgnoreCase)).ToList();
        if (force) return routes.Where(route => route.Enabled).ToList();
        return routes.Where(route => route.Enabled &&
            (!state.LastRunByRoute.TryGetValue(route.Id, out var lastRun) || lastRun.AddMinutes(route.MinimumIntervalMinutes) <= now)).ToList();
    }

    internal static bool IsEffectivePreview(bool requestedPreview, bool globalDryRun, IEnumerable<SyncRoute> routes) =>
        requestedPreview || globalDryRun || routes.All(route => route.Safety.DryRun);

    internal static IEnumerable<string> RouteIdsToAdvance(bool requestedPreview, IEnumerable<SyncRoute> routes) =>
        requestedPreview ? Array.Empty<string>() : routes.Select(route => route.Id);
}
