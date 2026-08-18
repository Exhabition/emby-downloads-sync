using System.Threading.Tasks;
using EmbyDownloadsSync.Plugin.Configuration;
using EmbyDownloadsSync.Plugin.UI.Infrastructure;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;

namespace EmbyDownloadsSync.Plugin.UI;

internal sealed class SettingsPageController : PluginPageControllerBase
{
    private readonly PluginInfo pluginInfo;
    private readonly PluginOptionsStore optionsStore;

    public SettingsPageController(PluginInfo pluginInfo, PluginOptionsStore optionsStore) : base(pluginInfo.Id)
    {
        this.pluginInfo = pluginInfo;
        this.optionsStore = optionsStore;
        PageInfo = new PluginPageInfo
        {
            Name = "EmbyDownloadsSyncSettings",
            DisplayName = "Downloads Sync settings",
            EnableInMainMenu = false,
            IsMainConfigPage = true,
            MenuIcon = "settings",
        };
    }

    public override PluginPageInfo PageInfo { get; }

    public override Task<IPluginUIView> CreateDefaultPageView() =>
        Task.FromResult<IPluginUIView>(new SettingsPageView(pluginInfo.Id, optionsStore));
}

internal sealed class SettingsPageView : PluginPageViewBase
{
    private readonly PluginOptionsStore optionsStore;

    public SettingsPageView(string pluginId, PluginOptionsStore optionsStore) : base(pluginId)
    {
        this.optionsStore = optionsStore;
        ContentData = optionsStore.Get();
    }

    public override Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data)
    {
        optionsStore.Set((PluginOptions)ContentData);
        return Task.FromResult<IPluginUIView>(this);
    }
}
