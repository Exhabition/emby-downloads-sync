using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Adapters;

public interface IDeviceService
{
	Task<QueryResultDevicesDeviceInfo> GetDevicesAsync();
}