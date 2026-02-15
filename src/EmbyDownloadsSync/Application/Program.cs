using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;

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