using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using RestSharp;
using ServiceStack;

namespace EmbyDownloadsSync.Adapters;

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

	public virtual async Task<SyncJobCreationResult> CreateDuplicateJob(SyncJob jobToCopy, string targetId)
	{
		var copyJob = new SyncJobRequestFix(jobToCopy, targetId);
		var restRequest = new RestRequest("/Sync/Jobs", Method.Post)
			.AddJsonBody(copyJob); 
		
		var response = await _api.ApiClient.RestClient.ExecuteAsync<SyncJobCreationResult>(restRequest);
		
		if (!response.IsSuccessful || response.Data == null)
			throw new Exception("Failed create sync job at Emby server");

		return response.Data;
	}
}