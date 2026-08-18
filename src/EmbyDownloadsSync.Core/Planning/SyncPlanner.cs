using System;
using System.Collections.Generic;
using System.Linq;
using EmbyDownloadsSync.Core.Domain;

namespace EmbyDownloadsSync.Core.Planning;

public sealed class SyncPlanner
{
    private readonly RouteValidator validator = new RouteValidator();

    public SyncPlan Build(PlanningSnapshot snapshot, IEnumerable<SyncRoute> routes)
    {
        if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
        if (routes == null) throw new ArgumentNullException(nameof(routes));
        var plan = new SyncPlan { CreatedUtc = snapshot.CapturedUtc };
        var jobsByDevice = snapshot.Jobs
            .GroupBy(job => job.TargetId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var enabledRoutes = routes.Where(value => value.Enabled).ToList();
        foreach (var route in enabledRoutes)
        {
            if (route.ReconciliationMode == ReconciliationMode.ManagedExact)
                plan.Warnings.Add($"{route.Name}: managed cleanup is gated until Emby's delete-job contract is verified; this run is create-only.");
            var errors = validator.Validate(route, snapshot.KnownDeviceIds);
            if (errors.Count > 0)
            {
                foreach (var error in errors) plan.Warnings.Add($"{route.Name}: {error}");
                continue;
            }

            BuildRoutePlan(plan, snapshot, jobsByDevice, route);
            ApplyLimits(plan, route);
        }

        ConsolidateAcrossRoutes(plan, enabledRoutes);

        return plan;
    }

    private static void ConsolidateAcrossRoutes(SyncPlan plan, IList<SyncRoute> routes)
    {
        var routeMap = routes.ToDictionary(route => route.Id, StringComparer.OrdinalIgnoreCase);
        var creates = plan.Actions.Where(action => action.Type == PlanActionType.Create && action.Job != null).ToList();
        foreach (var identical in creates.GroupBy(action =>
                     action.TargetDeviceId + "\u001f" + JobIdentity.Fingerprint(action.Job!, JobMatchMode.Exact),
                     StringComparer.OrdinalIgnoreCase))
        {
            foreach (var duplicate in identical.Skip(1))
            {
                duplicate.Type = PlanActionType.SkipExisting;
                duplicate.Reason = "An earlier route already planned the same target job.";
            }
        }

        creates = plan.Actions.Where(action => action.Type == PlanActionType.Create && action.Job != null).ToList();
        foreach (var contentGroup in creates.GroupBy(action =>
                     action.TargetDeviceId + "\u001f" + JobIdentity.ContentFingerprint(action.Job!),
                     StringComparer.OrdinalIgnoreCase))
        {
            var variants = contentGroup.ToList();
            if (variants.Count < 2 || variants.All(action => routeMap[action.RouteId].ConflictPolicy == ConflictPolicy.CreateVariant))
                continue;
            foreach (var conflict in variants.Skip(1))
            {
                conflict.Type = PlanActionType.Conflict;
                conflict.Reason = "Another route planned the same content with different settings; route order preserved the first plan.";
            }
        }
    }

    private static void BuildRoutePlan(
        SyncPlan plan,
        PlanningSnapshot snapshot,
        IDictionary<string, List<DownloadJob>> jobsByDevice,
        SyncRoute route)
    {
        var candidates = new List<Candidate>();
        foreach (var edge in RouteValidator.CompileEdges(route))
        {
            foreach (var sourceJob in GetJobs(jobsByDevice, edge.SourceDeviceId))
            {
                if (!JobPolicies.Matches(sourceJob, route.Filter, snapshot.CapturedUtc))
                {
                    plan.Actions.Add(Action(PlanActionType.SkipFiltered, route, edge, sourceJob, "Job was excluded by the route filter."));
                    continue;
                }

                candidates.Add(new Candidate
                {
                    Edge = edge,
                    Source = sourceJob,
                    Desired = JobPolicies.Transform(sourceJob, edge.TargetDeviceId, route.Transform),
                });
            }
        }

        foreach (var group in candidates.GroupBy(
                     candidate => candidate.Edge.TargetDeviceId + "\u001f" + JobIdentity.ContentFingerprint(candidate.Desired),
                     StringComparer.OrdinalIgnoreCase))
        {
            var groupedCandidates = group.ToList();
            if (route.ConflictPolicy == ConflictPolicy.CreateVariant)
            {
                foreach (var variant in groupedCandidates
                             .GroupBy(candidate => JobIdentity.Fingerprint(candidate.Desired, JobMatchMode.Exact), StringComparer.OrdinalIgnoreCase)
                             .Select(variantGroup => variantGroup.First()))
                    EvaluateCandidate(plan, jobsByDevice, route, variant);
            }
            else
            {
                var selected = SelectCandidate(groupedCandidates, route.ConflictPolicy);
                EvaluateCandidate(plan, jobsByDevice, route, selected);
            }
        }
    }

    private static Candidate SelectCandidate(IList<Candidate> candidates, ConflictPolicy policy)
    {
        switch (policy)
        {
            case ConflictPolicy.PreferNewestSource:
                return candidates.OrderByDescending(value => value.Source.DateCreatedUtc ?? DateTime.MinValue).First();
            case ConflictPolicy.PreferHigherBitrate:
                return candidates.OrderByDescending(value => value.Desired.Bitrate ?? 0).First();
            case ConflictPolicy.PreferLowerBitrate:
                return candidates.OrderBy(value => value.Desired.Bitrate ?? int.MaxValue).First();
            default:
                return candidates[0];
        }
    }

    private static void EvaluateCandidate(
        SyncPlan plan,
        IDictionary<string, List<DownloadJob>> jobsByDevice,
        SyncRoute route,
        Candidate candidate)
    {
        var targetJobs = GetJobs(jobsByDevice, candidate.Edge.TargetDeviceId);
        var desiredIdentity = JobIdentity.Fingerprint(candidate.Desired, route.MatchMode);
        var exact = targetJobs.FirstOrDefault(job => JobIdentity.Fingerprint(job, route.MatchMode) == desiredIdentity);
        if (exact != null)
        {
            var action = Action(PlanActionType.SkipExisting, route, candidate.Edge, candidate.Desired, "An equivalent target job already exists.");
            action.ExistingJobId = exact.Id;
            plan.Actions.Add(action);
            return;
        }

        var content = targetJobs.FirstOrDefault(job => JobIdentity.ContentFingerprint(job) == JobIdentity.ContentFingerprint(candidate.Desired));
        if (content != null && route.MatchMode != JobMatchMode.ContentOnly)
        {
            if (route.ConflictPolicy == ConflictPolicy.CreateVariant)
            {
                plan.Actions.Add(Action(PlanActionType.Create, route, candidate.Edge, candidate.Desired, "Creating the configured variant beside an existing content job."));
                return;
            }

            var type = route.ReconciliationMode == ReconciliationMode.ReportDrift ? PlanActionType.Drift : PlanActionType.Conflict;
            var action = Action(type, route, candidate.Edge, candidate.Desired, "Equivalent content exists with different synchronization settings; the target was preserved.");
            action.ExistingJobId = content.Id;
            plan.Actions.Add(action);
            return;
        }

        plan.Actions.Add(Action(PlanActionType.Create, route, candidate.Edge, candidate.Desired, "The target is missing this job."));
    }

    private static void ApplyLimits(SyncPlan plan, SyncRoute route)
    {
        var mutations = plan.Actions.Where(action => action.RouteId == route.Id && action.Type == PlanActionType.Create).ToList();
        var allowed = Math.Min(route.Safety.MaximumCreates, route.Safety.MaximumMutations);
        foreach (var action in mutations.Skip(allowed))
        {
            action.Type = PlanActionType.LimitExceeded;
            action.Reason = "The route mutation safety limit was reached.";
        }
    }

    private static List<DownloadJob> GetJobs(IDictionary<string, List<DownloadJob>> jobsByDevice, string deviceId) =>
        jobsByDevice.TryGetValue(deviceId, out var jobs) ? jobs : new List<DownloadJob>();

    private static PlanAction Action(PlanActionType type, SyncRoute route, DeviceEdge edge, DownloadJob job, string reason) =>
        new PlanAction
        {
            Type = type,
            RouteId = route.Id,
            RouteName = route.Name,
            SourceDeviceId = edge.SourceDeviceId,
            TargetDeviceId = edge.TargetDeviceId,
            Job = job,
            Reason = reason,
        };

    private sealed class Candidate
    {
        public DeviceEdge Edge { get; set; } = new DeviceEdge();

        public DownloadJob Source { get; set; } = new DownloadJob();

        public DownloadJob Desired { get; set; } = new DownloadJob();
    }
}
