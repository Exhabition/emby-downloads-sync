using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Domain.Exceptions;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class JobService(ISyncServiceApi syncServiceApi) : IJobService
{
    public async Task<List<SyncJob>> GetJobsByDeviceId(string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var allJobs = await GetJobs();
        var deviceJobs = allJobs.Items?
            .Where(job => job.TargetId == deviceId)
            .ToList() ?? [];

        return deviceJobs;
    }

    public async Task<QueryResultSyncJob> GetJobs()
    {
        var response = await syncServiceApi.GetSyncJobs();

        if (!response.IsSuccessful)
        {
            throw new EmbyApiException($"Failed to fetch sync jobs from Emby. " +
                                       $"Status: {response.StatusCode}, Error: {response.ErrorMessage}");
        }

        return response.Data ??
               throw new EmbyApiException("Emby returned a successful response but sync jobs data was null");
    }

    public async Task<SyncJobCreationResult> CreateDuplicateJob(SyncJob syncJob, string targetId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetId);

        var syncJobRequest = new SyncJobRequest(targetId, syncJob);
        var response = await syncServiceApi.PostSyncJobs(syncJobRequest);

        if (!response.IsSuccessful)
        {
            throw new EmbyApiException($"Failed to create sync job at Emby. " +
                                       $"Status: {response.StatusCode}, Error: {response.ErrorMessage}");
        }

        return response.Data ??
               throw new EmbyApiException("Emby returned a successful response but created sync job data was null");
    }
}