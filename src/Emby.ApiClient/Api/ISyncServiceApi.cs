namespace Emby.ApiClient.Api
{
	using System.Threading.Tasks;
	using Emby.ApiClient.Model;
	using RestSharp;

	public interface ISyncServiceApi
	{
		Task<RestResponse<QueryResultSyncJob>> GetSyncJobs();
		Task<RestResponse<SyncJobCreationResult>> PostSyncJobs(SyncJobRequest body);
	}
}
