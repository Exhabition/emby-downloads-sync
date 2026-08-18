using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Persistence;

namespace EmbyDownloadsSync.Tests;

public sealed class PluginRepositoryTests
{
    [Fact]
    public void ReturnedPersistenceModelsCannotMutateTheRepositoryCache()
    {
        var directory = Path.Combine(Path.GetTempPath(), "emby-downloads-sync-repository-" + Guid.NewGuid().ToString("N"));
        try
        {
            var repository = new PluginRepository(new SyncJobGatewayTests.TestJsonSerializer(), directory);
            repository.SaveRoute(new SyncRoute
            {
                Id = "route",
                Name = "Original",
                SourceDeviceIds = ["phone"],
                TargetDeviceIds = ["tablet"],
            });

            var returnedRoute = Assert.Single(repository.GetRoutes());
            returnedRoute.Name = "Mutated";
            returnedRoute.TargetDeviceIds.Add("laptop");
            var returnedState = repository.GetState();
            returnedState.Runs.Add(new SyncRunSummary());

            var storedRoute = Assert.Single(repository.GetRoutes());
            Assert.Equal("Original", storedRoute.Name);
            Assert.Equal(["tablet"], storedRoute.TargetDeviceIds);
            Assert.Empty(repository.GetState().Runs);
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
