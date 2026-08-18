using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Persistence;
using EmbyDownloadsSync.Plugin.UI.Infrastructure;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;

namespace EmbyDownloadsSync.Plugin.UI;

public sealed class DashboardPageOptions : EditableOptionsBase
{
    public override string EditorTitle => "Downloads Sync";
    public override string EditorDescription => "Build flexible device synchronization routes, preview their plans, and apply missing download jobs.";

    public StatusItem Status { get; set; } = new StatusItem("Synchronization", "Ready", ItemStatus.None);
    public ButtonItem PreviewAll { get; set; } = new ButtonItem("Preview all routes") { CommandId = "PreviewAll", Icon = IconNames.preview };
    public ButtonItem ApplyAll { get; set; } = new ButtonItem("Apply all routes") { CommandId = "ApplyAll", Icon = IconNames.sync };
    public ButtonItem Refresh { get; set; } = new ButtonItem("Refresh") { CommandId = "Refresh", Icon = IconNames.refresh };
    public CaptionItem RoutesCaption { get; set; } = new CaptionItem("Routes");
    public GenericItemList Routes { get; set; } = new GenericItemList();
    public CaptionItem EditorCaption { get; set; } = new CaptionItem("Route editor");

    [DisplayName("Route ID")]
    [Description("Leave blank to create a route. Select a route above to edit it.")]
    public string RouteId { get; set; } = string.Empty;
    [DisplayName("Name")]
    public string RouteName { get; set; } = "New route";
    [DisplayName("Enabled")]
    public bool RouteEnabled { get; set; } = true;
    [DisplayName("Topology")]
    public SyncTopology Topology { get; set; } = SyncTopology.OneToMany;
    [DisplayName("Source device IDs")]
    [Description("Comma-separated. For bidirectional and mesh routes, sources and targets are combined into one device group.")]
    public string SourceDeviceIds { get; set; } = string.Empty;
    [DisplayName("Target device IDs")]
    public string TargetDeviceIds { get; set; } = string.Empty;
    [DisplayName("Explicit edges")]
    [Description("For Explicit topology only. One source>target edge per line.")]
    [EditMultiline(6)]
    public string ExplicitEdges { get; set; } = string.Empty;
    [DisplayName("Matching")]
    public JobMatchMode MatchMode { get; set; } = JobMatchMode.Exact;
    [DisplayName("Conflict policy")]
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.KeepTarget;
    [DisplayName("Reconciliation")]
    public ReconciliationMode ReconciliationMode { get; set; } = ReconciliationMode.CreateOnly;
    [DisplayName("Include job names")]
    [Description("Glob by default; for example Travel*. Leave empty for all jobs.")]
    public string IncludeNamePattern { get; set; } = string.Empty;
    [DisplayName("Exclude job names")]
    public string ExcludeNamePattern { get; set; } = string.Empty;
    [DisplayName("Patterns are regular expressions")]
    public bool PatternsAreRegularExpressions { get; set; }
    [DisplayName("Included statuses")]
    public string IncludedStatuses { get; set; } = string.Empty;
    [DisplayName("Excluded statuses")]
    public string ExcludedStatuses { get; set; } = "Failed";
    [DisplayName("User ID filter")]
    public long UserIdFilter { get; set; }
    [DisplayName("Category filter")]
    public string CategoryFilter { get; set; } = string.Empty;
    [DisplayName("Unwatched-only filter")]
    public bool? UnwatchedOnlyFilter { get; set; }
    [DisplayName("Sync-new-content filter")]
    public bool? SyncNewContentFilter { get; set; }
    [DisplayName("Quality filter")]
    public string QualityFilter { get; set; } = string.Empty;
    [DisplayName("Profile filter")]
    public string ProfileFilter { get; set; } = string.Empty;
    [DisplayName("Container filter")]
    public string ContainerFilter { get; set; } = string.Empty;
    [DisplayName("Video codec filter")]
    public string VideoCodecFilter { get; set; } = string.Empty;
    [DisplayName("Audio codec filter")]
    public string AudioCodecFilter { get; set; } = string.Empty;
    [DisplayName("Minimum bitrate")]
    public int MinimumBitrate { get; set; }
    [DisplayName("Maximum bitrate")]
    public int MaximumBitrate { get; set; }
    [DisplayName("Minimum item limit")]
    public int MinimumItemLimit { get; set; }
    [DisplayName("Maximum item limit")]
    public int MaximumItemLimit { get; set; }
    [DisplayName("Include item IDs")]
    public string IncludedItemIds { get; set; } = string.Empty;
    [DisplayName("Exclude item IDs")]
    public string ExcludedItemIds { get; set; } = string.Empty;
    [DisplayName("Maximum job age in days")]
    public int MaximumAgeDays { get; set; }
    [DisplayName("Name prefix")]
    public string NamePrefix { get; set; } = string.Empty;
    [DisplayName("Name suffix")]
    public string NameSuffix { get; set; } = string.Empty;
    [DisplayName("Override user ID")]
    [Description("Set to 0 to preserve the source user.")]
    public long OverrideUserId { get; set; }
    [DisplayName("Override unwatched-only")]
    public bool? OverrideUnwatchedOnly { get; set; }
    [DisplayName("Override sync-new-content")]
    public bool? OverrideSyncNewContent { get; set; }
    [DisplayName("Override item limit")]
    public int OverrideItemLimit { get; set; }
    [DisplayName("Override bitrate")]
    public int OverrideBitrate { get; set; }
    [DisplayName("Override quality")]
    public string OverrideQuality { get; set; } = string.Empty;
    [DisplayName("Override profile")]
    public string OverrideProfile { get; set; } = string.Empty;
    [DisplayName("Override container")]
    public string OverrideContainer { get; set; } = string.Empty;
    [DisplayName("Override video codec")]
    public string OverrideVideoCodec { get; set; } = string.Empty;
    [DisplayName("Override audio codec")]
    public string OverrideAudioCodec { get; set; } = string.Empty;
    [DisplayName("Minimum interval in minutes")]
    public int MinimumIntervalMinutes { get; set; } = 15;
    [DisplayName("Route dry run")]
    public bool RouteDryRun { get; set; }
    [DisplayName("Maximum creates")]
    public int MaximumCreates { get; set; } = 100;
    [DisplayName("Maximum managed deletions")]
    [Description("Stored for managed-exact routes. Deletion remains gated until Emby's contract is verified.")]
    public int MaximumManagedDeletes { get; set; }
    [DisplayName("Continue after individual errors")]
    public bool ContinueOnError { get; set; } = true;

