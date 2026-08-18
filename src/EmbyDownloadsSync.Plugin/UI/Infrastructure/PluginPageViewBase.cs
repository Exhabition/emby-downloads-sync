using System;
using System.Threading.Tasks;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.GenericEdit;
using MediaBrowser.Model.Plugins.UI.Views;
using MediaBrowser.Model.Plugins.UI.Views.Enums;

namespace EmbyDownloadsSync.Plugin.UI.Infrastructure;

internal abstract class PluginPageViewBase : IPluginPageView, IPluginViewWithOptions
{
    protected PluginPageViewBase(string pluginId) => PluginId = pluginId;

    public event EventHandler<GenericEventArgs<IPluginUIView>>? UIViewInfoChanged;
    public virtual string Caption => ContentData.EditorTitle;
    public virtual string SubCaption => ContentData.EditorDescription;
    public string PluginId { get; }
    public IEditableObject ContentData { get; set; } = null!;
    public UserDto User { get; set; } = null!;
    public string RedirectViewUrl { get; set; } = string.Empty;
    public Uri HelpUrl { get; set; } = null!;
    public QueryCloseAction QueryCloseAction { get; set; }
    public WizardHidingBehavior WizardHidingBehavior { get; set; }
    public CompactViewAppearance CompactViewAppearance { get; set; }
    public DialogSize DialogSize { get; set; }
    public string OKButtonCaption { get; set; } = string.Empty;
    public DialogAction PrimaryDialogAction { get; set; }
    public bool ShowSave { get; set; } = true;
    public bool ShowBack { get; set; }
    public bool AllowSave { get; set; } = true;
    public bool AllowBack { get; set; } = true;
    public virtual bool IsCommandAllowed(string commandKey) => true;
    public virtual Task<IPluginUIView> RunCommand(string itemId, string commandId, string data) => Task.FromResult<IPluginUIView>(null!);
    public virtual Task Cancel() => Task.CompletedTask;
    public virtual void OnDialogResult(IPluginUIView dialogView, bool completedOk, object data) { }
    public virtual Task<IPluginUIView> OnSaveCommand(string itemId, string commandId, string data) => Task.FromResult<IPluginUIView>(this);
    public virtual PluginViewOptions ViewOptions => new PluginViewOptions
    {
        HelpUrl = HelpUrl,
        CompactViewAppearance = CompactViewAppearance,
        QueryCloseAction = QueryCloseAction,
        DialogSize = DialogSize,
        OKButtonCaption = OKButtonCaption,
        PrimaryDialogAction = PrimaryDialogAction,
        WizardHidingBehavior = WizardHidingBehavior,
    };
    protected void RaiseUIViewInfoChanged() => UIViewInfoChanged?.Invoke(this, new GenericEventArgs<IPluginUIView>(this));
}
