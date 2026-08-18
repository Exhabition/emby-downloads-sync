using System.ComponentModel;
using Emby.Web.GenericEdit;
using Emby.Web.GenericEdit.Validation;
using MediaBrowser.Model.Attributes;

namespace EmbyDownloadsSync.Plugin.Configuration;

public sealed class PluginOptions : EditableOptionsBase
{
    public override string EditorTitle => "Emby Downloads Sync settings";

    public override string EditorDescription => "Configure authentication and global safety defaults. Routes are managed from the Downloads Sync dashboard.";

    [DisplayName("Enable scheduled synchronization")]
    public bool Enabled { get; set; } = true;

    [DisplayName("Emby administrator API key")]
    [Description("Used only for loopback calls to Emby's sync-job endpoints. The key is never logged.")]
    [IsPassword]
    public string ApiKey { get; set; } = string.Empty;

    [DisplayName("Global dry-run mode")]
    [Description("Build and display plans without creating download jobs.")]
    public bool DryRun { get; set; } = true;

    [IsAdvanced]
    [DisplayName("Allow managed cleanup")]
    [Description("Reserved for verified Emby job deletion support. User-created jobs are never deleted.")]
    public bool AllowManagedCleanup { get; set; }

    [IsAdvanced]
    [DisplayName("Default maximum creates per route")]
    public int DefaultMaximumCreates { get; set; } = 100;

    [IsAdvanced]
    [DisplayName("Maximum retained run summaries")]
    public int RetainedRunCount { get; set; } = 50;

    [IsAdvanced]
    [DisplayName("HTTP timeout (seconds)")]
    public int HttpTimeoutSeconds { get; set; } = 30;

    protected override void Validate(ValidationContext context)
    {
        if (DefaultMaximumCreates < 0 || DefaultMaximumCreates > 10_000)
            context.AddValidationError(nameof(DefaultMaximumCreates), "Maximum creates must be between 0 and 10,000.");
        if (RetainedRunCount < 1 || RetainedRunCount > 500)
            context.AddValidationError(nameof(RetainedRunCount), "Retained run count must be between 1 and 500.");
        if (HttpTimeoutSeconds < 5 || HttpTimeoutSeconds > 300)
            context.AddValidationError(nameof(HttpTimeoutSeconds), "HTTP timeout must be between 5 and 300 seconds.");
    }
}
