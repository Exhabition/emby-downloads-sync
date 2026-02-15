using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Infrastructure.Services;

public interface IJobService
{
    Task<List<SyncJob>> GetJobsByDeviceId(string deviceId);
    Task<QueryResultSyncJob> GetJobs();
    Task<SyncJobCreationResult> CreateDuplicateJob(SyncJob syncJob, string targetId);
}
