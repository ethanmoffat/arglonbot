namespace arglonbot.Test;

[TestFixture]
public class PaschalFullMoonTest
{
    // Shifted epact is zero and must be incremented.
    [TestCase(1905, "1905-04-23")]
    // Shifted epact is one with a Metonic position above ten and must be incremented.
    [TestCase(1916, "1916-04-23")]
    public void TestGregorianEasterMatchesKnownDatesThatRequireEpactAdjustment(int year, DateTime expected)
    {
        Assert.That(PaschalFullMoon.GregorianEaster(year), Is.EqualTo(expected));
    }

    // Gregorian paschal full moon is Sunday, so Easter must be the following Sunday.
    [TestCase(1902, "1902-03-30")]
    // Orthodox paschal full moon is Sunday, so Easter must be the following Sunday.
    [TestCase(1901, "1901-04-14")]
    public void TestEasterFallsOnSundayStrictlyAfterPaschalFullMoon(int year, DateTime expected)
    {
        var easter = year == 1902
            ? PaschalFullMoon.GregorianEaster(year)
            : PaschalFullMoon.OrthodoxEaster(year);

        Assert.That(easter, Is.EqualTo(expected));
    }

    // The Julian-to-Gregorian offset is twelve days before the 1900 transition.
    [TestCase(1899, "1899-04-30")]
    // The Julian-to-Gregorian offset is thirteen days from the 1900 transition.
    [TestCase(1900, "1900-04-22")]
    // The Julian-to-Gregorian offset remains thirteen days before the 2100 transition.
    [TestCase(2099, "2099-04-12")]
    // The Julian-to-Gregorian offset is fourteen days from the 2100 transition.
    [TestCase(2100, "2100-05-02")]
    public void TestOrthodoxEasterAccountsForJulianGregorianCalendarDivergence(int year, DateTime expected)
    {
        Assert.That(PaschalFullMoon.OrthodoxEaster(year), Is.EqualTo(expected));
    }
}
