using EmbyDownloadsSync.Application.Configuration;

namespace EmbyDownloadsSync.Tests.Application.Configuration;

public class EmbySettingsTests
{
	[Fact]
	public void EmbySettings_SectionName_ReturnsEmby()
	{
		Assert.Equal("Emby", EmbySettings.SectionName);
	}

	[Fact]
	public void EmbySettings_SyncIntervalMinutes_DefaultsTo15()
	{
		var settings = new EmbySettings
		{
			ServerUrl = "http://localhost:8096",
			ApiKey = "test-key",
			DeviceIds = ["1", "2"]
		};

		Assert.Equal(15, settings.SyncIntervalMinutes);
	}
}