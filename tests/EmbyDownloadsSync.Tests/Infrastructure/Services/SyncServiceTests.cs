using Emby.ApiClient.Model;
using EmbyDownloadsSync.Configuration;
using EmbyDownloadsSync.Infrastructure;
using EmbyDownloadsSync.Services;
using EmbyDownloadsSync.Tests.Fakes;
using Moq;

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
			Items =
			[
				new DevicesDeviceInfo { ReportedDeviceId = "1", Name = "Test Device 1" },
				new DevicesDeviceInfo { ReportedDeviceId = "2", Name = "Test Device 2" }
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

	[Fact]
	public async Task SyncAllDevices_ShouldMarkMissingJobs()
	{
		// Arrange
		var config = new Config(
			"http://localhost:8096",
			"api-key",
			["master", "sub"]);

		var mockJobService = new Mock<IJobService>();
		mockJobService.Setup(s => s.GetJobsByDeviceId("master"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);
		mockJobService.Setup(s => s.GetJobsByDeviceId("sub"))
			.ReturnsAsync([]);

		var service = new SyncServiceTestDouble(config, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.MissingJobs);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldMarkExistingJobs()
	{
		// Arrange
		var config = new Config(
			"http://localhost:8096",
			"api-key",
			["master", "sub"]);

		var mockJobService = new Mock<IJobService>();
		mockJobService.Setup(s => s.GetJobsByDeviceId("master"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);
		mockJobService.Setup(s => s.GetJobsByDeviceId("sub"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);

		var service = new SyncServiceTestDouble(config, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.ExistingJobs);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldIgnoreFailedJobs()
	{
		// Arrange
		var config = new Config(
			"http://localhost:8096",
			"api-key",
			["master", "sub"]);

		var mockJobService = new Mock<IJobService>();
		mockJobService.Setup(s => s.GetJobsByDeviceId("master"))
			.ReturnsAsync([
				new SyncJob
				{
					Name = "Movie", RequestedItemIds = [124904L, 193414L], Status = SyncJobStatus.Failed
				}
			]);
		mockJobService.Setup(s => s.GetJobsByDeviceId("sub"))
			.ReturnsAsync([]);

		var service = new SyncServiceTestDouble(config, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.FailedJobs);
	}
}