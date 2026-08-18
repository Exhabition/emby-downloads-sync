using System;
using System.Collections.Generic;
using EmbyDownloadsSync.Plugin.Configuration;
using EmbyDownloadsSync.Plugin.UI;
using MediaBrowser.Common;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;

namespace EmbyDownloadsSync.Plugin;

public sealed class Plugin : BasePlugin, IHasUIPages, IHasPluginConfiguration
{
    private static readonly Guid PluginId = new Guid("43b82f97-e44d-4bc0-b8ba-6e7545b22d87");
    private readonly object runtimeSync = new object();
    private readonly IServerApplicationHost applicationHost;
    private readonly ILogger logger;
    private readonly PluginOptionsStore optionsStore;
    private IReadOnlyCollection<IPluginUIPageController>? pages;
    private PluginRuntime? runtime;

    public Plugin(IServerApplicationHost applicationHost, ILogManager logManager)
    {
        this.applicationHost = applicationHost;
        logger = logManager.GetLogger(Name);
        optionsStore = new PluginOptionsStore(applicationHost, logger);
        Instance = this;
    }

    internal static Plugin Instance { get; private set; } = null!;

    public override string Name => "Emby Downloads Sync";

    public override string Description => "Synchronizes download jobs between Emby devices using configurable routes and safety policies.";

    public override Guid Id => PluginId;

    internal PluginOptionsStore OptionsStore => optionsStore;

    internal PluginRuntime Runtime
    {
        get
        {
            lock (runtimeSync)
                return runtime ??= new PluginRuntime(applicationHost, optionsStore, logger, DataFolderPath);
        }
    }

    public IReadOnlyCollection<IPluginUIPageController> UIPageControllers
    {
        get
        {
            if (pages != null) return pages;
            var pluginInfo = GetPluginInfo();
            return pages = CreateUIPageControllers(pluginInfo, Runtime, optionsStore);
        }
    }

    internal static IReadOnlyCollection<IPluginUIPageController> CreateUIPageControllers(
        PluginInfo pluginInfo,
        PluginRuntime runtime,
        PluginOptionsStore optionsStore) => new IPluginUIPageController[]
        {
            new SettingsPageController(pluginInfo, optionsStore),
            new DashboardPageController(pluginInfo, runtime),
        };

    public Type ConfigurationType => typeof(PluginOptions);

    public BasePluginConfiguration Configuration { get; } = new BasePluginConfiguration();

    public void UpdateConfiguration(BasePluginConfiguration configuration)
    {
    }

    public void SetStartupInfo(Action<string> directoryCreateFn)
    {
    }

    internal void DisposeRuntime()
    {
        lock (runtimeSync)
        {
            runtime?.Dispose();
            runtime = null;
        }
    }
}
