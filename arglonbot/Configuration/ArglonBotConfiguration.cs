using System.ComponentModel.DataAnnotations;

using Microsoft.Extensions.Logging;

namespace arglonbot.Configuration
{
    public class ArglonBotConfiguration
    {
        public const string SectionName = "ArglonBot";

        [Required]
        public required string BotToken { get; set; }

        public LogLevel DiscordLogLevel { get; set; } = LogLevel.Debug;
    }
}
