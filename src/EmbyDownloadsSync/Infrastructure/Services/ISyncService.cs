using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Infrastructure.Services;

public interface ISyncService
{
	Task RunAsync(CancellationToken cancellationToken = default);
	Task ValidateDevices();
	Task SyncAllDevices();
}