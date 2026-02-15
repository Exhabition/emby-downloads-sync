using System.Net;
using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Domain.Exceptions;
using EmbyDownloadsSync.Infrastructure.Services;
using Moq;
using RestSharp;

namespace EmbyDownloadsSync.Tests.Infrastructure.Services;

public class DeviceServiceTests
{
	private readonly Mock<IDeviceServiceApi> _mockDeviceServiceApi;
	private readonly DeviceService _deviceService;

	public DeviceServiceTests()
	{
		_mockDeviceServiceApi = new Mock<IDeviceServiceApi>();
		_deviceService = new DeviceService(_mockDeviceServiceApi.Object);
	}

	[Fact]
	public async Task GetDevicesAsync_ReturnsData_WhenResponseIsSuccessful()
	{
		// Arrange
		var expectedData = new QueryResultDevicesDeviceInfo
		{
			Items = [new DevicesDeviceInfo { Name = "Test Device" }]
		};

		SetupGetDevicesSuccess(expectedData);

		// Act
		var result = await _deviceService.GetDevicesAsync();

		// Assert
		Assert.Same(expectedData, result);
	}

	[Fact]
	public async Task GetDevicesAsync_ThrowsEmbyApiException_WhenResponseIsNotSuccessful()
	{
		// Arrange
		SetupGetDevicesFailure(HttpStatusCode.InternalServerError, "Server error");

		// Act
		var exception = await Assert.ThrowsAsync<EmbyApiException>(() => _deviceService.GetDevicesAsync());

		// Assert
		Assert.Contains("Failed to fetch devices", exception.Message);
	}

	[Fact]
	public async Task GetDevicesAsync_ThrowsEmbyApiException_WhenDataIsNull()
	{
		// Arrange
		SetupGetDevicesSuccess(null!);

		// Act
		var exception = await Assert.ThrowsAsync<EmbyApiException>(() => _deviceService.GetDevicesAsync());

		// Assert
		Assert.Contains("device list data was null", exception.Message);
	}

	private void SetupGetDevicesSuccess(QueryResultDevicesDeviceInfo? data)
	{
		var response = new RestResponse<QueryResultDevicesDeviceInfo>(new RestRequest())
		{
			Data = data,
			StatusCode = HttpStatusCode.OK,
			IsSuccessStatusCode = true,
			ResponseStatus = ResponseStatus.Completed
		};

		_mockDeviceServiceApi
			.Setup(api => api.GetDevices(It.IsAny<string>()))
			.ReturnsAsync(response);
	}

	private void SetupGetDevicesFailure(HttpStatusCode statusCode, string errorMessage)
	{
		var response = new RestResponse<QueryResultDevicesDeviceInfo>(new RestRequest())
		{
			Data = null,
			StatusCode = statusCode,
			ErrorMessage = errorMessage,
			IsSuccessStatusCode = false,
			ResponseStatus = ResponseStatus.Completed
		};

		_mockDeviceServiceApi
			.Setup(api => api.GetDevices(It.IsAny<string>()))
			.ReturnsAsync(response);
	}
}