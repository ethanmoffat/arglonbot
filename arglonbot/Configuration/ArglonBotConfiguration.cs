using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace arglonbot.Configuration;

[method: SetsRequiredMembers]
public class ArglonBotConfiguration()
{
    public const string SectionName = "ArglonBotSettings";

    [Required]
    public required string BotToken { get; set; } = string.Empty;

    public LogLevel DiscordLogLevel { get; set; } = LogLevel.Debug;

    [Required]
    [ValidateObjectMembers]
    public required PeriodicOpenMouthSettings PeriodicOpenMouthSettings { get; set; } = new();
}
