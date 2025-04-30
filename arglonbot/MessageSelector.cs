using arglonbot.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SquareWidget.Astronomy.Core.Calculators;
using SquareWidget.Astronomy.Core.Models;

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

    public string FindMessageForDateTime(DateTime time)
    {
        var messageRules = _arglonBotConfiguration.CurrentValue.PeriodicOpenMouthSettings.Messages;
        try
        {
            return messageRules.First(x => EvaluateRule(x, time)).Message;
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("No matching message rule found for date {date}", time);
            throw;
        }
    }

    private bool EvaluateRule(PeriodicOpenMouthSettings.MessageInfo info, DateTime time)
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
                var interval = new DateRange(
                    new DateOnly(start.Year, start.Month, start.Day),
                    new DateOnly(end.Year, end.Month, end.Day)
                );
                return EvaluateAter(time, interval, info.After);
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
                && time.GetWeekOfMonth() == info.WeekOfMonth.Value;
        }

        return false;
    }

    private bool EvaluateAter(DateTime time, DateRange interval, MessageInfo.AfterInfo afterInfo)
    {
        switch (afterInfo.Type)
        {
            case MessageInfo.AfterType.MoonPhase:
                {
                    var phasesInInterval = MoonPhaseDatesCalculator.Calculate(interval);
                    var firstMatch = phasesInInterval.FirstOrDefault(x => x.PhaseName.Equals(afterInfo.Value, StringComparison.InvariantCultureIgnoreCase));
                    if (firstMatch == null)
                    {
                        _logger.LogWarning("Phase {value} was not found when calculating moon phases in interval [{start}, {end}]", afterInfo.Value, interval.StartDate, interval.EndDate);
                        return false;
                    }

                    // note: already confirmed that 'time' is within the interval
                    var weekOfInterval = (WeekOfMonth)(time.GetWeekOfYear() - interval.StartDate.ToDateTime(TimeOnly.MinValue).GetWeekOfYear());

                    return time > firstMatch.Moment.ToDateTime()
                        && afterInfo.DayOfWeek == time.DayOfWeek
                        && afterInfo.WeekOfInterval == weekOfInterval;
                }
            default: return false;
        }
    }
}

public interface IMessageSelector
{
    string FindMessageForDateTime(DateTime time);
}