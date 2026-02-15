using Microsoft.Extensions.Logging;

namespace EmbyDownloadsSync.Infrastructure.Services;

public partial class SyncService
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Starting Emby download sync...")]
    private static partial void LogStartingSync(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error during sync")]
    private static partial void LogSyncError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validating devices...")]
    private static partial void LogValidatingDevices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found Device: Id: {DeviceId} | Name: {Name} | Last Used: {LastActivity}")]
    private static partial void LogFoundDevice(ILogger logger, string? deviceId, string? name, DateTimeOffset? lastActivity);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Missing device: {DeviceId}")]
    private static partial void LogMissingDevice(ILogger logger, string deviceId);

    [LoggerMessage(Level = LogLevel.Information, Message = "All configured devices are valid")]
    private static partial void LogAllDevicesValid(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting sync cycle...")]
    private static partial void LogStartingSyncCycle(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job {JobName} is in failed state, skipping")]
    private static partial void LogJobFailed(ILogger logger, string? jobName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Job {JobName} already exists, skipping")]
    private static partial void LogJobExists(ILogger logger, string? jobName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Creating missing job on {TargetId}: {JobName} (Items: {ItemCount}, UnwatchedOnly: {UnwatchedOnly})")]
    private static partial void LogCreatingJob(ILogger logger, string targetId, string? jobName, int? itemCount, bool? unwatchedOnly);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to create job {JobName} on target device {TargetId}")]
    private static partial void LogJobCreationFailed(ILogger logger, string? jobName, string targetId, Exception ex);
}
