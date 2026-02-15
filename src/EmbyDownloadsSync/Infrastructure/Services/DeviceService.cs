using Emby.ApiClient.Api;
using Emby.ApiClient.Model;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class DeviceService : IDeviceService
{
    private readonly DeviceServiceApi _api;

    public DeviceService(DeviceServiceApi api)
    {
        _api = api;
    }

    public virtual async Task<QueryResultDevicesDeviceInfo> GetDevicesAsync()
    {
        var response = await _api.GetDevices(null);

        if (!response.IsSuccessful || response.Data == null)
            throw new Exception("Failed to fetch devices from Emby");

        return response.Data;
    }
}
