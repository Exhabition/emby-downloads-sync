using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Infrastructure.Services;

public interface IDeviceService
{
    Task<QueryResultDevicesDeviceInfo> GetDevicesAsync();
}
