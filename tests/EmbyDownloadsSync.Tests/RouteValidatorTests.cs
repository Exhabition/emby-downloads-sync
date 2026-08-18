using EmbyDownloadsSync.Core.Domain;
using EmbyDownloadsSync.Core.Planning;

namespace EmbyDownloadsSync.Tests;

public sealed class RouteValidatorTests
{
    [Fact]
    public void OneToManyCompilesOneEdgePerTarget()
    {
        var route = Route(SyncTopology.OneToMany, ["phone"], ["tablet", "laptop"]);

        var edges = RouteValidator.CompileEdges(route);

        Assert.Collection(edges,
            edge => Assert.Equal(("phone", "tablet"), (edge.SourceDeviceId, edge.TargetDeviceId)),
            edge => Assert.Equal(("phone", "laptop"), (edge.SourceDeviceId, edge.TargetDeviceId)));
    }

    [Fact]
    public void BidirectionalCompilesBothDirections()
    {
        var edges = RouteValidator.CompileEdges(Route(SyncTopology.Bidirectional, ["phone"], ["tablet"]));

        Assert.Equal(2, edges.Count);
        Assert.Contains(edges, edge => edge.SourceDeviceId == "phone" && edge.TargetDeviceId == "tablet");
        Assert.Contains(edges, edge => edge.SourceDeviceId == "tablet" && edge.TargetDeviceId == "phone");
    }

    [Fact]
    public void MeshCompilesEveryDirectedPair()
    {
        var edges = RouteValidator.CompileEdges(Route(SyncTopology.Mesh, ["one", "two", "three"], []));

        Assert.Equal(6, edges.Count);
        Assert.DoesNotContain(edges, edge => edge.SourceDeviceId == edge.TargetDeviceId);
    }

    [Fact]
    public void ValidationReportsUnknownAndSelfTargetDevices()
    {
        var route = Route(SyncTopology.Explicit, [], []);
        route.ExplicitEdges = [new DeviceEdge { SourceDeviceId = "missing", TargetDeviceId = "missing" }];

        var errors = new RouteValidator().Validate(route, ["known"]);

        Assert.Contains(errors, value => value.Contains("itself"));
        Assert.Contains(errors, value => value.Contains("was not found"));
    }

    private static SyncRoute Route(SyncTopology topology, string[] sources, string[] targets) => new SyncRoute
    {
        Id = "route",
        Name = "Route",
        Topology = topology,
        SourceDeviceIds = sources,
        TargetDeviceIds = targets,
    };
}
