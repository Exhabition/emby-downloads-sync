using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Tasks;

namespace EmbyDownloadsSync.Plugin.Tasks;

public sealed class SynchronizeDownloadsTask : IScheduledTask
{
    public string Name => "Synchronize download jobs";

    public string Key => "EmbyDownloadsSync";

    public string Description => "Evaluates enabled synchronization routes and creates missing Emby download jobs.";

    public string Category => "Emby Downloads Sync";

    public async Task Execute(CancellationToken cancellationToken, IProgress<double> progress) =>
        await Plugin.Instance.Runtime.Synchronizer.RunAsync(false, false, null, progress, cancellationToken).ConfigureAwait(false);

    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers() => new[]
    {
        new TaskTriggerInfo { Type = "IntervalTrigger", IntervalTicks = TimeSpan.FromMinutes(15).Ticks },
    };
}
