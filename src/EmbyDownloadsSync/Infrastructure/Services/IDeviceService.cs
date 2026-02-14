using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Infrastructure;

public interface IDeviceService
{
    Task<QueryResultDevicesDeviceInfo> GetDevicesAsync();
}
