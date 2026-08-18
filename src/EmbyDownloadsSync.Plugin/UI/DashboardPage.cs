using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Common;
using Emby.Web.GenericEdit.Elements;
using Emby.Web.GenericEdit.Elements.List;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Persistence;
using EmbyDownloadsSync.Plugin.Services;
using EmbyDownloadsSync.Plugin.UI.Infrastructure;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI.Views;

namespace EmbyDownloadsSync.Plugin.UI;

public sealed class DashboardPageOptions : EditableOptionsBase
{
    public override string EditorTitle => "Downloads Sync";
    public override string EditorDescription => "Choose where downloads should be copied. The default settings are suitable for most routes; use Show advanced settings for more control.";

    public StatusItem Status { get; set; } = new StatusItem("Synchronization", "Ready", ItemStatus.None);
    public ButtonItem PreviewAll { get; set; } = new ButtonItem("Preview all routes") { CommandId = "PreviewAll", Icon = IconNames.preview };
    public ButtonItem ApplyAll { get; set; } = new ButtonItem("Apply all routes") { CommandId = "ApplyAll", Icon = IconNames.sync };
    public ButtonItem Refresh { get; set; } = new ButtonItem("Refresh") { CommandId = "Refresh", Icon = IconNames.refresh };
    public CaptionItem RoutesCaption { get; set; } = new CaptionItem("Routes");
    public GenericItemList Routes { get; set; } = new GenericItemList();
    public CaptionItem EditorCaption { get; set; } = new CaptionItem("Copy downloads between devices");

    [IsAdvanced]
    [DisplayName("Route ID")]
    [Description("Leave blank to create a route. Select a route above to edit it.")]
    public string RouteId { get; set; } = string.Empty;
    [DisplayName("Name this setup")]
    [Description("For example: Phone downloads to tablet.")]
    public string RouteName { get; set; } = "New route";
    [DisplayName("Keep this setup active")]
    public bool RouteEnabled { get; set; } = true;
    [DisplayName("Copy downloads from")]
    [Description("Select the device whose downloads should be copied.")]
    [SelectItemsSource(nameof(DeviceOptions))]
    [EditMultilSelect]
    public string SourceDeviceIds { get; set; } = string.Empty;
    [DisplayName("Copy downloads to")]
    [Description("Select one or more destination devices.")]
    [SelectItemsSource(nameof(DeviceOptions))]
    [EditMultilSelect]
    public string TargetDeviceIds { get; set; } = string.Empty;
    [Browsable(false)]
    public List<EditorSelectOption> DeviceOptions { get; set; } = new List<EditorSelectOption>();

    public ButtonItem NewRoute { get; set; } = new ButtonItem("New route") { CommandId = "NewRoute", Icon = IconNames.add };
    public ButtonItem SaveRoute { get; set; } = new ButtonItem("Save route") { CommandId = "SaveRoute", Icon = IconNames.save };
    public ButtonItem PreviewRoute { get; set; } = new ButtonItem("Preview route") { CommandId = "PreviewRoute", Icon = IconNames.preview };
    public ButtonItem ApplyRoute { get; set; } = new ButtonItem("Apply route") { CommandId = "ApplyRoute", Icon = IconNames.sync };
    public CaptionItem ResultsCaption { get; set; } = new CaptionItem("Latest run");
    public string LatestRun { get; set; } = "No runs yet.";

