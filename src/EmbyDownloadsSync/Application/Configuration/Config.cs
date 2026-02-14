using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;

namespace EmbyDownloadsSync.Configuration;

public class Config
{
    public string ApiKey { get; set; }
    public string ServerUrl { get; set; }
    public List<string> DeviceIds { get; set; }
    public int SyncInterval { get; set; }

    public ApiClient ApiClient { get; set; }

    public Config()
    {
        var serverUrl = Environment.GetEnvironmentVariable("EMBY_SERVER_URL")
                    ?? throw new Exception("EMBY_SERVER_URL environment variable is not set");
        var apiKey = Environment.GetEnvironmentVariable("EMBY_API_KEY")
                 ?? throw new Exception("EMBY_API_KEY environment variable is not set");

        var deviceIdsString = Environment.GetEnvironmentVariable("EMBY_DEVICE_IDS")
                        ?? throw new Exception("EMBY_DEVICE_IDS environment variable is not set");
        var deviceIds = deviceIdsString.Split(',').ToList();

        var intervalString = Environment.GetEnvironmentVariable("SYNC_INTERVAL");
        var syncInterval = intervalString != null ? int.Parse(intervalString) : 15; // default to 15 minutes

        Initialize(serverUrl, apiKey, deviceIds, syncInterval);
    }

    public Config(string serverUrl, string apiKey, List<string> deviceIds, int syncInterval = 15)
    {
        Initialize(serverUrl, apiKey, deviceIds, syncInterval);
    }

    private void Initialize(string serverUrl, string apiKey, List<string> deviceIds, int syncInterval)
    {
        ServerUrl = serverUrl;
        ApiKey = apiKey;
        DeviceIds = deviceIds;
        SyncInterval = syncInterval;

        ApiClient = new ApiClient(ServerUrl, new EmbyApiKeyAuthenticator(ApiKey));
    }
}
