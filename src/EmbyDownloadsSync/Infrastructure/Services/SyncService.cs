using Emby.ApiClient.Api;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Application.Configuration;

namespace EmbyDownloadsSync.Infrastructure.Services;

public class SyncService : ISyncService
{
	public readonly Config Config;
	private readonly IDeviceService _deviceService;
	private readonly IJobService _jobService;

	public SyncService(Config config, IDeviceService deviceService = null, IJobService jobService = null)
	{
		Config = config;
		_deviceService = deviceService ?? new DeviceService(new DeviceServiceApi(Config.ApiClient));
		_jobService = jobService ?? new JobService(new SyncServiceApi(Config.ApiClient));
	}

	// TODO scoped-service
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

			await Task.Delay(Config.SyncInterval * 1000 * 60);
		}
	}

	public async Task ValidateDevices()
	{
		Console.WriteLine("Validating devices ...");
		var allDevices = await _deviceService.GetDevicesAsync();

		var foundDevices = new HashSet<string>();

		foreach (var device in allDevices.Items)
		{
			if (Config.DeviceIds.Contains(device.ReportedDeviceId))
			{
				foundDevices.Add(device.ReportedDeviceId);
				Console.WriteLine(
					$"Found Device: Id: {device.ReportedDeviceId} | Name: {device.Name} | Last Used: {device.DateLastActivity}");
			}
		}

		var missingDevices = Config.DeviceIds.Except(foundDevices).ToList();

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
		var masterDeviceId = Config.DeviceIds.First();
		var masterDeviceJobs = await _jobService.GetJobsByDeviceId(masterDeviceId);

		var subDeviceIds = Config.DeviceIds.Skip(1).ToList();
		var subDeviceJobsMap = await GetSubDeviceJobs(subDeviceIds);

		foreach (var masterDeviceJob in masterDeviceJobs)
		{
			if (masterDeviceJob.Status == SyncJobStatus.Failed)
			{
				HandleFailedJob(masterDeviceJob);
				continue;
			}
			;

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
			var subDeviceJobs = await _jobService.GetJobsByDeviceId(subDeviceId);
			var subDeviceJobsDict = subDeviceJobs.ToDictionary(job => GetJobKey(job), job => job);
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
		Console.WriteLine($" - RequestedItemIds: {masterJob.RequestedItemIds.ToString()}");

		try
		{
			await _jobService.CreateDuplicateJob(masterJob, targetId);
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to create job on target device {targetId}: {e.Message}");
		}
	}
}