    [IsAdvanced]
    public CaptionItem AdvancedRouteCaption { get; set; } = new CaptionItem("Advanced route settings");
    [IsAdvanced]
    [DisplayName("Device arrangement")]
    [Description("Controls the direction downloads travel. One source to one or more destinations is the normal choice.")]
    public SyncTopology Topology { get; set; } = SyncTopology.OneToMany;
    [IsAdvanced]
    [DisplayName("Custom device connections")]
    [Description("Only used when device arrangement is Custom connections. Enter one source>destination connection per line.")]
    [EditMultiline(6)]
    public string ExplicitEdges { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Matching")]
    public JobMatchMode MatchMode { get; set; } = JobMatchMode.Exact;
    [IsAdvanced]
    [DisplayName("Conflict policy")]
    public ConflictPolicy ConflictPolicy { get; set; } = ConflictPolicy.KeepTarget;
    [IsAdvanced]
    [DisplayName("Reconciliation")]
    public ReconciliationMode ReconciliationMode { get; set; } = ReconciliationMode.CreateOnly;
    [IsAdvanced]
    [DisplayName("Include job names")]
    [Description("Glob by default; for example Travel*. Leave empty for all jobs.")]
    public string IncludeNamePattern { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Exclude job names")]
    public string ExcludeNamePattern { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Patterns are regular expressions")]
    public bool PatternsAreRegularExpressions { get; set; }
    [IsAdvanced]
    [DisplayName("Included statuses")]
    public string IncludedStatuses { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Excluded statuses")]
    public string ExcludedStatuses { get; set; } = "Failed";
    [IsAdvanced]
    [DisplayName("User ID filter")]
    public long UserIdFilter { get; set; }
    [IsAdvanced]
    [DisplayName("Category filter")]
    public string CategoryFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Unwatched-only filter")]
    public bool? UnwatchedOnlyFilter { get; set; }
    [IsAdvanced]
    [DisplayName("Sync-new-content filter")]
    public bool? SyncNewContentFilter { get; set; }
    [IsAdvanced]
    [DisplayName("Quality filter")]
    public string QualityFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Profile filter")]
    public string ProfileFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Container filter")]
    public string ContainerFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Video codec filter")]
    public string VideoCodecFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Audio codec filter")]
    public string AudioCodecFilter { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Minimum bitrate")]
    public int MinimumBitrate { get; set; }
    [IsAdvanced]
    [DisplayName("Maximum bitrate")]
    public int MaximumBitrate { get; set; }
    [IsAdvanced]
    [DisplayName("Minimum item limit")]
    public int MinimumItemLimit { get; set; }
    [IsAdvanced]
    [DisplayName("Maximum item limit")]
    public int MaximumItemLimit { get; set; }
    [IsAdvanced]
    [DisplayName("Include item IDs")]
    public string IncludedItemIds { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Exclude item IDs")]
    public string ExcludedItemIds { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Maximum job age in days")]
    public int MaximumAgeDays { get; set; }
    [IsAdvanced]
    [DisplayName("Name prefix")]
    public string NamePrefix { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Name suffix")]
    public string NameSuffix { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Override user ID")]
    [Description("Set to 0 to preserve the source user.")]
    public long OverrideUserId { get; set; }
    [IsAdvanced]
    [DisplayName("Override unwatched-only")]
    public bool? OverrideUnwatchedOnly { get; set; }
    [IsAdvanced]
    [DisplayName("Override sync-new-content")]
    public bool? OverrideSyncNewContent { get; set; }
    [IsAdvanced]
    [DisplayName("Override item limit")]
    public int OverrideItemLimit { get; set; }
    [IsAdvanced]
    [DisplayName("Override bitrate")]
    public int OverrideBitrate { get; set; }
    [IsAdvanced]
    [DisplayName("Override quality")]
    public string OverrideQuality { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Override profile")]
    public string OverrideProfile { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Override container")]
    public string OverrideContainer { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Override video codec")]
    public string OverrideVideoCodec { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Override audio codec")]
    public string OverrideAudioCodec { get; set; } = string.Empty;
    [IsAdvanced]
    [DisplayName("Minimum interval in minutes")]
    public int MinimumIntervalMinutes { get; set; } = 15;
    [IsAdvanced]
    [DisplayName("Route dry run")]
    public bool RouteDryRun { get; set; }
    [IsAdvanced]
    [DisplayName("Maximum creates")]
    public int MaximumCreates { get; set; } = 100;
    [IsAdvanced]
    [DisplayName("Maximum managed deletions")]
    [Description("Stored for managed-exact routes. Deletion remains gated until Emby's contract is verified.")]
    public int MaximumManagedDeletes { get; set; }
    [IsAdvanced]
    [DisplayName("Continue after individual errors")]
    public bool ContinueOnError { get; set; } = true;

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

    public override async Task<IPluginUIView> CreateDefaultPageView()
    {
        var view = new DashboardPageView(pluginInfo.Id, runtime);
        await view.LoadAsync().ConfigureAwait(false);
        return view;
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

    public async Task LoadAsync()
    {
        var routes = runtime.Repository.GetRoutes();
        IList<DeviceDescriptor> devices;
        string? deviceWarning = null;
        try
        {
            devices = await runtime.DeviceCatalog.GetDevicesAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            runtime.Logger.ErrorException("Unable to load Emby sync devices", exception);
            devices = new List<DeviceDescriptor>();
            deviceWarning = "Device names could not be refreshed. Saved device IDs remain available.";
        }
        Options.DeviceOptions = BuildDeviceOptions(devices, routes);
        var rows = routes.Select(route => new GenericListItem
        {
            Icon = IconNames.sync,
            Status = route.Enabled ? ItemStatus.Succeeded : ItemStatus.None,
            PrimaryText = route.Name,
            SecondaryText = DescribeRoute(route),
            Button1 = new ButtonItem("Edit") { CommandId = "SelectRoute", ItemData = route.Id, Data1 = route.Id },
            Button2 = new ButtonItem("Delete") { CommandId = "DeleteRoute", ItemData = route.Id, Data1 = route.Id },
        }).ToList();
        Options.Routes = new GenericItemList(rows);
        var latest = runtime.Repository.GetState().Runs.FirstOrDefault();
        Options.LatestRun = latest == null
            ? "No runs yet."
            : $"{latest.FinishedUtc:u} · {(latest.Preview ? "preview" : "apply")} · planned {latest.PlannedCreates}, created {latest.Created}, dry-run {latest.DryRunSkipped}, skipped {latest.Skipped}, conflicts {latest.Conflicts}, failed {latest.Failed}";
        Options.Status.Status = deviceWarning == null ? ItemStatus.Succeeded : ItemStatus.Warning;
        Options.Status.StatusText = deviceWarning ?? $"{rows.Count} route(s) configured.";
    }

    public override async Task<IPluginUIView> RunCommand(string itemId, string commandId, string data)
    {
        try
        {
            switch (commandId)
            {
                case "Refresh": await LoadAsync().ConfigureAwait(false); break;
                case "NewRoute": Populate(new SyncRoute { Safety = { MaximumCreates = runtime.OptionsStore.Get().DefaultMaximumCreates } }); break;
                case "SelectRoute": Select(ResolveId(itemId, data)); break;
                case "DeleteRoute": runtime.Repository.DeleteRoute(ResolveId(itemId, data)); await LoadAsync().ConfigureAwait(false); break;
                case "SaveRoute": Save(); await LoadAsync().ConfigureAwait(false); break;
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
        await LoadAsync().ConfigureAwait(false);
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
    internal static List<EditorSelectOption> BuildDeviceOptions(IEnumerable<DeviceDescriptor> devices, IEnumerable<SyncRoute> routes)
    {
        var choices = devices
            .Where(device => !string.IsNullOrWhiteSpace(device.Id))
            .GroupBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(device => device.LastActivityUtc).First())
            .ToDictionary(device => device.Id, StringComparer.OrdinalIgnoreCase);
        var configuredIds = routes
            .SelectMany(route => route.SourceDeviceIds.Concat(route.TargetDeviceIds)
                .Concat(route.ExplicitEdges.SelectMany(edge => new[] { edge.SourceDeviceId, edge.TargetDeviceId })))
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var id in configuredIds)
            if (!choices.ContainsKey(id))
                choices[id] = new DeviceDescriptor { Id = id, Name = "Unknown device" };

        return choices.Values
            .OrderBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(device => device.Id, StringComparer.OrdinalIgnoreCase)
            .Select(device => new EditorSelectOption
            {
                Value = device.Id,
                Name = $"{DisplayDeviceName(device)} ({device.Id})",
                ShortName = DisplayDeviceName(device),
                ToolTip = device.LastActivityUtc.HasValue
                    ? $"Last active {device.LastActivityUtc.Value:u}"
                    : $"Device ID: {device.Id}",
                IsEnabled = true,
            })
            .ToList();
    }

    private static string DisplayDeviceName(DeviceDescriptor device) =>
        string.IsNullOrWhiteSpace(device.Name) ? "Unnamed device" : device.Name.Trim();

    private string DescribeRoute(SyncRoute route)
    {
        var namesById = Options.DeviceOptions.ToDictionary(option => option.Value, option => option.Name, StringComparer.OrdinalIgnoreCase);
        var sources = string.Join(", ", route.SourceDeviceIds.Select(id => namesById.TryGetValue(id, out var name) ? name : id));
        var targets = string.Join(", ", route.TargetDeviceIds.Select(id => namesById.TryGetValue(id, out var name) ? name : id));
        switch (route.Topology)
        {
            case SyncTopology.Bidirectional: return $"{sources} ↔ {targets}";
            case SyncTopology.Mesh: return $"Keep downloads in sync across {sources}";
            case SyncTopology.Explicit: return $"{route.ExplicitEdges.Count} custom device connection(s)";
            default: return $"{sources} → {targets}";
        }
    }
    private static IList<string> Csv(string value) => value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(part => part.Trim()).Where(part => part.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private static IList<long> LongCsv(string value) => Csv(value).Select(part => long.TryParse(part, out var parsed) ? parsed : throw new FormatException($"'{part}' is not a valid item ID.")).ToList();
    private static IList<DeviceEdge> ParseEdges(string value) => value.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(line => line.Split('>')).Select(parts => parts.Length == 2
        ? new DeviceEdge { SourceDeviceId = parts[0].Trim(), TargetDeviceId = parts[1].Trim() }
        : throw new FormatException("Explicit edges must use source>target, one per line.")).ToList();
}
