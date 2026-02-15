using Emby.ApiClient.Api;
using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;
using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: true)
	.AddEnvironmentVariables("EMBY_")
	.Build();

var settings = configuration.GetRequiredSection(EmbySettings.SectionName).Get<EmbySettings>()
			   ?? throw new InvalidOperationException("Failed to bind EmbySettings from configuration");
settings.Validate();

var apiClient = new ApiClient(settings.ServerUrl, new EmbyApiKeyAuthenticator(settings.ApiKey));

var services = new ServiceCollection();

services.AddLogging(builder =>
{
	builder.AddConsole();
});
services.AddSingleton(settings);
services.AddSingleton<ISyncServiceApi>(new SyncServiceApi(apiClient));
services.AddSingleton<IDeviceServiceApi>(new DeviceServiceApi(apiClient));
services.AddSingleton<IJobService, JobService>();
services.AddSingleton<IDeviceService, DeviceService>();
services.AddSingleton<ISyncService, SyncService>();

var serviceProvider = services.BuildServiceProvider();

try
{
	var syncService = serviceProvider.GetRequiredService<ISyncService>();
	await syncService.RunAsync();
}
catch (Exception ex)
{
	Console.WriteLine($"Fatal error: {ex.Message}");
}