    public ButtonItem NewRoute { get; set; } = new ButtonItem("New route") { CommandId = "NewRoute", Icon = IconNames.add };
    public ButtonItem SaveRoute { get; set; } = new ButtonItem("Save route") { CommandId = "SaveRoute", Icon = IconNames.save };
    public ButtonItem PreviewRoute { get; set; } = new ButtonItem("Preview route") { CommandId = "PreviewRoute", Icon = IconNames.preview };
    public ButtonItem ApplyRoute { get; set; } = new ButtonItem("Apply route") { CommandId = "ApplyRoute", Icon = IconNames.sync };
    public CaptionItem ResultsCaption { get; set; } = new CaptionItem("Latest run");
    public string LatestRun { get; set; } = "No runs yet.";
}

internal sealed class DashboardPageController : PluginPageControllerBase
{
    private readonly PluginInfo pluginInfo;
    private readonly PluginRuntime runtime;

    public DashboardPageController(PluginInfo pluginInfo, PluginRuntime runtime) : base(pluginInfo.Id)
    {
        this.pluginInfo = pluginInfo;
        this.runtime = runtime;
        PageInfo = new PluginPageInfo
        {
            Name = "EmbyDownloadsSyncDashboard",
            DisplayName = "Downloads Sync",
            EnableInMainMenu = true,
            IsMainConfigPage = false,
            MenuIcon = "sync",
        };
    }

    public override PluginPageInfo PageInfo { get; }

    public override Task<IPluginUIView> CreateDefaultPageView()
    {
        var view = new DashboardPageView(pluginInfo.Id, runtime);
        view.Load();
        return Task.FromResult<IPluginUIView>(view);
    }
}

