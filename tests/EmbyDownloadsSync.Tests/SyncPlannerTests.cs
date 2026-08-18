using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Core.Planning;
using System.Text.RegularExpressions;

namespace EmbyDownloadsSync.Tests;

public sealed class SyncPlannerTests
{
    [Fact]
    public void OneToManyCreatesMissingJobsOnEveryTarget()
    {
        var plan = Plan(Route(SyncTopology.OneToMany, ["phone"], ["tablet", "laptop"]), Job("phone", "Movie", 1, 2));

        Assert.Equal(2, plan.CreateCount);
        Assert.Contains(plan.Actions, action => action.TargetDeviceId == "tablet" && action.Job!.RequestedItemIds.SequenceEqual([1L, 2L]));
        Assert.Contains(plan.Actions, action => action.TargetDeviceId == "laptop");
    }

    [Fact]
    public void ExistingJobsAreIdempotentlySkipped()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        var plan = Plan(route, Job("phone", "Movie", 1), Job("tablet", "Movie", 1));

        Assert.Equal(0, plan.CreateCount);
        Assert.Single(plan.Actions, action => action.Type == PlanActionType.SkipExisting);
    }

    [Fact]
    public void BidirectionalUsesFrozenSnapshotWithoutCascading()
    {
        var route = Route(SyncTopology.Bidirectional, ["phone"], ["tablet"]);
        var plan = Plan(route, Job("phone", "A", 1), Job("tablet", "B", 2));

        Assert.Equal(2, plan.CreateCount);
        Assert.Single(plan.Actions, action => action.TargetDeviceId == "phone" && action.Job!.Name == "B");
        Assert.Single(plan.Actions, action => action.TargetDeviceId == "tablet" && action.Job!.Name == "A");
    }

    [Fact]
    public void FilterAndTransformAreAppliedBeforePlanning()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.Filter.IncludeNamePattern = "Keep*";
        route.Transform.NamePrefix = "Synced - ";
        route.Transform.Bitrate = 5_000_000;

        var plan = Plan(route, Job("phone", "Keep this", 1), Job("phone", "Ignore this", 2));

        var create = Assert.Single(plan.Actions, action => action.Type == PlanActionType.Create);
        Assert.Equal("Synced - Keep this", create.Job!.Name);
        Assert.Equal(5_000_000, create.Job.Bitrate);
        Assert.Single(plan.Actions, action => action.Type == PlanActionType.SkipFiltered);
    }

    [Fact]
    public void TechnicalAndBehaviorFiltersCanBeCombined()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.Filter = new JobFilter
        {
            UnwatchedOnly = true,
            SyncNewContent = true,
            Quality = "High",
            Profile = "Mobile",
            VideoCodec = "h264",
            MinimumBitrate = 2_000_000,
            IncludedItemIds = [2],
        };
        var accepted = Job("phone", "Accepted", 1, 2);
        accepted.Quality = "high";
        accepted.Profile = "mobile";
        accepted.VideoCodec = "H264";
        accepted.Bitrate = 3_000_000;
        var rejected = Job("phone", "Rejected", 2);
        rejected.Quality = "Low";
        rejected.Profile = "Mobile";
        rejected.VideoCodec = "h264";
        rejected.Bitrate = 3_000_000;

        var plan = Plan(route, accepted, rejected);

        Assert.Single(plan.Actions, action => action.Type == PlanActionType.Create && action.Job!.Name == "Accepted");
        Assert.Single(plan.Actions, action => action.Type == PlanActionType.SkipFiltered && action.Job!.Name == "Rejected");
    }

    [Fact]
    public void KeepTargetReportsSettingsConflict()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.MatchMode = JobMatchMode.Exact;
        route.ConflictPolicy = ConflictPolicy.KeepTarget;
        var source = Job("phone", "Movie", 1);
        source.Bitrate = 5_000_000;
        var target = Job("tablet", "Movie", 1);
        target.Id = 99;
        target.Bitrate = 1_000_000;

        var plan = Plan(route, source, target);

        var conflict = Assert.Single(plan.Actions, action => action.Type == PlanActionType.Conflict);
        Assert.Equal(99, conflict.ExistingJobId);
    }

    [Fact]
    public void CreateVariantAllowsDifferentSettingsForSameContent()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.MatchMode = JobMatchMode.Exact;
        route.ConflictPolicy = ConflictPolicy.CreateVariant;
        var source = Job("phone", "Movie", 1);
        source.Quality = "High";
        var target = Job("tablet", "Movie", 1);
        target.Quality = "Low";

        Assert.Equal(1, Plan(route, source, target).CreateCount);
    }

    [Fact]
    public void SourcePriorityAndBitratePoliciesResolveFanIn()
    {
        var route = Route(SyncTopology.ManyToOne, ["phone", "laptop"], ["tablet"]);
        route.ConflictPolicy = ConflictPolicy.PreferHigherBitrate;
        var low = Job("phone", "Movie", 1);
        low.Bitrate = 1_000_000;
        var high = Job("laptop", "Movie", 1);
        high.Bitrate = 8_000_000;

        var create = Assert.Single(Plan(route, low, high).Actions, action => action.Type == PlanActionType.Create);

        Assert.Equal("laptop", create.SourceDeviceId);
        Assert.Equal(8_000_000, create.Job!.Bitrate);
    }

    [Fact]
    public void CreateVariantPreservesDistinctFanInSettings()
    {
        var route = Route(SyncTopology.ManyToOne, ["phone", "laptop"], ["tablet"]);
        route.ConflictPolicy = ConflictPolicy.CreateVariant;
        var low = Job("phone", "Movie", 1);
        low.Bitrate = 1_000_000;
        var high = Job("laptop", "Movie", 1);
        high.Bitrate = 8_000_000;

        var plan = Plan(route, low, high);

        Assert.Equal(2, plan.CreateCount);
        Assert.Contains(plan.Actions, action => action.Type == PlanActionType.Create && action.Job!.Bitrate == 1_000_000);
        Assert.Contains(plan.Actions, action => action.Type == PlanActionType.Create && action.Job!.Bitrate == 8_000_000);
    }

    [Fact]
    public void CreationCeilingTurnsExcessActionsIntoLimitWarnings()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.Safety.MaximumCreates = 1;
        route.Safety.MaximumMutations = 1;

        var plan = Plan(route, Job("phone", "One", 1), Job("phone", "Two", 2));

        Assert.Equal(1, plan.CreateCount);
        Assert.Single(plan.Actions, action => action.Type == PlanActionType.LimitExceeded);
    }

    [Fact]
    public void InvalidRoutesProduceWarningsInsteadOfMutations()
    {
        var route = Route(SyncTopology.OneToMany, ["missing"], ["tablet"]);

        var plan = new SyncPlanner().Build(new PlanningSnapshot
        {
            KnownDeviceIds = ["phone", "tablet"],
            Jobs = [Job("phone", "Movie", 1)],
        }, [route]);

        Assert.Empty(plan.Actions);
        Assert.NotEmpty(plan.Warnings);
    }

    [Fact]
    public void OverlappingRoutesDoNotPlanDuplicateTargetJobs()
    {
        var first = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        first.Id = "first";
        var second = Route(SyncTopology.OneToMany, ["laptop"], ["tablet"]);
        second.Id = "second";

        var plan = new SyncPlanner().Build(new PlanningSnapshot
        {
            KnownDeviceIds = ["phone", "tablet", "laptop"],
            Jobs = [Job("phone", "Movie", 1), Job("laptop", "Movie", 1)],
        }, [first, second]);

        Assert.Equal(1, plan.CreateCount);
        Assert.Contains(plan.Actions, action => action.Reason.Contains("earlier route"));
    }

    [Fact]
    public void InvalidRegularExpressionIsReportedAsRouteWarning()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.Filter.PatternsAreRegularExpressions = true;
        route.Filter.IncludeNamePattern = "[";

        var plan = Plan(route, Job("phone", "Movie", 1));

        Assert.Empty(plan.Actions);
        Assert.Contains(plan.Warnings, warning => warning.Contains("regular expression"));
    }

    [Fact]
    public void ManagedExactCleanupRemainsGatedUntilDeleteContractIsVerified()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.ReconciliationMode = ReconciliationMode.ManagedExact;

        var plan = Plan(route, Job("phone", "Movie", 1));

        Assert.DoesNotContain(plan.Actions, action => action.Type == PlanActionType.DeleteManaged);
        Assert.Contains(plan.Warnings, warning => warning.Contains("cleanup is gated", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PathologicalRegularExpressionsHaveABoundedExecutionTime()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet"]);
        route.Filter.PatternsAreRegularExpressions = true;
        route.Filter.IncludeNamePattern = "^(a+)+$";
        var pathologicalName = new string('a', 10_000) + "!";

        Assert.Throws<RegexMatchTimeoutException>(() => Plan(route, Job("phone", pathologicalName, 1)));
    }

    private static SyncPlan Plan(SyncRoute route, params DownloadJob[] jobs) => new SyncPlanner().Build(new PlanningSnapshot
    {
        CapturedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        KnownDeviceIds = ["phone", "tablet", "laptop"],
        Jobs = jobs,
    }, [route]);

    private static SyncRoute Route(SyncTopology topology, string[] sources, string[] targets) => new SyncRoute
    {
        Id = "route",
        Name = "Route",
        Topology = topology,
        SourceDeviceIds = sources,
        TargetDeviceIds = targets,
    };

    private static DownloadJob Job(string device, string name, params long[] items) => new DownloadJob
    {
        TargetId = device,
        Name = name,
        RequestedItemIds = items,
        UserId = 10,
        Category = "Latest",
        UnwatchedOnly = true,
        SyncNewContent = true,
    };
}
