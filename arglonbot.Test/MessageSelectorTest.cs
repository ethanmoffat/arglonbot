using arglonbot.Configuration;

using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using static arglonbot.Configuration.PeriodicOpenMouthSettings;
using static arglonbot.Configuration.PeriodicOpenMouthSettings.MessageInfo;
using System.Collections;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace arglonbot.Test;

[TestFixture]
public class MessageSelectorTest
{
    private readonly List<MessageInfo> _messages = [];

    [SetUp]
    public void SetUp()
    {
        _messages.Clear();
    }

    [Test]
    public void TestDefaultRule()
    {
        const string WrongMessage = "Happy Birthday!";
        const string ExpectedMessage = "12345";

        var messageDate = DateTime.Parse("March 10");
        AddExactDateRule(messageDate, WrongMessage);
        AddDefaultRule(ExpectedMessage);

        var messageSelector = CreateSelector();

        var testDate = DateTime.Parse("January 1");
        Assert.That(messageSelector.FindMessageForDateTime(testDate), Is.EqualTo(ExpectedMessage));
    }

    [Test]
    public void TestExactDate()
    {
        const string Message = "Blaze itttt";

        var date = DateTime.Parse("April 20");
        AddExactDateRule(date, Message);
        AddDefaultRule("12345");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(date), Is.EqualTo(Message));
    }

    [Test]
    public void TestNoMatchingRuleThrows()
    {
        const string Message = "Blaze itttt";

        var messageDate = DateTime.Parse("April 20");
        AddExactDateRule(messageDate, Message);

        var messageSelector = CreateSelector();

        var testDate = DateTime.Parse("January 1");
        Assert.That(() => messageSelector.FindMessageForDateTime(testDate), Throws.InvalidOperationException);
    }

    private static IEnumerable DateRangeSource
    {
        get
        {
            yield return new TestCaseData(DateTime.Parse("November 30"), false);

            foreach (var i in Enumerable.Range(1, 24))
                yield return new TestCaseData(DateTime.Parse($"December {i}"), true);

            foreach (var i in Enumerable.Range(25, 7))
                yield return new TestCaseData(DateTime.Parse($"December {i}"), false);
        }
    }

    [TestCaseSource(nameof(DateRangeSource))]
    public void TestDateRangeRule(DateTime date, bool matches)
    {
        const string MatchMessage = "Advent!";
        const string NoMatchMessage = "default";

        var start = DateTime.Parse("December 1");
        var end = DateTime.Parse("December 24");
        AddDateRangeRule(start, end, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(date), Is.EqualTo(matches ? MatchMessage : NoMatchMessage));
    }

    [TestCase(Month.January, WeekOfMonth.Third, DayOfWeek.Monday, "2025-01-20")] // MLK day
    [TestCase(Month.February, WeekOfMonth.Second, DayOfWeek.Tuesday, "2025-02-11")] // testing 'second'
    [TestCase(Month.May, WeekOfMonth.Last, DayOfWeek.Monday, "2025-05-26")] // memorial day
    [TestCase(Month.May, WeekOfMonth.Last, DayOfWeek.Monday, "2026-05-25")] // memorial day
    [TestCase(Month.May, WeekOfMonth.Last, DayOfWeek.Monday, "2027-05-31")] // memorial day
    [TestCase(Month.September, WeekOfMonth.First, DayOfWeek.Monday, "2025-09-01")] // labor day
    [TestCase(Month.November, WeekOfMonth.Fourth, DayOfWeek.Thursday, "2025-11-27")] // thanksgiving
    public void TestWeekOfMonthRule(Month month, WeekOfMonth week, DayOfWeek day, string goodDate)
    {
        const string MatchMessage = "special holiday with weird semantics!";
        const string NoMatchMessage = "default";

        var matchDate = DateTime.Parse(goodDate);
        var noMatchDate = matchDate.AddDays(1);

        AddWeekOfMonthRule(month, week, day, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(matchDate), Is.EqualTo(MatchMessage));
            Assert.That(messageSelector.FindMessageForDateTime(noMatchDate), Is.EqualTo(NoMatchMessage));
        });
    }

    [TestCase("2024-03-31", "2024-03-22", "2024-04-25", "FullMoon", WeekOfMonth.First, DayOfWeek.Sunday)] // easter 2024
    [TestCase("2025-04-20", "2025-03-22", "2025-04-25", "FullMoon", WeekOfMonth.First, DayOfWeek.Sunday)] // easter 2025
    [TestCase("2026-04-05", "2026-03-22", "2026-04-25", "FullMoon", WeekOfMonth.First, DayOfWeek.Sunday)] // easter 2026
    [TestCase("2025-04-01", "2025-03-22", "2025-04-25", "NewMoon", WeekOfMonth.First, DayOfWeek.Tuesday)]
    [TestCase("2025-04-08", "2025-03-22", "2025-04-25", "NewMoon", WeekOfMonth.Second, DayOfWeek.Tuesday)]
    [TestCase("2025-04-12", "2025-03-22", "2025-04-25", "NewMoon", WeekOfMonth.Second, DayOfWeek.Saturday)]
    [TestCase("2025-07-30", "2025-07-01", "2025-07-31", "FullMoon", WeekOfMonth.Third, DayOfWeek.Wednesday)]
    public void TestMoonPhaseRule(DateTime matchDate, DateTime start, DateTime end, string moonPhase, WeekOfMonth weekAfter, DayOfWeek dayOfWeek)
    {
        const string MatchMessage = "moon-based holiday!";
        const string NoMatchMessage = "default";

        List<DateTime> noMatchDates = [matchDate.AddDays(1), matchDate.AddDays(7), matchDate.AddDays(-7)];

        AddMoonPhaseRule(start, end, moonPhase, weekAfter, dayOfWeek, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(matchDate), Is.EqualTo(MatchMessage));

            foreach (var noMatchDate in noMatchDates)
                Assert.That(messageSelector.FindMessageForDateTime(noMatchDate), Is.EqualTo(NoMatchMessage));
        });
    }

    private IMessageSelector CreateSelector()
    {
        var options = Mock.Of<IOptionsMonitor<ArglonBotConfiguration>>(
            x => x.CurrentValue == new ArglonBotConfiguration
                {
                    BotToken = string.Empty,
                    PeriodicOpenMouthSettings = new()
                    {
                        Channels = new List<ChannelInfo>(),
                        Messages = _messages
                    }
                });

        return new MessageSelector(
            Mock.Of<ILoggerFactory>(x => x.CreateLogger(It.IsAny<string>()) == Mock.Of<ILogger>()),
            options
        );
    }

    private void AddDefaultRule(string message)
    {
        _messages.Add(new(message));
    }

    private void AddExactDateRule(DateTime date, string message)
    {
        _messages.Add(new(message, date));
    }

    private void AddDateRangeRule(DateTime start, DateTime end, string message)
    {
        _messages.Add(new(message, DateStart: start, DateEnd: end));
    }

    private void AddWeekOfMonthRule(Month month, WeekOfMonth week, DayOfWeek dayOfWeek, string message)
    {
        _messages.Add(new(message, Month: month, WeekOfMonth: week, DayOfWeek: dayOfWeek));
    }

    /// <summary>
    /// Adds a rule for a date after the first given moon phase in a given interval, specified by how many weeks after and which day of the week.
    /// </summary>
    private void AddMoonPhaseRule(DateTime start, DateTime end, string moonPhase, WeekOfMonth weekAfter, DayOfWeek dayOfWeek, string message)
    {
        var afterInfo = new AfterInfo(AfterType.MoonPhase, moonPhase, dayOfWeek, weekAfter);
        _messages.Add(new(message, DateStart: start, DateEnd: end, After: afterInfo));
    }
}