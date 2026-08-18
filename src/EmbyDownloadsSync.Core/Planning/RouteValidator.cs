using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EmbyDownloadsSync.Core.Domain;

namespace EmbyDownloadsSync.Core.Planning;

public sealed class RouteValidator
{
    private static readonly TimeSpan PatternTimeout = TimeSpan.FromMilliseconds(500);

    public IList<string> Validate(SyncRoute route, IEnumerable<string>? knownDevices = null)
    {
        if (route == null) throw new ArgumentNullException(nameof(route));
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(route.Id)) errors.Add("Route ID is required.");
        if (string.IsNullOrWhiteSpace(route.Name)) errors.Add("Route name is required.");
        if (route.MinimumIntervalMinutes < 1) errors.Add("Minimum interval must be at least one minute.");
        if (route.Safety.MaximumCreates < 0 || route.Safety.MaximumDeletes < 0 || route.Safety.MaximumMutations < 0)
            errors.Add("Safety limits cannot be negative.");
        if (route.Filter.PatternsAreRegularExpressions)
        {
            ValidateRegularExpression(route.Filter.IncludeNamePattern, "include", errors);
            ValidateRegularExpression(route.Filter.ExcludeNamePattern, "exclude", errors);
        }

        IList<DeviceEdge> edges;
        try
        {
            edges = CompileEdges(route);
        }
        catch (InvalidOperationException exception)
        {
            errors.Add(exception.Message);
            return errors;
        }

        if (edges.Count == 0) errors.Add("Route must contain at least one source-to-target edge.");
        if (edges.Any(edge => string.Equals(edge.SourceDeviceId, edge.TargetDeviceId, StringComparison.OrdinalIgnoreCase)))
            errors.Add("A device cannot synchronize to itself.");

        var known = new HashSet<string>(knownDevices ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        if (known.Count > 0)
        {
            foreach (var deviceId in edges.SelectMany(edge => new[] { edge.SourceDeviceId, edge.TargetDeviceId }).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!known.Contains(deviceId)) errors.Add($"Configured device '{deviceId}' was not found.");
            }
        }

        return errors;
    }

    private static void ValidateRegularExpression(string pattern, string label, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return;
        try { _ = new Regex(pattern, RegexOptions.CultureInvariant, PatternTimeout); }
        catch (ArgumentException exception) { errors.Add($"The {label} regular expression is invalid: {exception.Message}"); }
    }

    public static IList<DeviceEdge> CompileEdges(SyncRoute route)
    {
        var sources = Clean(route.SourceDeviceIds);
        var targets = Clean(route.TargetDeviceIds);
        var result = new List<DeviceEdge>();
        switch (route.Topology)
        {
            case SyncTopology.OneToMany:
                if (sources.Count != 1) throw new InvalidOperationException("One-to-many routes require exactly one source device.");
                result.AddRange(targets.Select(target => Edge(sources[0], target)));
                break;
            case SyncTopology.ManyToOne:
                if (targets.Count != 1) throw new InvalidOperationException("Many-to-one routes require exactly one target device.");
                result.AddRange(sources.Select(source => Edge(source, targets[0])));
                break;
            case SyncTopology.Bidirectional:
                var pair = sources.Concat(targets).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (pair.Count != 2) throw new InvalidOperationException("Bidirectional routes require exactly two distinct devices.");
                result.Add(Edge(pair[0], pair[1]));
                result.Add(Edge(pair[1], pair[0]));
                break;
            case SyncTopology.Mesh:
                var devices = sources.Concat(targets).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                if (devices.Count < 2) throw new InvalidOperationException("Mesh routes require at least two devices.");
                foreach (var source in devices)
                    foreach (var target in devices)
                        if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase)) result.Add(Edge(source, target));
                break;
            case SyncTopology.Explicit:
                result.AddRange(route.ExplicitEdges.Select(edge => Edge(edge.SourceDeviceId, edge.TargetDeviceId)));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(route.Topology));
        }

        return result
            .Where(edge => !string.IsNullOrWhiteSpace(edge.SourceDeviceId) && !string.IsNullOrWhiteSpace(edge.TargetDeviceId))
            .GroupBy(edge => edge.SourceDeviceId + "\u001f" + edge.TargetDeviceId, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToList();
    }

    private static List<string> Clean(IEnumerable<string> values) => values
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => value.Trim())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static DeviceEdge Edge(string source, string target) => new DeviceEdge
    {
        SourceDeviceId = source?.Trim() ?? string.Empty,
        TargetDeviceId = target?.Trim() ?? string.Empty,
    };
}
