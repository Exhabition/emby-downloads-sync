using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using EmbyDownloadsSync.Plugin.Api;
using EmbyDownloadsSync.Plugin.Configuration;
using EmbyDownloadsSync.Plugin.Integration;
using EmbyDownloadsSync.Plugin.Tasks;
using EmbyDownloadsSync.Plugin.UI;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Plugins.UI;
using MediaBrowser.Model.Attributes;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Tasks;

namespace EmbyDownloadsSync.Tests;

public sealed class PluginContractTests
{
    [Fact]
    public void PluginImplementsOfficialConfigurationAndUiContracts()
    {
        var type = typeof(EmbyDownloadsSync.Plugin.Plugin);
        Assert.True(typeof(BasePlugin).IsAssignableFrom(type));
        Assert.True(typeof(IHasUIPages).IsAssignableFrom(type));
        Assert.True(typeof(IHasPluginConfiguration).IsAssignableFrom(type));
    }

    [Fact]
    public void DiscoveryContractsArePublicAndConstructible()
    {
        Assert.True(typeof(IScheduledTask).IsAssignableFrom(typeof(SynchronizeDownloadsTask)));
        Assert.True(typeof(IServerEntryPoint).IsAssignableFrom(typeof(PluginEntryPoint)));
        Assert.True(typeof(IService).IsAssignableFrom(typeof(SyncApiService)));
        Assert.NotNull(typeof(PluginEntryPoint).GetConstructor(Type.EmptyTypes));
        Assert.NotNull(typeof(SyncApiService).GetConstructor(Type.EmptyTypes));
    }

    [Fact]
    public void ScheduledTaskDefaultsToFifteenMinutes()
    {
        var trigger = Assert.Single(new SynchronizeDownloadsTask().GetDefaultTriggers());
        Assert.Equal("IntervalTrigger", trigger.Type);
        Assert.Equal(TimeSpan.FromMinutes(15).Ticks, trigger.IntervalTicks);
    }

    [Fact]
    public void RouteEditorKeepsTheEverydayWorkflowSimple()
    {
        AssertSimple<DashboardPageOptions>(
            nameof(DashboardPageOptions.RouteName),
            nameof(DashboardPageOptions.RouteEnabled),
            nameof(DashboardPageOptions.SourceDeviceIds),
            nameof(DashboardPageOptions.TargetDeviceIds));
        AssertAdvanced<DashboardPageOptions>(
            nameof(DashboardPageOptions.RouteId),
            nameof(DashboardPageOptions.Topology),
            nameof(DashboardPageOptions.MatchMode),
            nameof(DashboardPageOptions.IncludeNamePattern),
            nameof(DashboardPageOptions.OverrideQuality),
            nameof(DashboardPageOptions.MaximumCreates));
    }

    [Fact]
    public void GlobalTuningOptionsAreAdvanced()
    {
        AssertSimple<PluginOptions>(nameof(PluginOptions.Enabled), nameof(PluginOptions.ApiKey), nameof(PluginOptions.DryRun));
        AssertAdvanced<PluginOptions>(
            nameof(PluginOptions.AllowManagedCleanup),
            nameof(PluginOptions.DefaultMaximumCreates),
            nameof(PluginOptions.RetainedRunCount),
            nameof(PluginOptions.HttpTimeoutSeconds));
    }

    [Fact]
    public void EveryApiRequestRequiresAdministratorAuthentication()
    {
        var requestTypes = typeof(GetSyncRoutes).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(GetSyncRoutes).Namespace && type.GetCustomAttributes(false).Any(attribute => attribute.GetType().Name == "RouteAttribute"))
            .ToArray();
        Assert.NotEmpty(requestTypes);
        Assert.All(requestTypes, type => Assert.Contains(type.GetCustomAttributes(false), attribute =>
            attribute is AuthenticatedAttribute authenticated && authenticated.Roles == "Admin"));
    }

    [Fact]
    public void DeployablePluginContainsCoreAndHasNoPluginOwnedReference()
    {
        var deployablePlugin = GetDeployablePluginPath();
        Assert.True(File.Exists(deployablePlugin));
        using var stream = File.OpenRead(deployablePlugin);
        using var reader = new PEReader(stream);
        var metadata = reader.GetMetadataReader();
        var references = metadata.AssemblyReferences.Select(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name)).ToArray();
        Assert.DoesNotContain("EmbyDownloadsSync.Core", references);
    }

    private static string GetDeployablePluginPath()
    {
        var configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        return Path.Combine(root, "src", "EmbyDownloadsSync.Plugin", "bin", configuration, "netstandard2.0", "deploy", "EmbyDownloadsSync.dll");
    }

    private static void AssertAdvanced<T>(params string[] propertyNames) => Assert.All(propertyNames, propertyName =>
        Assert.NotNull(typeof(T).GetProperty(propertyName)!.GetCustomAttributes(typeof(IsAdvancedAttribute), true).SingleOrDefault()));

    private static void AssertSimple<T>(params string[] propertyNames) => Assert.All(propertyNames, propertyName =>
        Assert.Empty(typeof(T).GetProperty(propertyName)!.GetCustomAttributes(typeof(IsAdvancedAttribute), true)));
}
