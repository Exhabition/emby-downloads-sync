using System;
using System.Threading.Tasks;
using EmbyDownloadsSync.Services;
using EmbyDownloadsSync.Utils;

namespace EmbyDownloadsSync
{
	class Program
	{
		static async Task Main()
		{
			try
			{
				var config = new Config();
			
				var syncService = new SyncService(config);
			
				await syncService.RunAsync();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Fatal error: {ex.Message}");
			}
		}
	}
}