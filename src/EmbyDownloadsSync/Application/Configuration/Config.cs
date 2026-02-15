namespace EmbyDownloadsSync.Application.Configuration;

public class EmbySettings
{
    public const string SectionName = "Emby";

    public required string ServerUrl { get; set; }
    public required string ApiKey { get; set; }
    public required List<string> DeviceIds { get; set; }
    public int SyncIntervalMinutes { get; set; } = 15;
}
