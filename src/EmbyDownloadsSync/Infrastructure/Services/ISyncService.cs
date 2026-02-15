namespace EmbyDownloadsSync.Infrastructure.Services;

public interface ISyncService
{
    Task RunAsync();
    Task ValidateDevices();
    Task SyncAllDevices();
}
