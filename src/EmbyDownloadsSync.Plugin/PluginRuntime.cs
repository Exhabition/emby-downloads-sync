using System;
using System.IO;
using EmbyDownloadsSync.Plugin.Configuration;
using EmbyDownloadsSync.Plugin.Persistence;
using EmbyDownloadsSync.Plugin.Services;
using MediaBrowser.Common;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;

namespace EmbyDownloadsSync.Plugin;

internal sealed class PluginRuntime : IDisposable
{
    private bool disposed;

    public PluginRuntime(IServerApplicationHost applicationHost, PluginOptionsStore optionsStore, ILogger logger, string dataFolderPath)
    {
        Logger = logger;
        OptionsStore = optionsStore;
        Directory.CreateDirectory(dataFolderPath);
        var serializer = applicationHost.Resolve<IJsonSerializer>();
        Repository = new PluginRepository(serializer, dataFolderPath);
        DeviceCatalog = new EmbyDeviceCatalog(applicationHost.Resolve<IDeviceManager>());
        Gateway = new EmbySyncJobGateway(
            applicationHost,
            serializer,
            () => TimeSpan.FromSeconds(optionsStore.Get().HttpTimeoutSeconds));
        Synchronizer = new SynchronizationService(optionsStore, Repository, DeviceCatalog, Gateway, logger);
        TaskManager = applicationHost.Resolve<ITaskManager>();
    }

    public ILogger Logger { get; }

    public PluginOptionsStore OptionsStore { get; }

    public PluginRepository Repository { get; }

    public IDeviceCatalog DeviceCatalog { get; }

    public ISyncJobGateway Gateway { get; }

    public SynchronizationService Synchronizer { get; }

    public ITaskManager TaskManager { get; }

    public void QueueSynchronization() => TaskManager.QueueIfNotRunning<Tasks.SynchronizeDownloadsTask>();

    public void Dispose()
    {
        if (disposed) return;
        Synchronizer.Dispose();
        disposed = true;
    }
}
