using System;
using System.IO;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;

namespace EmbyDownloadsSync.Plugin.Configuration;

internal sealed class PluginOptionsStore
{
    private readonly ILogger logger;
    private readonly JsonFileStore<PluginOptions> store;

    public PluginOptionsStore(IApplicationHost applicationHost, ILogger logger)
    {
        this.logger = logger;
        var serializer = applicationHost.Resolve<IJsonSerializer>();
        var directory = applicationHost.Resolve<IApplicationPaths>().PluginConfigurationsPath;
        store = new JsonFileStore<PluginOptions>(serializer, Path.Combine(directory, "EmbyDownloadsSync.json"));
    }

    public event EventHandler? Saved;

    public PluginOptions Get()
    {
        try { return store.Get(); }
        catch (Exception exception)
        {
            logger.ErrorException("Unable to load Emby Downloads Sync settings", exception);
            return new PluginOptions();
        }
    }

    public void Set(PluginOptions options)
    {
        store.Set(options);
        Saved?.Invoke(this, EventArgs.Empty);
    }
}
