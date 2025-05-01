using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace arglonbot.Configuration;

public class ArglonBotConfiguration
{
    public const string SectionName = "ArglonBotSettings";

    [Required]
    public required string BotToken { get; set; }

    public LogLevel DiscordLogLevel { get; set; } = LogLevel.Debug;

    [Required]
    [ValidateObjectMembers]
    public required PeriodicOpenMouthSettings PeriodicOpenMouthSettings { get; set; }
}