internal sealed class DashboardPageView : PluginPageViewBase
{
    private readonly PluginRuntime runtime;

    public DashboardPageView(string pluginId, PluginRuntime runtime) : base(pluginId)
    {
        this.runtime = runtime;
        ContentData = new DashboardPageOptions();
        ShowSave = false;
    }

    private DashboardPageOptions Options => (DashboardPageOptions)ContentData;

    public void Load()
    {
        var rows = runtime.Repository.GetRoutes().Select(route => new GenericListItem
        {
            Icon = IconNames.sync,
            Status = route.Enabled ? ItemStatus.Succeeded : ItemStatus.None,
            PrimaryText = route.Name,
            SecondaryText = $"{route.Topology} · {string.Join(", ", route.SourceDeviceIds)} → {string.Join(", ", route.TargetDeviceIds)} · {route.MatchMode}",
            Button1 = new ButtonItem("Edit") { CommandId = "SelectRoute", ItemData = route.Id, Data1 = route.Id },
            Button2 = new ButtonItem("Delete") { CommandId = "DeleteRoute", ItemData = route.Id, Data1 = route.Id },
        }).ToList();
        Options.Routes = new GenericItemList(rows);
        var latest = runtime.Repository.GetState().Runs.FirstOrDefault();
        Options.LatestRun = latest == null
            ? "No runs yet."
            : $"{latest.FinishedUtc:u} · {(latest.Preview ? "preview" : "apply")} · planned {latest.PlannedCreates}, created {latest.Created}, dry-run {latest.DryRunSkipped}, skipped {latest.Skipped}, conflicts {latest.Conflicts}, failed {latest.Failed}";
        Options.Status.Status = ItemStatus.Succeeded;
        Options.Status.StatusText = $"{rows.Count} route(s) configured.";
    }

    public override async Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        try
        {
            switch (commandId)
            {
                case "Refresh": Load(); break;
                case "NewRoute": Populate(new SyncRoute { Safety = { MaximumCreates = runtime.OptionsStore.Get().DefaultMaximumCreates } }); break;
                case "SelectRoute": Select(ResolveId(itemId, data)); break;
                case "DeleteRoute": runtime.Repository.DeleteRoute(ResolveId(itemId, data)); Load(); break;
                case "SaveRoute": Save(); Load(); break;
                case "PreviewAll": await RunAsync(true, null).ConfigureAwait(false); break;
                case "ApplyAll": await RunAsync(false, null).ConfigureAwait(false); break;
                case "PreviewRoute": await RunAsync(true, RequireSelectedRoute()).ConfigureAwait(false); break;
                case "ApplyRoute": await RunAsync(false, RequireSelectedRoute()).ConfigureAwait(false); break;
                default: return await base.RunCommand(itemId, commandId, data).ConfigureAwait(false);
            }
        }
        catch (Exception exception)
        {
            runtime.Logger.ErrorException("Downloads Sync dashboard command {0} failed", exception, commandId);
            Options.Status.Status = ItemStatus.Failed;
            Options.Status.StatusText = exception.Message;
        }

