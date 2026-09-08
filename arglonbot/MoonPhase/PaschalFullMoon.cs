using System.Globalization;

namespace arglonbot;

public static class PaschalFullMoon
{
    private static readonly JulianCalendar Julian = new();

    // Easter is the first Sunday strictly after the paschal full moon.
    public static DateTime GregorianEaster(int year) => FirstSundayAfter(Gregorian(year));

    public static DateTime OrthodoxEaster(int year) => FirstSundayAfter(Orthodox(year));

    // Uses the epact method from Reingold & Dershowitz, "Calendrical Calculations".
    private static DateTime Gregorian(int year)
    {
        var metonicPosition = year % 19;
        var century = (year / 100) + 1;
        var shiftedEpact = (14 + (11 * metonicPosition) - ((3 * century) / 4) + ((5 + (8 * century)) / 25)) % 30;
        var adjustedEpact = shiftedEpact == 0 || (shiftedEpact == 1 && metonicPosition > 10)
            ? shiftedEpact + 1
            : shiftedEpact;

        return new DateTime(year, 4, 19).AddDays(-adjustedEpact);
    }

    // The Julian computus keeps the original Metonic tables with no Gregorian solar or lunar
    // corrections, so the epact is simply (19c + 15) mod 30 days after the Julian March 21.
    private static DateTime Orthodox(int year) =>
        JulianMarch21(year).AddDays(((19 * (year % 19)) + 15) % 30);

    private static DateTime FirstSundayAfter(DateTime date) => date.AddDays(7 - (int)date.DayOfWeek);

    // Julian March 21 expressed as a Gregorian date - 13 days later for 1900-2099, and drifting by a
    // further day each century the calendars diverge.
    private static DateTime JulianMarch21(int year) => Julian.ToDateTime(year, 3, 21, 0, 0, 0, 0);
}
