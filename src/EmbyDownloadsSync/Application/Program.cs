using Emby.ApiClient.Api;
using Emby.ApiClient.Client;
using Emby.ApiClient.Client.Authentication;
using EmbyDownloadsSync.Application.Configuration;
using EmbyDownloadsSync.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var configuration = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json", optional: true)
	.AddEnvironmentVariables("EMBY_")
	.Build();

var services = new ServiceCollection();

services.Configure<EmbySettings>(configuration.GetSection(EmbySettings.SectionName));

var settings = configuration.GetSection(EmbySettings.SectionName).Get<EmbySettings>()
			   ?? throw new InvalidOperationException("EmbySettings configuration is missing");

var apiClient = new ApiClient(settings.ServerUrl, new EmbyApiKeyAuthenticator(settings.ApiKey));

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