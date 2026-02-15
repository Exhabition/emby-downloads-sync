namespace EmbyDownloadsSync.Application.Configuration;

public class EmbySettings
{
    public const string SectionName = "Emby";

    public string ServerUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public List<string> DeviceIds { get; set; } = [];
    public int SyncIntervalMinutes { get; set; } = 15;

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerUrl))
            throw new InvalidOperationException("EmbySettings.ServerUrl is required");
        if (string.IsNullOrWhiteSpace(ApiKey))
            throw new InvalidOperationException("EmbySettings.ApiKey is required");
        if (DeviceIds.Count == 0)
            throw new InvalidOperationException("EmbySettings.DeviceIds must contain at least one device");
    }
}
