using System.Globalization;

namespace arglonbot
{
    // see: https://stackoverflow.com/a/2136549/2562283
    public static class DateTimeExtensions
    {
        private static readonly GregorianCalendar _gc = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        public static WeekOfMonth GetWeekOfMonth(this DateTime date)
        {
            var first = new DateTime(date.Year, date.Month, 1);
            return (WeekOfMonth)(date.GetWeekOfYear() - first.GetWeekOfYear());
        }

        public static int GetWeekOfYear(this DateTime date) => _gc.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
    }

    public enum WeekOfMonth
    {
        First,
        Second,
        Third,
        Fourth,
        Last
    }
}
