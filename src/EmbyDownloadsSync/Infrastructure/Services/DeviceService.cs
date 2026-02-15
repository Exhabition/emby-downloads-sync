using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Domain.Exceptions;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class DeviceService(IDeviceServiceApi deviceServiceApi) : IDeviceService
{
    public async Task<QueryResultDevicesDeviceInfo> GetDevicesAsync()
    {
        var response = await deviceServiceApi.GetDevices();

        if (!response.IsSuccessful)
            throw new EmbyApiException("Failed to fetch devices from Emby");

        return response.Data ??
               throw new EmbyApiException("Emby returned a successful response but device list data was null");
    }
}