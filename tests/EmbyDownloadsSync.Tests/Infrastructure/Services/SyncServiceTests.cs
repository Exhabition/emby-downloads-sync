using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace EmbyDownloadsSync.Tests.Infrastructure.Services;

public class SyncServiceTests
{
	private const string MainDeviceId = "af398b";
	private const string TargetDeviceId = "adi394";

	private readonly EmbySettings _settings = new()
	{
		ServerUrl = "http://localhost:8096",
		ApiKey = "api-key",
		DeviceIds = [MainDeviceId, TargetDeviceId]
	};

	private readonly Mock<IDeviceService> _mockDeviceService = new();
	private readonly Mock<IJobService> _mockJobService = new();

	[Fact]
	public async Task ValidateDevices_ShouldSucceed_WhenAllConfiguredDevicesExist()
	{
		// Arrange
		_settings.DeviceIds = ["1", "2"];
		_mockDeviceService.Setup(s => s.GetDevicesAsync())
			.ReturnsAsync(new QueryResultDevicesDeviceInfo
			{
				Items =
				[
					new DevicesDeviceInfo { ReportedDeviceId = "1", Name = "Luke's Z Flip 5" },
					new DevicesDeviceInfo { ReportedDeviceId = "2", Name = "Luke's Tab A9+" }
				]
			});

		var service = CreateService();

		// Act & Assert
		await service.ValidateDevices();
	}

	[Fact]
	public async Task ValidateDevices_ShouldThrow_WhenConfiguredDeviceIsMissing()
	{
		// Arrange
		_settings.DeviceIds = ["1", "3"];
		_mockDeviceService.Setup(s => s.GetDevicesAsync())
			.ReturnsAsync(new QueryResultDevicesDeviceInfo
			{
				Items =
				[
					new DevicesDeviceInfo { ReportedDeviceId = "1", Name = "Luke's Z Flip 5" },
					new DevicesDeviceInfo { ReportedDeviceId = "2", Name = "Luke's Tab A9+" }
				]
			});

		var service = CreateService();

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ValidateDevices());

		Assert.Contains("3", exception.Message);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldCreateDuplicateJob_WhenJobMissingOnTargetDevice()
	{
		// Arrange
		var mainJob = new SyncJob { Name = "The Mentalist", RequestedItemIds = [124904L, 193414L] };

		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([mainJob]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(mainJob, TargetDeviceId), Times.Once);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldNotCreateDuplicateJob_WhenJobExistsOnTargetDevice()
	{
		// Arrange
		var mainJob = new SyncJob { Name = "The Mentalist", RequestedItemIds = [124904L, 193414L] };
		var targetJob = new SyncJob { Name = "The Mentalist", RequestedItemIds = [124904L, 193414L] };

		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([mainJob]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([targetJob]);

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(It.IsAny<SyncJob>(), It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldSkipFailedJobs_AndNotCreateDuplicates()
	{
		// Arrange
		var failedJob = new SyncJob
		{
			Name = "The Mentalist",
			RequestedItemIds = [124904L, 193414L],
			Status = SyncJobStatus.Failed
		};

		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([failedJob]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(It.IsAny<SyncJob>(), It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldSyncToMultipleTargetDevices()
	{
		// Arrange
		_settings.DeviceIds = [MainDeviceId, "target1", "target2"];

		var mainJob = new SyncJob { Name = "The Mentalist", RequestedItemIds = [124904L] };

		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([mainJob]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId("target1")).ReturnsAsync([]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId("target2")).ReturnsAsync([]);

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(mainJob, "target1"), Times.Once);
		_mockJobService.Verify(s => s.CreateDuplicateJob(mainJob, "target2"), Times.Once);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldContinue_WhenCreateDuplicateJobThrows()
	{
		// Arrange
		var job1 = new SyncJob { Name = "22 Jump Street", RequestedItemIds = [1L] };
		var job2 = new SyncJob { Name = "21 Jump Street", RequestedItemIds = [2L] };

		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([job1, job2]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);
		_mockJobService.Setup(s => s.CreateDuplicateJob(job1, TargetDeviceId))
			.ThrowsAsync(new InvalidOperationException("API error"));

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(job1, TargetDeviceId), Times.Once);
		_mockJobService.Verify(s => s.CreateDuplicateJob(job2, TargetDeviceId), Times.Once);
	}

	[Fact]
	public async Task SyncAllDevices_ShouldDoNothing_WhenNoJobsOnMainDevice()
	{
		// Arrange
		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);

		var service = CreateService();

		// Act
		await service.SyncAllDevices();

		// Assert
		_mockJobService.Verify(s => s.CreateDuplicateJob(It.IsAny<SyncJob>(), It.IsAny<string>()), Times.Never);
	}

	[Fact]
	public async Task RunAsync_ShouldValidateDevicesAndSync_ThenStopOnCancellation()
	{
		// Arrange
		_settings.SyncIntervalMinutes = 1;
		SetupValidDevices();
		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId)).ReturnsAsync([]);
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);

		var service = CreateService();
		using var cts = new CancellationTokenSource();

		// Act
		var runTask = service.RunAsync(cts.Token);
		await Task.Delay(50, TestContext.Current.CancellationToken);
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);
		_mockDeviceService.Verify(s => s.GetDevicesAsync(), Times.Once);
		_mockJobService.Verify(s => s.GetJobsByDeviceId(MainDeviceId), Times.Once);
	}

	[Fact]
	public async Task RunAsync_ShouldContinue_WhenSyncAllDevicesThrows()
	{
		// Arrange
		_settings.SyncIntervalMinutes = 1;
		SetupValidDevices();

		var callCount = 0;
		_mockJobService.Setup(s => s.GetJobsByDeviceId(MainDeviceId))
			.ReturnsAsync(() =>
			{
				callCount++;
				return callCount == 1 ? throw new InvalidOperationException("First call fails") : [];
			});
		_mockJobService.Setup(s => s.GetJobsByDeviceId(TargetDeviceId)).ReturnsAsync([]);

		var service = CreateService();
		using var cts = new CancellationTokenSource();

		// Act
		var runTask = service.RunAsync(cts.Token);
		await Task.Delay(50, TestContext.Current.CancellationToken);
		await cts.CancelAsync();

		// Assert
		await Assert.ThrowsAsync<OperationCanceledException>(() => runTask);
		Assert.Equal(1, callCount);
	}

	private SyncService CreateService() =>
		new(_settings, _mockDeviceService.Object, _mockJobService.Object, NullLogger<SyncService>.Instance);

	private void SetupValidDevices()
	{
		_mockDeviceService.Setup(s => s.GetDevicesAsync())
			.ReturnsAsync(new QueryResultDevicesDeviceInfo
			{
				Items =
				[
					new DevicesDeviceInfo { ReportedDeviceId = MainDeviceId, Name = "Main" },
					new DevicesDeviceInfo { ReportedDeviceId = TargetDeviceId, Name = "Target" }
				]
			});
	}
}