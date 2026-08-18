using System;
using System.Collections.Generic;
using EmbyDownloadsSync.Core.Domain;

namespace EmbyDownloadsSync.Plugin.Persistence;

public sealed class RouteCollection
{
    public IList<SyncRoute> Routes { get; set; } = new List<SyncRoute>();
}

public sealed class PluginState
{
    public IDictionary<string, DateTime> LastRunByRoute { get; set; } = new Dictionary<string, DateTime>();

    public IList<SyncRunSummary> Runs { get; set; } = new List<SyncRunSummary>();

    public IList<ManagedJobRecord> ManagedJobs { get; set; } = new List<ManagedJobRecord>();
}

public sealed class SyncRunSummary
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime StartedUtc { get; set; }

    public DateTime FinishedUtc { get; set; }

    public bool Preview { get; set; }

    public bool Success { get; set; }

    public int PlannedCreates { get; set; }

    public int Created { get; set; }

    public int DryRunSkipped { get; set; }

    public int Skipped { get; set; }

    public int Conflicts { get; set; }

    public int Failed { get; set; }

    public IList<string> Messages { get; set; } = new List<string>();

    public SyncPlan? Plan { get; set; }
}

public sealed class ManagedJobRecord
{
    public string RouteId { get; set; } = string.Empty;

    public string TargetDeviceId { get; set; } = string.Empty;

    public long? JobId { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public DateTime CreatedUtc { get; set; }
}
