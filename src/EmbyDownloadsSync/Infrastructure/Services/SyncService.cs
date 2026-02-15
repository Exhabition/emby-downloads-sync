using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class SyncService(EmbySettings settings, IDeviceService deviceService, IJobService jobService) : ISyncService
{
	public EmbySettings Settings { get; } = settings;

	public async Task RunAsync()
	{
		Console.WriteLine("Starting Emby download sync...");

		await ValidateDevices();

		while (true)
		{
			try
			{
				await SyncAllDevices();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error during sync: {ex.Message}");
			}

			await Task.Delay(Settings.SyncIntervalMinutes * 1000 * 60);
		}
	}

	public async Task ValidateDevices()
	{
		Console.WriteLine("Validating devices ...");
		var allDevices = await deviceService.GetDevicesAsync();

		var foundDevices = new HashSet<string>();

		foreach (var device in allDevices.Items)
		{
			if (Settings.DeviceIds.Contains(device.ReportedDeviceId))
			{
				foundDevices.Add(device.ReportedDeviceId);
				Console.WriteLine(
					$"Found Device: Id: {device.ReportedDeviceId} | Name: {device.Name} | Last Used: {device.DateLastActivity}");
			}
		}

		var missingDevices = Settings.DeviceIds.Except(foundDevices).ToList();

		if (missingDevices.Count > 0)
		{
			Console.WriteLine("Missing devices:");
			foreach (var id in missingDevices)
			{
				Console.WriteLine($" - {id}");
			}

			throw new ArgumentException(
				"One or more configured device IDs are invalid. See above for missing devices.");
		}

		Console.WriteLine("All configured devices are valid.");
	}

	public async Task SyncAllDevices()
	{
		Console.WriteLine("Starting sync cycle...");
		var masterDeviceId = Settings.DeviceIds.First();
		var masterDeviceJobs = await jobService.GetJobsByDeviceId(masterDeviceId);

		var subDeviceIds = Settings.DeviceIds.Skip(1).ToList();
		var subDeviceJobsMap = await GetSubDeviceJobs(subDeviceIds);

		foreach (var masterDeviceJob in masterDeviceJobs)
		{
			if (masterDeviceJob.Status == SyncJobStatus.Failed)
			{
				HandleFailedJob(masterDeviceJob);
				continue;
			}

			var masterJobUniqueId = GetJobKey(masterDeviceJob);

			foreach (var keyValuePair in subDeviceJobsMap)
			{
				var subDeviceId = keyValuePair.Key;
				var subDeviceJobMap = keyValuePair.Value;

				if (!subDeviceJobMap.ContainsKey(masterJobUniqueId))
				{
					await HandleMissingJob(masterDeviceJob, subDeviceId);
				}
				else
				{
					HandleExistingJob(masterDeviceJob);
				}
			}
		}
	}

	private async Task<Dictionary<string, Dictionary<string, SyncJob>>> GetSubDeviceJobs(IEnumerable<string> subDeviceIds)
	{
		var result = new Dictionary<string, Dictionary<string, SyncJob>>();

		foreach (var subDeviceId in subDeviceIds)
		{
			var subDeviceJobs = await jobService.GetJobsByDeviceId(subDeviceId);
			var subDeviceJobsDict = subDeviceJobs.ToDictionary(GetJobKey, job => job);
			result[subDeviceId] = subDeviceJobsDict;
		}

		return result;
	}

	protected string GetJobKey(SyncJob job) => $"{job.Name}_{string.Join(",", job.RequestedItemIds)}";

	protected virtual void HandleFailedJob(SyncJob masterJob)
	{
		Console.WriteLine("Job is in failed state, skipping...");
	}

	protected virtual void HandleExistingJob(SyncJob masterJob)
	{
		Console.WriteLine("Job already exists, skipping...");
	}

	protected virtual async Task HandleMissingJob(SyncJob masterJob, string targetId)
	{
		Console.WriteLine($"Found missing job on {targetId} , creating...");
		Console.WriteLine($"Name: {masterJob.Name}");
		Console.WriteLine($" - UnwatchedOnly: {masterJob.UnwatchedOnly}");
		Console.WriteLine($" - SyncNewContent: {masterJob.SyncNewContent}");
		Console.WriteLine($" - ItemCount: {masterJob.ItemCount}");
		Console.WriteLine($" - RequestedItemIds: {masterJob.RequestedItemIds}");

		try
		{
			await jobService.CreateDuplicateJob(masterJob, targetId);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to create job on target device {targetId}: {e.Message}");
		}
	}
}