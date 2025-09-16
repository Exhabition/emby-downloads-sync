using Emby.ApiClient.Api;
using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;
using Emby.ApiClient.Model;
using EmbyDownloadsSync.Adapters;
using EmbyDownloadsSync.Utils;

namespace EmbyDownloadsSync.Services;

public class SyncService
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

	private async Task SyncAllDevices()
	{
		Console.WriteLine("Starting sync cycle...");
		var masterDeviceId = Config.DeviceIds.First();
		var masterDeviceJobs = await _jobService.GetJobsByDeviceId(masterDeviceId);
		
		var subDeviceIds = Config.DeviceIds.Skip(1).ToList();
		List<Dictionary<string,SyncJob>> subDeviceJobsList = new List<Dictionary<string, SyncJob>>();

		foreach (var subDeviceId in subDeviceIds)
		{
			var subDeviceJobs = await _jobService.GetJobsByDeviceId(subDeviceId);
			var subDeviceJobsDict = subDeviceJobs.ToDictionary(
				job => $"{job.Name}_{job.RequestedItemIds}", job => job);
			
			subDeviceJobsList.Add(subDeviceJobsDict);
		}

		foreach (var masterDeviceJob in masterDeviceJobs)
		{
			if (masterDeviceJob.Status == SyncJobStatus.Failed) continue;
			var masterJobUniqueId = $"{masterDeviceJob.Name}_{masterDeviceJob.RequestedItemIds}";
			
			foreach (var subDeviceJob in subDeviceJobsList)
			{
				// Ignore faulty downloads
				if (!subDeviceJob.ContainsKey(masterJobUniqueId))
				{
					// TODO fix way getting subDeviceId
					Console.WriteLine($"Found missing job on {subDeviceJob.First().Value.TargetId} , creating...");
					Console.WriteLine($"Name: {masterDeviceJob.Name}");
					Console.WriteLine($" - UnwatchedOnly: {masterDeviceJob.UnwatchedOnly}");
					Console.WriteLine($" - SyncNewContent: {masterDeviceJob.SyncNewContent}");
					Console.WriteLine($" - ItemCount: {masterDeviceJob.ItemCount}");
					Console.WriteLine($" - RequestedItemIds: {masterDeviceJob.RequestedItemIds.ToString()}");
				}
				else
				{
					Console.WriteLine("Job already exists, skipping...");
				}
			}
		}
	}
}