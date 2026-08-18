using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace EmbyDownloadsSync.Core.Domain;

public enum SyncTopology
{
    [Description("One device to other devices")]
    OneToMany,
    [Description("Several devices to one device")]
    ManyToOne,
    [Description("Both directions")]
    Bidirectional,
    [Description("Keep every device in sync")]
    Mesh,
    [Description("Custom connections")]
    Explicit,
}

public enum JobMatchMode
{
    ContentOnly,
    ContentAndUser,
    NameAndContent,
    Exact,
}

public enum ConflictPolicy
{
    KeepTarget,
    PreferFirstSource,
    PreferNewestSource,
    PreferHigherBitrate,
    PreferLowerBitrate,
    CreateVariant,
    ReportOnly,
}

public enum ReconciliationMode
{
    CreateOnly,
    ReportDrift,
    ManagedExact,
}

public enum PlanActionType
{
    Create,
    SkipExisting,
    SkipFiltered,
    Conflict,
    Drift,
    DeleteManaged,
    LimitExceeded,
}

public sealed class DownloadJob
{
    public long? Id { get; set; }

    public string TargetId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public long? UserId { get; set; }

    public long? ParentId { get; set; }

    public IList<long> RequestedItemIds { get; set; } = new List<long>();

    public string Category { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public bool? UnwatchedOnly { get; set; }

    public bool? SyncNewContent { get; set; }

    public int? ItemLimit { get; set; }

    public int? Bitrate { get; set; }

    public string Quality { get; set; } = string.Empty;

    public string Profile { get; set; } = string.Empty;

    public string Container { get; set; } = string.Empty;

    public string VideoCodec { get; set; } = string.Empty;

    public string AudioCodec { get; set; } = string.Empty;

    public DateTime? DateCreatedUtc { get; set; }

    public DownloadJob CopyTo(string targetId)
    {
        return new DownloadJob
        {
            TargetId = targetId,
            Name = Name,
            UserId = UserId,
            ParentId = ParentId,
            RequestedItemIds = new List<long>(RequestedItemIds),
            Category = Category,
            Status = Status,
            UnwatchedOnly = UnwatchedOnly,
            SyncNewContent = SyncNewContent,
            ItemLimit = ItemLimit,
            Bitrate = Bitrate,
            Quality = Quality,
            Profile = Profile,
            Container = Container,
            VideoCodec = VideoCodec,
            AudioCodec = AudioCodec,
            DateCreatedUtc = DateCreatedUtc,
        };
    }
}

public sealed class DeviceEdge
{
    public string SourceDeviceId { get; set; } = string.Empty;

    public string TargetDeviceId { get; set; } = string.Empty;
}

public sealed class JobFilter
{
    public string IncludeNamePattern { get; set; } = string.Empty;

    public string ExcludeNamePattern { get; set; } = string.Empty;

    public bool PatternsAreRegularExpressions { get; set; }

    public IList<string> IncludedStatuses { get; set; } = new List<string>();

    public IList<string> ExcludedStatuses { get; set; } = new List<string> { "Failed" };

    public IList<long> IncludedItemIds { get; set; } = new List<long>();

    public IList<long> ExcludedItemIds { get; set; } = new List<long>();

    public long? UserId { get; set; }

    public string Category { get; set; } = string.Empty;

    public bool? UnwatchedOnly { get; set; }

    public bool? SyncNewContent { get; set; }

    public string Quality { get; set; } = string.Empty;

    public string Profile { get; set; } = string.Empty;

    public string Container { get; set; } = string.Empty;

    public string VideoCodec { get; set; } = string.Empty;

    public string AudioCodec { get; set; } = string.Empty;

    public int? MinimumBitrate { get; set; }

    public int? MaximumBitrate { get; set; }

    public int? MinimumItemLimit { get; set; }

    public int? MaximumItemLimit { get; set; }

    public int? MaximumAgeDays { get; set; }
}

public sealed class JobTransform
{
    public string NamePrefix { get; set; } = string.Empty;

    public string NameSuffix { get; set; } = string.Empty;

    public long? UserId { get; set; }

    public bool OverrideUserId { get; set; }

    public bool? UnwatchedOnly { get; set; }

    public bool? SyncNewContent { get; set; }

    public int? ItemLimit { get; set; }

    public int? Bitrate { get; set; }

    public string Quality { get; set; } = string.Empty;

    public string Profile { get; set; } = string.Empty;

    public string Container { get; set; } = string.Empty;

    public string VideoCodec { get; set; } = string.Empty;

    public string AudioCodec { get; set; } = string.Empty;
}

public sealed class RouteSafety
{
    public bool DryRun { get; set; }

    public int MaximumCreates { get; set; } = 100;

    public int MaximumDeletes { get; set; }

    public int MaximumMutations { get; set; } = 100;

    public bool ContinueOnError { get; set; } = true;
}

public sealed class SyncRoute
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name { get; set; } = "New route";

    public bool Enabled { get; set; } = true;

    public SyncTopology Topology { get; set; } = SyncTopology.OneToMany;

    public IList<string> SourceDeviceIds { get; set; } = new List<string>();

    public IList<string> TargetDeviceIds { get; set; } = new List<string>();

    public IList<DeviceEdge> ExplicitEdges { get; set; } = new List<DeviceEdge>();

    public JobMatchMode MatchMode { get; set; } = JobMatchMode.Exact;

    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.KeepTarget;

    public ReconciliationMode ReconciliationMode { get; set; } = ReconciliationMode.CreateOnly;

    public JobFilter Filter { get; set; } = new JobFilter();

    public JobTransform Transform { get; set; } = new JobTransform();

    public RouteSafety Safety { get; set; } = new RouteSafety();

    public int MinimumIntervalMinutes { get; set; } = 15;
}

public sealed class PlanningSnapshot
{
    public IList<DownloadJob> Jobs { get; set; } = new List<DownloadJob>();

    public IList<string> KnownDeviceIds { get; set; } = new List<string>();

    public DateTime CapturedUtc { get; set; } = DateTime.UtcNow;
}

public sealed class PlanAction
{
    public PlanActionType Type { get; set; }

    public string RouteId { get; set; } = string.Empty;

    public string RouteName { get; set; } = string.Empty;

    public string SourceDeviceId { get; set; } = string.Empty;

    public string TargetDeviceId { get; set; } = string.Empty;

    public DownloadJob? Job { get; set; }

    public long? ExistingJobId { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class SyncPlan
{
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public IList<PlanAction> Actions { get; set; } = new List<PlanAction>();

    public IList<string> Warnings { get; set; } = new List<string>();

    public int CreateCount => Count(PlanActionType.Create);

    public int ConflictCount => Count(PlanActionType.Conflict);

    public int SkippedCount => Count(PlanActionType.SkipExisting) + Count(PlanActionType.SkipFiltered);

    private int Count(PlanActionType type)
    {
        var count = 0;
        foreach (var action in Actions)
        {
            if (action.Type == type)
            {
                count++;
            }
        }

        return count;
    }
}
