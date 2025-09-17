using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Adapters;

public interface IJobService
{
	Task<List<SyncJob>> GetJobsByDeviceId(string deviceId);
	Task<QueryResultSyncJob> GetJobs();
	Task<SyncJobCreationResult> CreateDuplicateJob(SyncJob syncJob, string targetId);
}