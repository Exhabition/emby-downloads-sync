using EmbyDownloadsSync.Utils;

namespace EmbyDownloadsSync.Tests;

public class ConfigTests
{
	public ConfigTests()
	{
		// Clear environment variables before each test to avoid side effects
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", null);
		Environment.SetEnvironmentVariable("EMBY_API_KEY", null);
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", null);
		Environment.SetEnvironmentVariable("SYNC_INTERVAL", null);
	}
	
	[Fact]
	public void CreatingConfig_WithoutConstructorParameters_ShouldThrowErrorIfNoApiKeyEnvironmentVar()
	{
		// Arrange
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", "http://localhost:8096");
		Environment.SetEnvironmentVariable("EMBY_API_KEY", null);
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", "1,2,3");
		
		// Act
		var createConfig = () => new Config();
		
		// Assert
		var exception = Assert.Throws<Exception>(createConfig);
		Assert.Equal("EMBY_API_KEY environment variable is not set", exception.Message);
	}
	
	[Fact]
	public void CreatingConfig_WithoutConstructorParameters_ShouldThrowErrorIfNoDeviceIdsEnvironmentVar()
	{
		// Arrange
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", "http://localhost:8096");
		Environment.SetEnvironmentVariable("EMBY_API_KEY", "test-api-key");
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", null);
		
		// Act
		var createConfig = () => new Config();
		
		// Assert
		var exception = Assert.Throws<Exception>(createConfig);
		Assert.Equal("EMBY_DEVICE_IDS environment variable is not set", exception.Message);
	}
	
	[Fact]
	public void CreatingConfig_WithoutConstructorParameters_ShouldThrowErrorIfNoServerUrlEnvironmentVar()
	{
		// Arrange
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", null);
		Environment.SetEnvironmentVariable("EMBY_API_KEY", "test-api-key");
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", "1,2,3");
		
		// Act
		var createConfig = () => new Config();
		
		// Assert
		var exception = Assert.Throws<Exception>(createConfig);
		Assert.Equal("EMBY_SERVER_URL environment variable is not set", exception.Message);
	}
	
	[Fact]
	public void CreatingConfig_ReadsEnvironmentVariables_ConvertsToConfigWithDefaultIntervalAndApiClient()
	{
		// Arrange
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", "http://localhost:8096");
		Environment.SetEnvironmentVariable("EMBY_API_KEY", "api-key");
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", "1,2,3");

		// Act
		var config = new Config();

		// Assert
		Assert.Equal("http://localhost:8096", config.ServerUrl);
		Assert.Equal("api-key", config.ApiKey);
		Assert.Equal(["1","2","3"], config.DeviceIds);
		Assert.Equal(15, config.SyncInterval);
		Assert.NotNull(config.ApiClient);
	}
	
	[Fact]
	public void CreatingConfig_ReadsEnvironmentVariables_ConvertsToConfigWithDifferentIntervalAndApiClient()
	{
		// Arrange
		Environment.SetEnvironmentVariable("EMBY_SERVER_URL", "http://localhost:8096");
		Environment.SetEnvironmentVariable("EMBY_API_KEY", "api-key");
		Environment.SetEnvironmentVariable("EMBY_DEVICE_IDS", "1,2,3");
		Environment.SetEnvironmentVariable("SYNC_INTERVAL", "10");
		
		// Act
		var config = new Config();

		// Assert
		Assert.Equal("http://localhost:8096", config.ServerUrl);
		Assert.Equal("api-key", config.ApiKey);
		Assert.Equal(["1","2","3"], config.DeviceIds);
		Assert.Equal(10, config.SyncInterval);
		Assert.NotNull(config.ApiClient);
	}
}