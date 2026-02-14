namespace EmbyDownloadsSync.Services;

public interface ISyncService
{
    Task RunAsync();
    Task ValidateDevices();
    Task SyncAllDevices();
}
