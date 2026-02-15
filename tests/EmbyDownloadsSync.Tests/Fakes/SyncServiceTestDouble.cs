using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;

namespace EmbyDownloadsSync.Tests.Fakes;

public class SyncServiceTestDouble : SyncService
{
    public readonly List<string> MissingJobs = [];
    public readonly List<string> ExistingJobs = [];
    public readonly List<string> FailedJobs = [];

    public SyncServiceTestDouble(EmbySettings settings, IDeviceService deviceService, IJobService jobService)
        : base(settings, deviceService, jobService)
    {
    }

    protected override void HandleExistingJob(SyncJob masterJob)
        => ExistingJobs.Add(GetJobKey(masterJob));

    protected override async Task HandleMissingJob(SyncJob masterJob, string targetId)
        => MissingJobs.Add(GetJobKey(masterJob));

    protected override void HandleFailedJob(SyncJob masterJob) => FailedJobs.Add(GetJobKey(masterJob));
}
