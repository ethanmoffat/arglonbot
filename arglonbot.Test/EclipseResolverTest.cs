using static arglonbot.Configuration.MessageInfo;

using Moq;
using CosineKitty;

namespace arglonbot.Test;

[TestFixture]
public class EclipseResolverTest
{
    private readonly EclipseResolver _resolver = new(new EclipsePathResolver(), new RegionDescriber());

    // Real eclipse peak times (UTC).
    [TestCase(EclipseEventType.Solar, "2024-04-08 00:00", "2024-04-08 18:17", EclipseKind.Total)]
    [TestCase(EclipseEventType.Solar, "2026-08-12 00:00", "2026-08-12 17:45", EclipseKind.Total)]
    [TestCase(EclipseEventType.Solar, "2024-10-02 00:00", "2024-10-02 18:45", EclipseKind.Annular)]
    [TestCase(EclipseEventType.Lunar, "2025-03-14 00:00", "2025-03-14 06:58", EclipseKind.Total)]
    [TestCase(EclipseEventType.Lunar, "2025-09-07 00:00", "2025-09-07 18:11", EclipseKind.Total)]
    public void TestResolveReturnsKindPeakTimeAndLocationForUpcomingSupportedEclipse(EclipseEventType type, DateTime time, DateTime expectedPeak, EclipseKind expectedKind)
    {
        var result = _resolver.Resolve(type, DateTime.SpecifyKind(time, DateTimeKind.Utc));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Type, Is.EqualTo(type));
        Assert.That(result.Kind, Is.EqualTo(expectedKind));
        Assert.That(result.PeakUtc, Is.EqualTo(expectedPeak).Within(TimeSpan.FromMinutes(1)));
        Assert.That(result.Location, Is.Not.Empty);
    }

    [TestCase(EclipseEventType.Solar, "2025-03-29 00:00", TestName = "TestPartialSolarIsIgnored")]
    [TestCase(EclipseEventType.Lunar, "2024-09-18 00:00", TestName = "TestPartialLunarIsIgnored")]
    [TestCase(EclipseEventType.Lunar, "2024-03-25 00:00", TestName = "TestPenumbralLunarIsIgnored")]
    public void TestResolveReturnsNullForPartialAndPenumbralEclipses(EclipseEventType type, DateTime time)
    {
        Assert.That(_resolver.Resolve(type, DateTime.SpecifyKind(time, DateTimeKind.Utc)), Is.Null);
    }

    [TestCase(EclipseEventType.Solar, "2024-04-06 00:00", TestName = "TestSolarMoreThanADayAway")]
    [TestCase(EclipseEventType.Solar, "2024-04-09 00:00", TestName = "TestSolarAlreadyPassed")]
    [TestCase(EclipseEventType.Solar, "2024-06-15 00:00", TestName = "TestSolarNowhereNear")]
    [TestCase(EclipseEventType.Lunar, "2025-03-12 00:00", TestName = "TestLunarMoreThanADayAway")]
    [TestCase(EclipseEventType.Lunar, "2025-03-15 00:00", TestName = "TestLunarAlreadyPassed")]
    public void TestResolveReturnsNullForEclipsesOutsideTheTwentyFourHourWindow(EclipseEventType type, DateTime time)
    {
        Assert.That(_resolver.Resolve(type, DateTime.SpecifyKind(time, DateTimeKind.Utc)), Is.Null);
    }

    [Test]
    public void TestResolveFindsEclipseOnNextCalendarDayWithinWindow()
    {
        var eveningBefore = new DateTime(2025, 3, 13, 20, 0, 0, DateTimeKind.Utc);

        var result = _resolver.Resolve(EclipseEventType.Lunar, eveningBefore);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PeakUtc, Is.EqualTo(new DateTime(2025, 3, 14, 6, 58, 0)).Within(TimeSpan.FromMinutes(1)));
    }

    [Test]
    public void TestResolveReturnsNullForEclipseThatAlreadyPeakedToday()
    {
        var afterThePeak = new DateTime(2024, 4, 8, 20, 0, 0, DateTimeKind.Utc);

        Assert.That(_resolver.Resolve(EclipseEventType.Solar, afterThePeak), Is.Null);
    }

    [Test]
    public void TestSolarEclipseDoesNotMatchLunarRuleAndViceVersa()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_resolver.Resolve(EclipseEventType.Lunar, new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc)), Is.Null);
            Assert.That(_resolver.Resolve(EclipseEventType.Solar, new DateTime(2025, 3, 14, 0, 0, 0, DateTimeKind.Utc)), Is.Null);
        });
    }

    [TestCase("2024-04-08", "Mexico", "the central US", "eastern Canada")] // Mexico -> Canada
    [TestCase("2026-08-12", "Greenland", "western Europe", null)] // Greenland -> Spain
    public void TestSolarLocationNamesLandRegionsCrossedByPathOfTotality(DateTime date, string firstRegion, string? secondRegion, string? thirdRegion)
    {
        var result = _resolver.Resolve(EclipseEventType.Solar, DateTime.SpecifyKind(date, DateTimeKind.Utc));

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.Location, Does.Contain(firstRegion));

            if (secondRegion != null)
                Assert.That(result.Location, Does.Contain(secondRegion));

            if (thirdRegion != null)
                Assert.That(result.Location, Does.Contain(thirdRegion));
        });
    }

    [Test]
    public void TestSolarLocationRegionsAreOrderedByShadowTravelDirection()
    {
        var result = _resolver.Resolve(EclipseEventType.Solar, new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Location.IndexOf("Mexico", StringComparison.Ordinal),
            Is.LessThan(result.Location.IndexOf("eastern Canada", StringComparison.Ordinal)));
    }

    [Test]
    public void TestSolarEclipseFallsBackToGreatestEclipseCoordinatesWhenPathHasNoNamedRegions()
    {
        var regionDescriber = Mock.Of<IRegionDescriber>(x => x.Describe(It.IsAny<IReadOnlyList<EclipsePathPoint>>()) == string.Empty);
        var resolver = new EclipseResolver(new EclipsePathResolver(), regionDescriber);

        var result = resolver.Resolve(EclipseEventType.Solar, new DateTime(2024, 4, 8, 0, 0, 0, DateTimeKind.Utc));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Location, Is.EqualTo("25.3°N 104.1°W"));
    }

    [Test]
    public void TestResolveNormalizesLocalTimeBeforeEvaluatingEclipseWindow()
    {
        var localPeakTime = new DateTime(2024, 4, 8, 11, 17, 0, DateTimeKind.Local);

        var result = _resolver.Resolve(EclipseEventType.Solar, localPeakTime);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.PeakUtc, Is.EqualTo(localPeakTime.ToUniversalTime()).Within(TimeSpan.FromMinutes(1)));
    }
}