        RaiseUIViewInfoChanged();
        return this;
    }

    private async Task RunAsync(bool preview, string? routeId)
    {
        Options.Status.Status = ItemStatus.InProgress;
        Options.Status.StatusText = preview ? "Building preview..." : "Applying synchronization plan...";
        var run = await runtime.Synchronizer.RunAsync(preview, true, routeId, null, CancellationToken.None).ConfigureAwait(false);
        Load();
        Options.Status.Status = run.Success ? ItemStatus.Succeeded : ItemStatus.Warning;
        Options.Status.StatusText = $"Planned {run.PlannedCreates}, created {run.Created}, dry-run {run.DryRunSkipped}, failed {run.Failed}.";
    }

    private void Select(string routeId)
    {
        var route = runtime.Repository.GetRoutes().FirstOrDefault(value => value.Id == routeId)
            ?? throw new InvalidOperationException("The selected route no longer exists.");
        Populate(route);
    }

    private void Populate(SyncRoute route)
    {
        Options.RouteId = route.Id;
        Options.RouteName = route.Name;
        Options.RouteEnabled = route.Enabled;
        Options.Topology = route.Topology;
        Options.SourceDeviceIds = string.Join(",", route.SourceDeviceIds);
        Options.TargetDeviceIds = string.Join(",", route.TargetDeviceIds);
        Options.ExplicitEdges = string.Join(Environment.NewLine, route.ExplicitEdges.Select(edge => edge.SourceDeviceId + ">" + edge.TargetDeviceId));
        Options.MatchMode = route.MatchMode;
        Options.ConflictPolicy = route.ConflictPolicy;
        Options.ReconciliationMode = route.ReconciliationMode;
        Options.IncludeNamePattern = route.Filter.IncludeNamePattern;
        Options.ExcludeNamePattern = route.Filter.ExcludeNamePattern;
        Options.PatternsAreRegularExpressions = route.Filter.PatternsAreRegularExpressions;
        Options.IncludedStatuses = string.Join(",", route.Filter.IncludedStatuses);
        Options.ExcludedStatuses = string.Join(",", route.Filter.ExcludedStatuses);
        Options.UserIdFilter = route.Filter.UserId ?? 0;
        Options.CategoryFilter = route.Filter.Category;
        Options.UnwatchedOnlyFilter = route.Filter.UnwatchedOnly;
        Options.SyncNewContentFilter = route.Filter.SyncNewContent;
        Options.QualityFilter = route.Filter.Quality;
        Options.ProfileFilter = route.Filter.Profile;
        Options.ContainerFilter = route.Filter.Container;
        Options.VideoCodecFilter = route.Filter.VideoCodec;
        Options.AudioCodecFilter = route.Filter.AudioCodec;
        Options.MinimumBitrate = route.Filter.MinimumBitrate ?? 0;
        Options.MaximumBitrate = route.Filter.MaximumBitrate ?? 0;
        Options.MinimumItemLimit = route.Filter.MinimumItemLimit ?? 0;
        Options.MaximumItemLimit = route.Filter.MaximumItemLimit ?? 0;
        Options.IncludedItemIds = string.Join(",", route.Filter.IncludedItemIds);
        Options.ExcludedItemIds = string.Join(",", route.Filter.ExcludedItemIds);
        Options.MaximumAgeDays = route.Filter.MaximumAgeDays ?? 0;
        Options.NamePrefix = route.Transform.NamePrefix;
        Options.NameSuffix = route.Transform.NameSuffix;
        Options.OverrideUserId = route.Transform.OverrideUserId ? route.Transform.UserId ?? 0 : 0;
        Options.OverrideUnwatchedOnly = route.Transform.UnwatchedOnly;
        Options.OverrideSyncNewContent = route.Transform.SyncNewContent;
        Options.OverrideItemLimit = route.Transform.ItemLimit ?? 0;
        Options.OverrideBitrate = route.Transform.Bitrate ?? 0;
        Options.OverrideQuality = route.Transform.Quality;
        Options.OverrideProfile = route.Transform.Profile;
        Options.OverrideContainer = route.Transform.Container;
        Options.OverrideVideoCodec = route.Transform.VideoCodec;
        Options.OverrideAudioCodec = route.Transform.AudioCodec;
        Options.MinimumIntervalMinutes = route.MinimumIntervalMinutes;
        Options.RouteDryRun = route.Safety.DryRun;
        Options.MaximumCreates = route.Safety.MaximumCreates;
        Options.MaximumManagedDeletes = route.Safety.MaximumDeletes;
        Options.ContinueOnError = route.Safety.ContinueOnError;
    }

    private void Save()
    {
        var route = new SyncRoute
        {
            Id = string.IsNullOrWhiteSpace(Options.RouteId) ? Guid.NewGuid().ToString("N") : Options.RouteId.Trim(),
            Name = Options.RouteName.Trim(),
            Enabled = Options.RouteEnabled,
            Topology = Options.Topology,
            SourceDeviceIds = Csv(Options.SourceDeviceIds),
            TargetDeviceIds = Csv(Options.TargetDeviceIds),
            ExplicitEdges = ParseEdges(Options.ExplicitEdges),
            MatchMode = Options.MatchMode,
            ConflictPolicy = Options.ConflictPolicy,
            ReconciliationMode = Options.ReconciliationMode,
            MinimumIntervalMinutes = Options.MinimumIntervalMinutes,
            Filter = new JobFilter
            {
                IncludeNamePattern = Options.IncludeNamePattern,
                ExcludeNamePattern = Options.ExcludeNamePattern,
                PatternsAreRegularExpressions = Options.PatternsAreRegularExpressions,
                IncludedStatuses = Csv(Options.IncludedStatuses),
                ExcludedStatuses = Csv(Options.ExcludedStatuses),
                UserId = Options.UserIdFilter > 0 ? Options.UserIdFilter : (long?)null,
                Category = Options.CategoryFilter,
                UnwatchedOnly = Options.UnwatchedOnlyFilter,
                SyncNewContent = Options.SyncNewContentFilter,
                Quality = Options.QualityFilter,
                Profile = Options.ProfileFilter,
                Container = Options.ContainerFilter,
                VideoCodec = Options.VideoCodecFilter,
                AudioCodec = Options.AudioCodecFilter,
                MinimumBitrate = Options.MinimumBitrate > 0 ? Options.MinimumBitrate : (int?)null,
                MaximumBitrate = Options.MaximumBitrate > 0 ? Options.MaximumBitrate : (int?)null,
                MinimumItemLimit = Options.MinimumItemLimit > 0 ? Options.MinimumItemLimit : (int?)null,
                MaximumItemLimit = Options.MaximumItemLimit > 0 ? Options.MaximumItemLimit : (int?)null,
                IncludedItemIds = LongCsv(Options.IncludedItemIds),
                ExcludedItemIds = LongCsv(Options.ExcludedItemIds),
                MaximumAgeDays = Options.MaximumAgeDays > 0 ? Options.MaximumAgeDays : (int?)null,
            },
            Transform = new JobTransform
            {
                NamePrefix = Options.NamePrefix,
                NameSuffix = Options.NameSuffix,
                UserId = Options.OverrideUserId > 0 ? Options.OverrideUserId : (long?)null,
                OverrideUserId = Options.OverrideUserId > 0,
                UnwatchedOnly = Options.OverrideUnwatchedOnly,
                SyncNewContent = Options.OverrideSyncNewContent,
                ItemLimit = Options.OverrideItemLimit > 0 ? Options.OverrideItemLimit : (int?)null,
                Bitrate = Options.OverrideBitrate > 0 ? Options.OverrideBitrate : (int?)null,
                Quality = Options.OverrideQuality,
                Profile = Options.OverrideProfile,
                Container = Options.OverrideContainer,
                VideoCodec = Options.OverrideVideoCodec,
                AudioCodec = Options.OverrideAudioCodec,
            },
            Safety = new RouteSafety
            {
                DryRun = Options.RouteDryRun,
                MaximumCreates = Options.MaximumCreates,
                MaximumDeletes = Options.MaximumManagedDeletes,
                MaximumMutations = Options.MaximumCreates,
                ContinueOnError = Options.ContinueOnError,
            },
        };
        var validation = new Core.Planning.RouteValidator().Validate(route);
        if (validation.Count > 0) throw new InvalidOperationException(string.Join(" ", validation));
        runtime.Repository.SaveRoute(route);
        Populate(route);
    }

    private string RequireSelectedRoute() => !string.IsNullOrWhiteSpace(Options.RouteId)
        ? Options.RouteId
        : throw new InvalidOperationException("Select or save a route first.");

    private static string ResolveId(string itemId, string data) => !string.IsNullOrWhiteSpace(data) ? data : itemId;
    private static IList<string> Csv(string value) => value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private static IList<long> LongCsv(string value) => Csv(value).Select(part => long.TryParse(part, out var parsed) ? parsed : throw new FormatException($"'{part}' is not a valid item ID.")).ToList();
    private static IList<DeviceEdge> ParseEdges(string value) => value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split('>')).Select(parts => parts.Length == 2
        ? new DeviceEdge { SourceDeviceId = parts[0].Trim(), TargetDeviceId = parts[1].Trim() }
        : throw new FormatException("Explicit edges must use source>target, one per line.")).ToList();
}
