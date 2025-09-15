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
	private readonly SyncService _syncService;
	
	public SyncServiceTests()
	{
		const string serverUrl = "http://localhost:8096";
		const string apiKey = "api-key";
		List<string> deviceIds = ["1", "2"];
		
		var testConfig = new Config(serverUrl, apiKey, deviceIds);
		var mockDeviceService = new Mock<IDeviceService>();

		var fakeDevices = new QueryResultDevicesDeviceInfo
		{
			Items = [
				new DevicesDeviceInfo { Id = "1", Name = "Test Device 1" },
				new DevicesDeviceInfo { Id = "2", Name = "Test Device 2" }
			]
		};

		mockDeviceService.Setup(service => service.GetDevicesAsync())
			.ReturnsAsync(fakeDevices)
			.Verifiable();
		
		_syncService = new SyncService(testConfig, deviceService: mockDeviceService.Object);
	}
	
	[Fact]
	public void ValidateDevices_ShouldAccept_WhenAllConfiguredDevicesExistOnServer()
	{
		// Arrange
		_syncService.Config.DeviceIds = ["1", "2"];
		
		// Act
		var validateAct = async () => await _syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsCompletedSuccessfully);
	}
	
	[Fact]
	public void ValidateDevices_ShouldThrowException_WhenConfiguredDeviceIsMissingOnServer()
	{
		// Arrange
		_syncService.Config.DeviceIds = ["1", "3"];

		// Act;
		var validateAct = async () => await _syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsFaulted);
	}
}