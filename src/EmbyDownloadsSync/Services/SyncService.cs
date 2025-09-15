using Emby.ApiClient.Api;
using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;
using EmbyDownloadsSync.Adapters;
using EmbyDownloadsSync.Utils;

namespace EmbyDownloadsSync.Services;

public class SyncService
{
	public readonly Config Config;
	private readonly ApiClient _apiClient;
	private readonly IDeviceService _deviceService;

	public SyncService(Config config, IDeviceService deviceService = null)
	{
		Config = config;
		_deviceService = deviceService ?? new DeviceService(new DeviceServiceApi(_apiClient));
	}

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
		var allDevices = await _deviceService.GetDevicesAsync();

		var foundDevices = new HashSet<string>();

		foreach (var device in allDevices.Items)
		{
			if (Config.DeviceIds.Contains(device.Id))
			{
				foundDevices.Add(device.Id);
				Console.WriteLine(
					$"Found Device: Id: {device.Id} | Name: {device.Name} | Last Used: {device.DateLastActivity}");
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
		Console.WriteLine("Syncing all devices");
	}
}