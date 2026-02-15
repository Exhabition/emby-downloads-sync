using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;
using EmbyDownloadsSync.Tests.Fakes;
using Moq;

namespace EmbyDownloadsSync.Tests.Infrastructure.Services;

public class SyncServiceTests
{
	private readonly SyncService _syncService;
	private readonly EmbySettings _settings;

	public SyncServiceTests()
	{
		_settings = new EmbySettings
		{
			ServerUrl = "http://localhost:8096",
			ApiKey = "api-key",
			DeviceIds = ["1", "2"]
		};

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

		_syncService = new SyncService(_settings, mockDeviceService.Object, Mock.Of<IJobService>());
	}

	[Fact]
	public void ValidateDevices_ShouldAccept_WhenAllConfiguredDevicesExistOnServer()
	{
		// Arrange
		_settings.DeviceIds = ["1", "2"];

		// Act
		var validateAct = async () => await _syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsCompletedSuccessfully);
	}

	[Fact]
	public void ValidateDevices_ShouldThrowException_WhenConfiguredDeviceIsMissingOnServer()
	{
		// Arrange
		_settings.DeviceIds = ["1", "3"];

		// Act;
		var validateAct = async () => await _syncService.ValidateDevices();

		// Assert
		Assert.True(validateAct.Invoke().IsFaulted);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldMarkMissingJobs()
	{
		// Arrange
		var settings = new EmbySettings
		{
			ServerUrl = "http://localhost:8096",
			ApiKey = "api-key",
			DeviceIds = ["master", "sub"]
		};

		var mockJobService = new Mock<IJobService>();
		mockJobService.Setup(s => s.GetJobsByDeviceId("master"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);
		mockJobService.Setup(s => s.GetJobsByDeviceId("sub"))
			.ReturnsAsync([]);

		var service = new SyncServiceTestDouble(settings, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.MissingJobs);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldMarkExistingJobs()
	{
		// Arrange
		var settings = new EmbySettings
		{
			ServerUrl = "http://localhost:8096",
			ApiKey = "api-key",
			DeviceIds = ["master", "sub"]
		};

		var mockJobService = new Mock<IJobService>();
		mockJobService.Setup(s => s.GetJobsByDeviceId("master"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);
		mockJobService.Setup(s => s.GetJobsByDeviceId("sub"))
			.ReturnsAsync([new SyncJob { Name = "Movie", RequestedItemIds = [124904L, 193414L] }]);

		var service = new SyncServiceTestDouble(settings, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.ExistingJobs);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldIgnoreFailedJobs()
	{
		// Arrange
		var settings = new EmbySettings
		{
			ServerUrl = "http://localhost:8096",
			ApiKey = "api-key",
			DeviceIds = ["master", "sub"]
		};

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

		var service = new SyncServiceTestDouble(settings, Mock.Of<IDeviceService>(), mockJobService.Object);

		// Act
		await service.SyncAllDevices();

		// Assert
		Assert.Contains("Movie_124904,193414", service.FailedJobs);
	}
}