namespace Emby.ApiClient.Api
{
    using System.Threading.Tasks;
    using Emby.ApiClient.Model;
    using RestSharp;

    public interface IDeviceServiceApi
    {
        Task<RestResponse<QueryResultDevicesDeviceInfo>> GetDevices(string sortOrder = null);
    }
}
