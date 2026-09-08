namespace arglonbot.Test;

[TestFixture]
public class RegionDescriberTest
{
    private readonly RegionDescriber _describer = new();

    private static EclipsePathPoint Point(double latitude, double longitude) =>
        new(DateTime.UtcNow, latitude, longitude);

    [Test]
    public void TestEmptyPathDescribesNoRegions()
    {
        Assert.That(_describer.Describe([]), Is.Empty);
    }

    [Test]
    public void TestPathEntirelyOverOceanDescribesNoRegions()
    {
        // Mid-Pacific and mid-Atlantic, well away from any land box.
        Assert.That(_describer.Describe([Point(0, -150), Point(-20, -20)]), Is.Empty);
    }

    [TestCase(25.0, -104.0, "Mexico")]
    [TestCase(39.2, -86.9, "the central US")]
    [TestCase(48.9, -55.0, "eastern Canada")]
    [TestCase(-25.0, 135.0, "Australia")]
    [TestCase(65.0, -18.0, "Iceland")]
    public void TestPointWithinOverlappingBoxesResolvesToTheNarrowerRegion(double latitude, double longitude, string expected)
    {
        Assert.That(_describer.Describe([Point(latitude, longitude)]), Is.EqualTo(expected));
    }

    [Test]
    public void TestConsecutivePointsInSameRegionAreNamedOnce()
    {
        var result = _describer.Describe([Point(25.0, -104.0), Point(26.0, -103.0), Point(27.0, -102.0)]);

        Assert.That(result, Is.EqualTo("Mexico"));
    }

    [Test]
    public void TestOceanPointsBetweenLandPointsDoNotSplitTheRegion()
    {
        var result = _describer.Describe([Point(0, -150), Point(25.0, -104.0), Point(0, -150)]);

        Assert.That(result, Is.EqualTo("Mexico"));
    }

    [Test]
    public void TestTwoRegionsAreJoinedWithAndWithoutAComma()
    {
        var result = _describer.Describe([Point(25.0, -104.0), Point(48.9, -55.0)]);

        Assert.That(result, Is.EqualTo("Mexico and eastern Canada"));
    }

    [Test]
    public void TestThreeOrMoreRegionsAreJoinedWithOxfordComma()
    {
        var result = _describer.Describe([Point(25.0, -104.0), Point(39.2, -86.9), Point(48.9, -55.0)]);

        Assert.That(result, Is.EqualTo("Mexico, the central US, and eastern Canada"));
    }

    [Test]
    public void TestRegionsAreNamedInPathOrderRatherThanAlphabetically()
    {
        var result = _describer.Describe([Point(48.9, -55.0), Point(25.0, -104.0)]);

        Assert.That(result, Is.EqualTo("eastern Canada and Mexico"));
    }
}
