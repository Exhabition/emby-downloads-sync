using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Plugin.Configuration;
using MediaBrowser.Model.Serialization;

namespace EmbyDownloadsSync.Plugin.Persistence;

internal sealed class PluginRepository
{
    private readonly object sync = new object();
    private readonly IJsonSerializer serializer;
    private readonly JsonFileStore<RouteCollection> routeStore;
    private readonly JsonFileStore<PluginState> stateStore;

    public PluginRepository(IJsonSerializer serializer, string dataFolderPath)
    {
        this.serializer = serializer;
        routeStore = new JsonFileStore<RouteCollection>(serializer, Path.Combine(dataFolderPath, "routes.json"));
        stateStore = new JsonFileStore<PluginState>(serializer, Path.Combine(dataFolderPath, "state.json"));
    }

    public IList<SyncRoute> GetRoutes()
    {
        lock (sync) return routeStore.Get().Routes.Select(Clone).ToList();
    }

    public void SaveRoute(SyncRoute route)
    {
        lock (sync)
        {
            var collection = routeStore.Get();
            var existing = collection.Routes.FirstOrDefault(value => string.Equals(value.Id, route.Id, StringComparison.OrdinalIgnoreCase));
            if (existing != null) collection.Routes.Remove(existing);
            collection.Routes.Add(Clone(route));
            routeStore.Set(collection);
        }
    }

    public bool DeleteRoute(string routeId)
    {
        lock (sync)
        {
            var collection = routeStore.Get();
            var removed = collection.Routes.FirstOrDefault(value => string.Equals(value.Id, routeId, StringComparison.OrdinalIgnoreCase));
            if (removed == null) return false;
            collection.Routes.Remove(removed);
            routeStore.Set(collection);
            return true;
        }
    }

    public PluginState GetState()
    {
        lock (sync) return Clone(stateStore.Get());
    }

    public void RecordRun(SyncRunSummary run, IEnumerable<string> routeIds, int retainedRuns)
    {
        lock (sync)
        {
            var state = stateStore.Get();
            state.Runs.Insert(0, Clone(run));
            while (state.Runs.Count > retainedRuns) state.Runs.RemoveAt(state.Runs.Count - 1);
            foreach (var routeId in routeIds) state.LastRunByRoute[routeId] = run.FinishedUtc;
            stateStore.Set(state);
        }
    }

    public void RecordManagedJob(ManagedJobRecord record)
    {
        lock (sync)
        {
            var state = stateStore.Get();
            state.ManagedJobs.Add(Clone(record));
            stateStore.Set(state);
        }
    }

    private T Clone<T>(T value) where T : class =>
        serializer.DeserializeFromString<T>(serializer.SerializeToString(value))
        ?? throw new InvalidOperationException($"Unable to clone persisted {typeof(T).Name} data.");
}
