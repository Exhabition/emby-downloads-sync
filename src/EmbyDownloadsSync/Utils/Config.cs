using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;

namespace EmbyDownloadsSync.Utils;

public class Config
{
	public string ApiKey { get; }
	public string ServerUrl { get; }
	public List<string> DeviceIds { get; set; }
	public int SyncInterval { get; }
	
	public ApiClient ApiClient { get; }

	public Config()
	{
		ServerUrl = Environment.GetEnvironmentVariable("EMBY_SERVER_URL") 
		            ?? throw new ArgumentNullException("EMBY_SERVER_URL environment variable is not set");
		ApiKey = Environment.GetEnvironmentVariable("EMBY_API_KEY") 
		         ?? throw new ArgumentNullException("EMBY_API_KEY environment variable is not set");
		
		var deviceIdsString = Environment.GetEnvironmentVariable("EMBY_DEVICE_IDS") 
		                ?? throw new ArgumentNullException("EMBY_DEVICE_IDS environment variable is not set");
		DeviceIds = deviceIdsString.Split(',').ToList();

		var intervalString = Environment.GetEnvironmentVariable("SYNC_INTERVAL");
		SyncInterval = intervalString != null ? int.Parse(intervalString) : 15; // default to 15 minutes
		
		ApiClient = new ApiClient(ServerUrl, new EmbyApiKeyAuthenticator(ApiKey));
	}

	public Config(string serverUrl, string apiKey, List<string> deviceIds, int syncInterval = 15)
	{
		ServerUrl = serverUrl;
		ApiKey = apiKey;
		DeviceIds = deviceIds;
		SyncInterval = syncInterval;
		
		ApiClient = new ApiClient(ServerUrl, new EmbyApiKeyAuthenticator(ApiKey));
	}
}