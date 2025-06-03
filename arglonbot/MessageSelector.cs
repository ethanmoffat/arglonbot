using arglonbot.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using CosineKitty;

using static arglonbot.Configuration.PeriodicOpenMouthSettings;

namespace arglonbot;

public class MessageSelector : IMessageSelector
{
    private readonly ILogger<MessageSelector> _logger;
    private readonly IOptionsMonitor<ArglonBotConfiguration> _arglonBotConfiguration;

    public MessageSelector(ILoggerFactory loggerFactory,
                           IOptionsMonitor<ArglonBotConfiguration> arglonBotConfiguration)
    {
        _logger = loggerFactory.CreateLogger<MessageSelector>();
        _arglonBotConfiguration = arglonBotConfiguration;
    }

    public List<string> FindMessagesForDateTime(ChannelInfo channelInfo, DateTime time)
    {
        var messageRules = new List<MessageInfo>(_arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings.Messages);
        messageRules.AddRange(_arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings.ExtraMessages);
        messageRules.AddRange(channelInfo.Messages);

        List<string> res = [.. messageRules.Where(x => EvaluateRule(x, time)).Select(x => x.Message)];
        if (res.Count == 0)
        {
            _logger.LogError("No matching message rule found for date {date}", time);
            throw new InvalidOperationException("Sequence contains no matching element"); // pretend it's still .Single()
        }

        return res;
    }

    private bool EvaluateRule(MessageInfo info, DateTime time)
    {
        // if all filters are unset, this message matches the time (for default message)
        if (info.Date == null && info.DateStart == null && info.DateEnd == null
            && info.Month == null && info.DayOfWeek == null && info.WeekOfMonth == null
            && info.After == null)
            return true;

        if (info.Date != null)
        {
            return time.Date.Equals(info.Date.Value.Date);
        }

        if (info.DateStart != null && info.DateEnd != null)
        {
            var isInRange = time >= info.DateStart.Value.Date && time <= info.DateEnd.Value.Date;
            if (isInRange && info.After != null)
            {
                var start = info.DateStart.Value;
                var end = info.DateEnd.Value;
                return EvaluateAter(time, start, end, info.After);
            }
            else
            {
                return isInRange;
            }
        }

        if (info.Month != null && info.DayOfWeek != null && info.WeekOfMonth != null)
        {
            return (Month)time.Month == info.Month.Value
                && time.DayOfWeek == info.DayOfWeek.Value
                && DoesWeekOfMonthMatch(info.WeekOfMonth.Value, time);
        }

        return false;
    }

    private bool EvaluateAter(DateTime time, DateTime start, DateTime end, MessageInfo.AfterInfo afterInfo)
    {
        switch (afterInfo.Type)
        {
            case MessageInfo.AfterType.MoonPhase:
                {
                    var astroTime = new AstroTime(start);
                    var firstMatch = Astronomy.MoonQuartersAfter(astroTime).Take(10)
                        .OrderBy(x => x.time.ToUtcDateTime())
                        .FirstOrDefault(x => PhaseName(x.quarter) == afterInfo.Value);

                    if (firstMatch.Equals(default(MoonQuarterInfo)))
                    {
                        _logger.LogWarning("Phase {value} was not found when calculating moon phases in interval [{start}, {end}]", afterInfo.Value, start, end);
                        return false;
                    }

                    var moonEventTime = firstMatch.time.ToUtcDateTime();
                    if (time > moonEventTime)
                    {
                        var searchFromTime = moonEventTime.DayOfWeek == time.DayOfWeek
                            ? moonEventTime.AddDays(7)
                            : moonEventTime;

                        return afterInfo.DayOfWeek == time.DayOfWeek && DoesWeekOfIntervalMatch(afterInfo.WeekOfInterval, time.Date - searchFromTime.Date);
                    }

                    return false;
                }
            default: return false;
        }
    }

    private static bool DoesWeekOfMonthMatch(WeekOfMonth weekOfMonth, DateTime time) => weekOfMonth switch
    {
        WeekOfMonth.Last => DateTime.DaysInMonth(time.Year, time.Month) - time.Day <= 7,
        _ => (WeekOfMonth)(time.Day / 7) == weekOfMonth,
    };

    private static bool DoesWeekOfIntervalMatch(WeekOfMonth weekOfMonth, TimeSpan howManyDays) =>
        weekOfMonth != WeekOfMonth.Last && (int)weekOfMonth == (int)(howManyDays.TotalDays / 7);

    private static string PhaseName(int quarter) => quarter switch
    {
        0 => "NewMoon",
        1 => "FirstQuarter",
        2 => "FullMoon",
        3 => "ThirdQuarter",
        _ => throw new ArgumentOutOfRangeException(nameof(quarter)),
    };
}

public interface IMessageSelector
{
    List<string> FindMessagesForDateTime(ChannelInfo channelInfo, DateTime time);
}