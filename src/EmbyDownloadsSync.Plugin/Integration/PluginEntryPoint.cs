using System;
using MediaBrowser.Controller.Plugins;

namespace EmbyDownloadsSync.Plugin.Integration;

public sealed class PluginEntryPoint : IServerEntryPoint
{
    public void Run()
    {
        var runtime = Plugin.Instance.Runtime;
        runtime.Logger.Info("Emby Downloads Sync initialized with plugin data at {0}", Plugin.Instance.DataFolderPath);
    }

    public void Dispose() => Plugin.Instance.DisposeRuntime();
}
