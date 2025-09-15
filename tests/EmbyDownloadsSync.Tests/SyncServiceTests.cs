using System.Net;
using Emby.ApiClient.Api;
using Emby.ApiClient.Client;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Adapters;
using EmbyDownloadsSync.Services;
using EmbyDownloadsSync.Utils;
using Moq;
using RestSharp;
using ServiceStack;

namespace EmbyDownloadsSync.Tests;

public class SyncServiceTests
{
	private SyncService syncService;
	
	public SyncServiceTests()
	{
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", "http://localhost:8096");
		Environment.SetEnvironmentVariable("EMBY_API_KEY", "api-key");
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", "1,2");
		
		var testConfig = new Config();
		var mockDeviceService = new Mock<IDeviceService>();

		var fakeDevices = new QueryResultDevicesDeviceInfo
		{
			Items = new List<DevicesDeviceInfo>
			{
				new DevicesDeviceInfo { Id = "1", Name = "Test Device 1" },
				new DevicesDeviceInfo { Id = "2", Name = "Test Device 2" }
			}
		};

		mockDeviceService.Setup(service => service.GetDevicesAsync())
			.ReturnsAsync(fakeDevices)
			.Verifiable();
		
		syncService = new SyncService(testConfig, deviceService: mockDeviceService.Object);
	}
	
	[Fact]
	public void ValidateDevices_ShouldAccept_WhenAllConfiguredDevicesExistOnServer()
	{
		// Arrange
		syncService.Config.DeviceIds = ["1", "2"];
		
		// Act
		Func<Task> validateAct = async () => await syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsCompletedSuccessfully);
	}
	
	[Fact]
	public async Task ValidateDevices_ShouldThrowException_WhenConfiguredDeviceIsMissingOnServer()
	{
		// Arrange
		syncService.Config.DeviceIds = ["1", "3"];

		// Act;
		Func<Task> validateAct = async () => await syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsFaulted);
	}
}