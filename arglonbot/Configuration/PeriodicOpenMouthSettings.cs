using System.ComponentModel.DataAnnotations;

namespace arglonbot.Configuration;

public class PeriodicOpenMouthSettings
{
    public record ChannelInfo(
        [Required] string Name,
        [Required] ulong GuildId,
        [Required] ulong ChannelId);

    public record MessageInfo(
        [Required] string Message,
        DateTime? Date = default,
        DateTime? DateStart = default,
        DateTime? DateEnd = default,
        Month? Month = default,
        DayOfWeek? DayOfWeek = default,
        WeekOfMonth? WeekOfMonth = default,
        MessageInfo.AfterInfo? After = null)
    {
        public record class AfterInfo(
            [Required] AfterType Type,
            [Required] string Value,
            [Required] DayOfWeek DayOfWeek,
            [Required] WeekOfMonth WeekOfInterval);

        public enum AfterType
        {
            None,
            MoonPhase
        }
    }

    public enum Month
    {
        January = 1,
        February,
        March,
        April,
        May,
        June,
        July,
        August,
        September,
        October,
        November,
        December
    }

    [Required]
    public TimeSpan NotificationTime { get; set; }

    public string NotificationTimeZone { get; set; } = "Pacific Standard Time";

    [Required]
    public TimeSpan NotificationInterval { get; set; }

    public required List<ChannelInfo> Channels { get; set; } = [];

    public required List<MessageInfo> Messages { get; set; } = [];
}