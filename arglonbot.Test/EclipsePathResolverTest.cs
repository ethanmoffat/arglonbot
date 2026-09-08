using static arglonbot.Configuration.MessageInfo;

namespace arglonbot.Test;

[TestFixture]
public class EclipsePathResolverTest
{
    private readonly EclipsePathResolver _resolver = new();

    [Test]
    public void TestTracePathMatchesPublishedGroundTrackAndTravelsWestToEast()
    {
        var path = _resolver.TracePath(new DateTime(2024, 4, 8, 18, 17, 0, DateTimeKind.Utc));

        Assert.That(path, Is.Not.Empty);
        Assert.Multiple(() =>
        {
            // Peak point should agree with the library's greatest-eclipse coordinates (~25.3N 104.1W).
            var atPeak = path.OrderBy(x => Math.Abs((x.TimeUtc - new DateTime(2024, 4, 8, 18, 17, 0, DateTimeKind.Utc)).TotalMinutes)).First();
            Assert.That(atPeak.Latitude, Is.EqualTo(25.3).Within(1.0));
            Assert.That(atPeak.Longitude, Is.EqualTo(-104.1).Within(1.0));

            // The shadow travels west to east and south to north across this eclipse.
            Assert.That(path[^1].Longitude, Is.GreaterThan(path[0].Longitude));
            Assert.That(path[^1].Latitude, Is.GreaterThan(path[0].Latitude));
        });
    }

    [Test]
    public void TestTracePathReturnsPointsInChronologicalOrder()
    {
        var path = _resolver.TracePath(new DateTime(2024, 4, 8, 18, 17, 0, DateTimeKind.Utc));

        Assert.That(path.Select(x => x.TimeUtc), Is.Ordered);
    }

    [Test]
    public void TestTracePathEndsWhenUmbraLeavesEarthRatherThanSpanningSearchWindow()
    {
        var peak = new DateTime(2024, 4, 8, 18, 17, 0, DateTimeKind.Utc);

        var path = _resolver.TracePath(peak);

        Assert.That(path[0].TimeUtc, Is.GreaterThan(peak.AddHours(-3)));
        Assert.That(path[^1].TimeUtc, Is.LessThan(peak.AddHours(3)));
    }

    [Test]
    public void TestTracePathReturnsCoordinatesWithinValidLatitudeAndLongitudeRanges()
    {
        var path = _resolver.TracePath(new DateTime(2026, 8, 12, 17, 45, 0, DateTimeKind.Utc));

        Assert.That(path, Is.Not.Empty);
        Assert.That(path.All(x => x.Latitude is >= -90 and <= 90), Is.True);
        Assert.That(path.All(x => x.Longitude is >= -180 and <= 180), Is.True);
    }

    [Test]
    public void TestTracePathReturnsNoPointsForPartialEclipse()
    {
        var path = _resolver.TracePath(new DateTime(2025, 3, 29, 10, 47, 0, DateTimeKind.Utc));

        Assert.That(path, Is.Empty);
    }

    [TestCase(EclipseEventType.Solar, "2024-04-08 00:00", true)]
    [TestCase(EclipseEventType.Lunar, "2025-03-14 00:00", false)]
    public void TestMapUrlIsProvidedForSolarButNotLunarEclipses(EclipseEventType type, DateTime time, bool hasMapUrl)
    {
        var resolver = new EclipseResolver(_resolver, new RegionDescriber());

        var result = resolver.Resolve(type, DateTime.SpecifyKind(time, DateTimeKind.Utc));

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.MapUrl.StartsWith("https://www.openstreetmap.org/", StringComparison.Ordinal), Is.EqualTo(hasMapUrl));
    }
}
