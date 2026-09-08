using System.Collections;

using arglonbot.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using static arglonbot.Configuration.MessageInfo;

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
    public void TestMessageWithNoFiltersMatchesAnyDateAsDefault()
    {
        const string WrongMessage = "Happy Birthday!";
        const string ExpectedMessage = "12345";

        var messageDate = DateTime.Parse("March 10");
        AddExactDateRule(messageDate, WrongMessage);
        AddDefaultRule(ExpectedMessage);

        var messageSelector = CreateSelector();

        var testDate = DateTime.Parse("January 1");
        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, testDate), Is.EqualTo(ExpectedMessage));
    }

    [Test]
    public void TestExactDateRuleMatchesOnlyThatDate()
    {
        const string Message = "Blaze itttt";

        var date = DateTime.Parse("April 20");
        AddExactDateRule(date, Message);
        AddDefaultRule("12345");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, date), Is.EqualTo(Message));
    }

    [Test]
    public void TestNoMatchingRuleAndNoDefaultThrowsInvalidOperation()
    {
        const string Message = "Blaze itttt";

        var messageDate = DateTime.Parse("April 20");
        AddExactDateRule(messageDate, Message);

        var messageSelector = CreateSelector();

        var testDate = DateTime.Parse("January 1");
        Assert.That(() => messageSelector.FindMessageForDateTime(ChannelInfo.None, testDate), Throws.InvalidOperationException);
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
    public void TestDateRangeRuleMatchesOnlyDatesInclusivelyWithinRange(DateTime date, bool matches)
    {
        const string MatchMessage = "Advent!";
        const string NoMatchMessage = "default";

        var start = DateTime.Parse("December 1");
        var end = DateTime.Parse("December 24");
        AddDateRangeRule(start, end, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, date), Is.EqualTo(matches ? MatchMessage : NoMatchMessage));
    }

    // MLK day - 3rd Monday of January
    [TestCase(Month.January, WeekdayOccurrence.Third, DayOfWeek.Monday, "2024-01-15")]
    [TestCase(Month.January, WeekdayOccurrence.Third, DayOfWeek.Monday, "2025-01-20")]
    [TestCase(Month.January, WeekdayOccurrence.Third, DayOfWeek.Monday, "2026-01-19")]
    [TestCase(Month.January, WeekdayOccurrence.Third, DayOfWeek.Monday, "2027-01-18")]
    // President's day - 3rd Monday of February
    [TestCase(Month.February, WeekdayOccurrence.Third, DayOfWeek.Monday, "2024-02-19")]
    [TestCase(Month.February, WeekdayOccurrence.Third, DayOfWeek.Monday, "2025-02-17")]
    [TestCase(Month.February, WeekdayOccurrence.Third, DayOfWeek.Monday, "2026-02-16")]
    [TestCase(Month.February, WeekdayOccurrence.Third, DayOfWeek.Monday, "2027-02-15")]
    // Mother's day - 2nd Sunday of May
    [TestCase(Month.May, WeekdayOccurrence.Second, DayOfWeek.Sunday, "2024-05-12")]
    [TestCase(Month.May, WeekdayOccurrence.Second, DayOfWeek.Sunday, "2025-05-11")]
    [TestCase(Month.May, WeekdayOccurrence.Second, DayOfWeek.Sunday, "2026-05-10")]
    [TestCase(Month.May, WeekdayOccurrence.Second, DayOfWeek.Sunday, "2027-05-09")]
    // Memorial day - last Monday of May
    [TestCase(Month.May, WeekdayOccurrence.Last, DayOfWeek.Monday, "2024-05-27")]
    [TestCase(Month.May, WeekdayOccurrence.Last, DayOfWeek.Monday, "2025-05-26")]
    [TestCase(Month.May, WeekdayOccurrence.Last, DayOfWeek.Monday, "2026-05-25")]
    [TestCase(Month.May, WeekdayOccurrence.Last, DayOfWeek.Monday, "2027-05-31")]
    // Father's day - 3rd Sunday of June
    [TestCase(Month.June, WeekdayOccurrence.Third, DayOfWeek.Sunday, "2024-06-16")]
    [TestCase(Month.June, WeekdayOccurrence.Third, DayOfWeek.Sunday, "2025-06-15")]
    [TestCase(Month.June, WeekdayOccurrence.Third, DayOfWeek.Sunday, "2026-06-21")]
    [TestCase(Month.June, WeekdayOccurrence.Third, DayOfWeek.Sunday, "2027-06-20")]
    // Labor day - 1st Monday of September
    [TestCase(Month.September, WeekdayOccurrence.First, DayOfWeek.Monday, "2024-09-02")]
    [TestCase(Month.September, WeekdayOccurrence.First, DayOfWeek.Monday, "2025-09-01")]
    [TestCase(Month.September, WeekdayOccurrence.First, DayOfWeek.Monday, "2026-09-07")]
    [TestCase(Month.September, WeekdayOccurrence.First, DayOfWeek.Monday, "2027-09-06")]
    // Thanksgiving - 4th Thursday of November
    [TestCase(Month.November, WeekdayOccurrence.Fourth, DayOfWeek.Thursday, "2024-11-28")]
    [TestCase(Month.November, WeekdayOccurrence.Fourth, DayOfWeek.Thursday, "2025-11-27")]
    [TestCase(Month.November, WeekdayOccurrence.Fourth, DayOfWeek.Thursday, "2026-11-26")]
    [TestCase(Month.November, WeekdayOccurrence.Fourth, DayOfWeek.Thursday, "2027-11-25")]
    // --- additional edge cases ---
    [TestCase(Month.February, WeekdayOccurrence.Second, DayOfWeek.Tuesday, "2025-02-11")] // testing 'second'
    [TestCase(Month.June, WeekdayOccurrence.First, DayOfWeek.Sunday, "2025-06-01")] // "first" occurrence on the 1st of the month
    [TestCase(Month.March, WeekdayOccurrence.First, DayOfWeek.Saturday, "2025-03-01")] // first Saturday is the 1st
    [TestCase(Month.January, WeekdayOccurrence.First, DayOfWeek.Wednesday, "2025-01-01")] // first day of the year
    [TestCase(Month.August, WeekdayOccurrence.Last, DayOfWeek.Sunday, "2025-08-31")] // 5th Sunday == last Sunday
    [TestCase(Month.May, WeekdayOccurrence.Fourth, DayOfWeek.Monday, "2025-05-26")] // 4th Monday of May 2025 is also the last (memorial day) - ensures Fourth still matches
    [TestCase(Month.February, WeekdayOccurrence.Last, DayOfWeek.Saturday, "2025-02-22")] // last Saturday of a 28-day February
    [TestCase(Month.December, WeekdayOccurrence.Last, DayOfWeek.Wednesday, "2025-12-31")] // last Wednesday on the 31st
    public void TestWeekdayOccurrenceRuleMatchesRealHolidayDatesAndNotAdjacentWeeks(Month month, WeekdayOccurrence week, DayOfWeek day, string goodDate)
    {
        const string MatchMessage = "special holiday with weird semantics!";
        const string NoMatchMessage = "default";

        var matchDate = DateTime.Parse(goodDate);
        List<DateTime> noMatchDates = [matchDate.AddDays(1), matchDate.AddDays(7), matchDate.AddDays(-7)];

        AddWeekOfMonthRule(month, week, day, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, matchDate), Is.EqualTo(MatchMessage));

            foreach (var noMatchDate in noMatchDates)
                Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, noMatchDate), Is.EqualTo(NoMatchMessage));
        });
    }

    [TestCase("NotAPhase")]
    [TestCase("")]
    public void TestMoonPhaseRuleWithUnrecognizedPhaseNameDoesNotMatch(string phase)
    {
        const string NoMatchMessage = "default";

        var start = new DateTime(2025, 3, 21);
        var end = new DateTime(2025, 4, 25);

        AddMoonPhaseRule(start, end, phase, WeekdayOccurrence.First, DayOfWeek.Sunday, "moon-based holiday!");
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, new DateTime(2025, 4, 6)), Is.EqualTo(NoMatchMessage));
    }

    [TestCase("2025-03-23", "ThirdQuarter")] // quarter 2025-03-22 Sat
    [TestCase("2025-03-30", "NewMoon")] // quarter 2025-03-29 Sat
    [TestCase("2025-04-06", "FirstQuarter")] // quarter 2025-04-05 Sat
    [TestCase("2025-04-14", "FullMoon")] // quarter 2025-04-13 Sun
    public void TestMoonPhaseRuleResolvesEachOfTheFourQuarterNames(DateTime matchDate, string phase)
    {
        const string MatchMessage = "moon-based holiday!";

        var start = new DateTime(2025, 3, 21);
        var end = new DateTime(2025, 4, 30);

        // The day after each phase always matches WeekdayOccurrence.First for that weekday.
        AddMoonPhaseRule(start, end, phase, WeekdayOccurrence.First, matchDate.DayOfWeek, MatchMessage);
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, matchDate), Is.EqualTo(MatchMessage));
    }

    [TestCase("2024-03-31", "2024-03-21", "2024-04-25", "FullMoon", WeekdayOccurrence.First, DayOfWeek.Sunday)]
    [TestCase("2025-04-20", "2025-03-21", "2025-04-25", "FullMoon", WeekdayOccurrence.First, DayOfWeek.Sunday)]
    [TestCase("2026-04-05", "2026-03-21", "2026-04-25", "FullMoon", WeekdayOccurrence.First, DayOfWeek.Sunday)]
    [TestCase("2025-04-01", "2025-03-21", "2025-04-25", "NewMoon", WeekdayOccurrence.First, DayOfWeek.Tuesday)]
    [TestCase("2025-04-08", "2025-03-21", "2025-04-25", "NewMoon", WeekdayOccurrence.Second, DayOfWeek.Tuesday)]
    [TestCase("2025-04-12", "2025-03-21", "2025-04-25", "NewMoon", WeekdayOccurrence.Second, DayOfWeek.Saturday)]
    [TestCase("2025-07-30", "2025-07-01", "2025-07-31", "FullMoon", WeekdayOccurrence.Third, DayOfWeek.Wednesday)]
    public void TestMoonPhaseRuleMatchesNthWeekdayAfterPhaseAndNotAdjacentDays(DateTime matchDate, DateTime start, DateTime end, string moonPhase, WeekdayOccurrence weekAfter, DayOfWeek dayOfWeek)
    {
        const string MatchMessage = "moon-based holiday!";
        const string NoMatchMessage = "default";

        List<DateTime> noMatchDates = [matchDate.AddDays(1), matchDate.AddDays(7), matchDate.AddDays(-7)];

        AddMoonPhaseRule(start, end, moonPhase, weekAfter, dayOfWeek, MatchMessage);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, matchDate), Is.EqualTo(MatchMessage));

            foreach (var noMatchDate in noMatchDates)
                Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, noMatchDate), Is.EqualTo(NoMatchMessage));
        });
    }

    [TestCase("2020-04-12")]
    [TestCase("2021-04-04")]
    [TestCase("2022-04-17")]
    [TestCase("2023-04-09")]
    [TestCase("2024-03-31")]
    [TestCase("2025-04-20")]
    [TestCase("2026-04-05")]
    [TestCase("2027-03-28")]
    [TestCase("2028-04-16")]
    [TestCase("2029-04-01")]
    [TestCase("2030-04-21")]
    [TestCase("2008-03-23")] // paschal full moon on March 21 -> earliest-boundary edge case
    [TestCase("2038-04-25")] // latest possible Easter
    [TestCase("1978-03-26")] // early-boundary Easter
    [TestCase("1980-04-06")]
    [TestCase("1983-04-03")]
    [TestCase("1981-04-19")] // astronomical/ecclesiastical divergence
    public void TestWesternEasterRuleMatchesRealEasterDatesIncludingBoundaryAndDivergenceYears(DateTime easterDate)
    {
        const string MatchMessage = "Happy Easter!";
        const string NoMatchMessage = "default";

        _messages.Add(new(MatchMessage, easter: EasterObservance.Western));
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate), Is.EqualTo(MatchMessage), $"Easter {easterDate:yyyy-MM-dd} should match");
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate.AddDays(-7)), Is.EqualTo(NoMatchMessage), "the Sunday before Easter should not match");
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate.AddDays(7)), Is.EqualTo(NoMatchMessage), "the Sunday after Easter should not match");
        });
    }

    [TestCase(0, "th")]
    [TestCase(1, "st")]
    [TestCase(2, "nd")]
    [TestCase(3, "rd")]
    [TestCase(4, "th")]
    [TestCase(42, "nd")]
    [TestCase(69, "th")]
    [TestCase(101, "st")]
    [TestCase(1003, "rd")]
    public void TestYearsSinceStartTokenExpandsToOrdinalYearCount(int yearsAfter, string expectedSuffix)
    {
        const string Token = "{token}";
        const string Format = $"This is the {Token} anniversary!";
        var MatchMessage = Format.Replace(Token, $"{yearsAfter}{expectedSuffix}");
        const string NoMatchMessage = "This is the default message.";

        var testDate = DateTime.Now;
        var startDate = testDate.AddYears(-yearsAfter);

        AddYearsAfterTokenReplacement(testDate, startDate, Token, Format);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, testDate), Is.EqualTo(MatchMessage));
    }

    [TestCase(0, "th")]
    [TestCase(1, "st")]
    [TestCase(2, "nd")]
    [TestCase(3, "rd")]
    [TestCase(4, "th")]
    [TestCase(42, "nd")]
    [TestCase(69, "th")]
    [TestCase(101, "st")]
    [TestCase(1003, "rd")]
    public void TestMonthsSinceStartTokenExpandsToOrdinalMonthCount(int monthsAfter, string expectedSuffix)
    {
        const string Token = "{token}";
        const string Format = $"This is the {Token} monthly anniversary!";
        var MatchMessage = Format.Replace(Token, $"{monthsAfter}{expectedSuffix}");
        const string NoMatchMessage = "This is the default message.";

        var testDate = DateTime.Now;
        var startDate = testDate.AddMonths(-monthsAfter);

        AddMonthsAfterTokenReplacement(testDate, startDate, Token, Format);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, testDate), Is.EqualTo(MatchMessage));
    }

    [TestCase(EasterObservance.Western, "2024-03-31")]
    [TestCase(EasterObservance.Western, "2025-04-20")]
    [TestCase(EasterObservance.Western, "2026-04-05")]
    [TestCase(EasterObservance.Western, "1981-04-19")] // astronomical/ecclesiastical divergence year
    [TestCase(EasterObservance.Orthodox, "2024-05-05")]
    [TestCase(EasterObservance.Orthodox, "2025-04-20")]
    [TestCase(EasterObservance.Orthodox, "2026-04-12")]
    [TestCase(EasterObservance.Both, "2025-04-20")]
    public void TestEasterRuleMatchesOnlyTheDateOfTheConfiguredObservance(EasterObservance observance, DateTime easterDate)
    {
        const string MatchMessage = "easter!";
        const string NoMatchMessage = "default";

        _messages.Add(new(MatchMessage, easter: observance));
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate), Is.EqualTo(MatchMessage));
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate.AddDays(-1)), Is.EqualTo(NoMatchMessage));
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, easterDate.AddDays(1)), Is.EqualTo(NoMatchMessage));
        });
    }

    [Test]
    public void TestWesternAndOrthodoxRulesMatchTheirOwnDatesInDivergentYears()
    {
        const string Western = "western";
        const string Orthodox = "orthodox";
        const string NoMatchMessage = "default";

        _messages.Add(new(Western, easter: EasterObservance.Western));
        _messages.Add(new(Orthodox, easter: EasterObservance.Orthodox));
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();
        Assert.Multiple(() =>
        {
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, new DateTime(2024, 3, 31)), Is.EqualTo(Western));
            Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, new DateTime(2024, 5, 5)), Is.EqualTo(Orthodox));
        });
    }

    [TestCase("2024-03-31", TestName = "TestBothDoesNotMatchWesternEasterInDivergentYear")]
    [TestCase("2024-05-05", TestName = "TestBothDoesNotMatchOrthodoxEasterInDivergentYear")]
    [TestCase("2026-04-05", TestName = "TestBothDoesNotMatchWesternEaster2026")]
    [TestCase("2026-04-12", TestName = "TestBothDoesNotMatchOrthodoxEaster2026")]
    public void TestBothObservanceDoesNotMatchEitherEasterInDivergentYears(DateTime date)
    {
        const string NoMatchMessage = "default";

        _messages.Add(new("both", easter: EasterObservance.Both));
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, date), Is.EqualTo(NoMatchMessage));
    }

    [Test]
    public void TestBothObservanceWinsOverWesternAndOrthodoxWhenConfiguredFirst()
    {
        const string Shared = "shared";

        _messages.Add(new(Shared, easter: EasterObservance.Both));
        _messages.Add(new("western", easter: EasterObservance.Western));
        _messages.Add(new("orthodox", easter: EasterObservance.Orthodox));
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, new DateTime(2025, 4, 20)), Is.EqualTo(Shared));
    }

    [Test]
    public void TestSubstituteTokenExpandsToConfiguredValue()
    {
        const string Token = "{token}";
        const string Format = $"This is a {Token} template!";
        const string Replacement = "substituted";
        const string MatchMessage = $"This is a {Replacement} template!";
        const string NoMatchMessage = "This is the default message.";

        AddDirectSubstitutionTokenReplacement(DateTime.Now.Date, Token, Replacement, Format);
        AddDefaultRule(NoMatchMessage);

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, DateTime.Now.Date), Is.EqualTo(MatchMessage));
    }

    [Test]
    public void TestSubstituteTokenExpandsEveryOccurrenceInMessage()
    {
        const string Token = "{token}";
        const string Format = $"{Token} and {Token} again";
        const string Replacement = "x";

        AddDirectSubstitutionTokenReplacement(DateTime.Now.Date, Token, Replacement, Format);
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, DateTime.Now.Date), Is.EqualTo("x and x again"));
    }

    [Test]
    public void TestAllTokenReplacementsOnAMessageAreApplied()
    {
        var date = DateTime.Now.Date;
        const string Format = "{first} then {second}";

        _messages.Add(new(Format, date)
        {
            TokenReplacements = [
                new("{first}", ReplacementType.Substitute, "one"),
                new("{second}", ReplacementType.Substitute, "two")
            ]
        });
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, date), Is.EqualTo("one then two"));
    }

    [TestCase(ReplacementType.YearsSinceStart)]
    [TestCase(ReplacementType.MonthsSinceStart)]
    public void TestDateBasedTokenFallsBackToConfiguredValueWhenDateStartIsMissing(ReplacementType replacementType)
    {
        var date = DateTime.Now.Date;
        const string Token = "{token}";
        const string Fallback = "fallback";

        _messages.Add(new($"value: {Token}", date)
        {
            TokenReplacements = [new(Token, replacementType, Fallback)]
        });
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, date), Is.EqualTo($"value: {Fallback}"));
    }

    [Test]
    public void TestBraceTextIsLeftIntactWhenMessageHasNoTokenReplacements()
    {
        const string Message = "no {token} here is replaced";

        AddExactDateRule(DateTime.Now.Date, Message);
        AddDefaultRule("default");

        var messageSelector = CreateSelector();

        Assert.That(messageSelector.FindMessageForDateTime(ChannelInfo.None, DateTime.Now.Date), Is.EqualTo(Message));
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
                        Messages = _messages,
                        ExtraMessages = new List<MessageInfo>()
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
        _messages.Add(new(message, dateStart: start, dateEnd: end));
    }

    private void AddWeekOfMonthRule(Month month, WeekdayOccurrence week, DayOfWeek dayOfWeek, string message)
    {
        _messages.Add(new(message, month: month, weekOfMonth: week, dayOfWeek: dayOfWeek));
    }

    private void AddMoonPhaseRule(DateTime start, DateTime end, string moonPhase, WeekdayOccurrence weekAfter, DayOfWeek dayOfWeek, string message)
    {
        var afterInfo = new AfterInfo(AfterType.MoonPhase, moonPhase, dayOfWeek, weekAfter);
        _messages.Add(new(message, dateStart: start, dateEnd: end) { After = afterInfo });
    }

    private void AddDirectSubstitutionTokenReplacement(DateTime date, string token, string replacement, string message)
    {
        _messages.Add(new(message, date)
        {
            TokenReplacements = [
                new(token, ReplacementType.Substitute, replacement)
            ]
        });
    }

    private void AddYearsAfterTokenReplacement(DateTime date, DateTime startDate, string token, string message)
    {
        _messages.Add(new(message, date: date, dateStart: startDate)
        {
            TokenReplacements = [
                new(token, ReplacementType.YearsSinceStart)
            ]
        });
    }

    private void AddMonthsAfterTokenReplacement(DateTime date, DateTime startDate, string token, string message)
    {
        _messages.Add(new(message, date: date, dateStart: startDate)
        {
            TokenReplacements = [
                new(token, ReplacementType.MonthsSinceStart)
            ]
        });
    }
}
