using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Domain.ValueObjects;
using RestSharp;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class JobService : IJobService
{
    private readonly SyncServiceApi _api;

    public JobService(SyncServiceApi api)
    {
        _api = api;
    }

    public virtual async Task<List<SyncJob>> GetJobsByDeviceId(string deviceId)
    {
        var allJobs = await GetJobs();

        // TODO TargetId != DeviceId
        var deviceJobs = allJobs.Items.Where(job => job.TargetId == deviceId).ToList();

        return deviceJobs;
    }

    public virtual async Task<QueryResultSyncJob> GetJobs()
    {
        var response = await _api.GetSyncJobs();

        if (!response.IsSuccessful || response.Data == null)
            throw new Exception("Failed to fetch jobs from Emby");

        return response.Data;
    }

    public virtual async Task<SyncJobCreationResult> CreateDuplicateJob(SyncJob syncJob, string targetId)
    {
        var syncJobRequest = new SyncJobRequest(targetId, syncJob);
        
        var response = await _api.PostSyncJobs(syncJobRequest);

        if (!response.IsSuccessful || response.Data == null)
            throw new Exception("Failed create sync job at Emby server");

        return response.Data;
    }
}
