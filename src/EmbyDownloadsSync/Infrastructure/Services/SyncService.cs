using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;
using Microsoft.Extensions.Logging;

namespace EmbyDownloadsSync.Infrastructure.Services;

public partial class SyncService(
	EmbySettings settings,
	IDeviceService deviceService,
	IJobService jobService,
	ILogger<SyncService> logger) : ISyncService
{
	public async Task RunAsync(CancellationToken cancellationToken = default)
	{
		LogStartingSync(logger);

		await ValidateDevices();

		using var timer = new PeriodicTimer(TimeSpan.FromMinutes(settings.SyncIntervalMinutes));

		do
		{
			try
			{
				await SyncAllDevices();
			}
			catch (Exception ex)
			{
				LogSyncError(logger, ex);
			}
		} while (await timer.WaitForNextTickAsync(cancellationToken));
	}

	public async Task ValidateDevices()
	{
		LogValidatingDevices(logger);

		var allDevices = await deviceService.GetDevicesAsync();
		var foundDeviceIds = allDevices.Items
			.Where(device => settings.DeviceIds.Contains(device.ReportedDeviceId))
			.Select(device =>
			{
				LogFoundDevice(logger, device.ReportedDeviceId, device.Name, device.DateLastActivity);
				return device.ReportedDeviceId;
			})
			.ToHashSet();

		var missingDevices = settings.DeviceIds.Except(foundDeviceIds).ToList();

		if (missingDevices.Count > 0)
		{
			foreach (var id in missingDevices)
			{
				LogMissingDevice(logger, id);
			}

			throw new InvalidOperationException(
				$"One or more configured device IDs are invalid: {string.Join(", ", missingDevices)}");
		}

		LogAllDevicesValid(logger);
	}

	public async Task SyncAllDevices()
	{
		LogSyncDevices(logger);

		var masterDeviceId = settings.DeviceIds[0];
		var masterDeviceJobs = await jobService.GetJobsByDeviceId(masterDeviceId);
		var subDeviceIds = settings.DeviceIds.Skip(1).ToList();
		var subDeviceJobsMap = await GetTargetDeviceJobsAsync(subDeviceIds);

		foreach (var masterJob in masterDeviceJobs)
		{
			if (masterJob.Status == SyncJobStatus.Failed)
			{
				await HandleFailedJob(masterJob);
				continue;
			}

			var masterJobKey = GetJobKey(masterJob);

			foreach (var (subDeviceId, subDeviceJobMap) in subDeviceJobsMap)
			{
				if (subDeviceJobMap.ContainsKey(masterJobKey))
				{
					await HandleExistingJob(masterJob);
				}
				else
				{
					await HandleMissingJob(masterJob, subDeviceId);
				}
			}
		}
	}

	private async Task<Dictionary<string, Dictionary<string, SyncJob>>> GetTargetDeviceJobsAsync(
		IEnumerable<string> targetDeviceIds)
	{
		var result = new Dictionary<string, Dictionary<string, SyncJob>>();

		foreach (var targetDeviceId in targetDeviceIds)
		{
			var jobs = await jobService.GetJobsByDeviceId(targetDeviceId);
			result[targetDeviceId] = jobs.ToDictionary(GetJobKey);
		}

		return result;
	}

	private static string GetJobKey(SyncJob job) =>
		$"{job.Name}_{string.Join(",", job.RequestedItemIds)}";

	private async Task HandleFailedJob(SyncJob masterJob) =>
		LogJobFailed(logger, masterJob.Name);

	private async Task HandleExistingJob(SyncJob masterJob) =>
		LogJobExists(logger, masterJob.Name);

	private async Task HandleMissingJob(SyncJob masterJob, string targetId)
	{
		LogCreatingJob(logger, targetId, masterJob.Name, masterJob.ItemCount, masterJob.UnwatchedOnly);

		try
		{
			await jobService.CreateDuplicateJob(masterJob, targetId);
		}
		catch (Exception ex)
		{
			LogJobCreationFailed(logger, masterJob.Name, targetId, ex);
		}
	}
}