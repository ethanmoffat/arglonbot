using arglonbot.Configuration;

using CosineKitty;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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

    public string FindMessageForDateTime(ChannelInfo channelInfo, DateTime time)
    {
        var overrideRules = new List<MessageInfo>(_arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings.ExtraMessages);
        overrideRules.AddRange(channelInfo.Messages);

        var messageRules = new List<MessageInfo>(_arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings.Messages);
        messageRules.AddRange(channelInfo.Messages);

        try
        {
            var message = GetMessage(overrideRules, time, shouldThrow: false);
            if (string.IsNullOrEmpty(message))
            {
                message = GetMessage(messageRules, time, shouldThrow: true)!;
            }
            return message;
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("No matching message rule found for date {date}", time);
            throw;
        }
    }

    private string? GetMessage(List<MessageInfo> messageRules, DateTime time, bool shouldThrow)
    {
        var orderedRules = messageRules
            .OrderBy(x => x.Date ?? DateTime.MaxValue)
            .ThenBy(x => x.DateStart ?? DateTime.MaxValue)
            .ThenBy(x => ((int?)x?.Month ?? short.MaxValue) + ((int?)x?.WeekOfMonth ?? short.MaxValue) + ((int?)x?.DayOfWeek ?? short.MaxValue));

        if (shouldThrow)
        {
            var message = orderedRules.First(x => EvaluateRule(x, time));
            return FormatMessage(message);
        }
        else
        {
            var message = orderedRules.FirstOrDefault(x => EvaluateRule(x, time));
            return message == null ? null : FormatMessage(message);
        }
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

    private string FormatMessage(MessageInfo inputMessage)
    {
        var ret = inputMessage.Message;
        foreach (var replacement in inputMessage.TokenReplacements)
        {
            ret = ret.Replace(
                replacement.Token,
                (replacement.ReplacementType, inputMessage.DateStart.HasValue) switch
                {
                    (MessageInfo.ReplacementType.YearsSinceStart, true) => FormatNth(inputMessage.DateStart!.Value.YearsSince()),
                    (MessageInfo.ReplacementType.MonthsSinceStart, true) => FormatNth(inputMessage.DateStart!.Value.MonthsSince()),
                    (_, _) => replacement.Value
                });
        }
        return ret;

        static string FormatNth(int interval) => (interval % 10) switch
        {
            1 => $"{interval}st",
            2 => $"{interval}nd",
            3 => $"{interval}rd",
            _ => $"{interval}th"
        };
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
    string FindMessageForDateTime(ChannelInfo channelInfo, DateTime time);
}