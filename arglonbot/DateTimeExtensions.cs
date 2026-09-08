namespace arglonbot
{
    public static class DateTimeExtensions
    {
        public static int YearsSince(this DateTime date)
        {
            var now = DateTime.Now;

            var isNowMonthAfterDateMonth = now.Month > date.Month;
            var isNowMonthEqualToDateMonth = now.Month == date.Month;
            var isNowDayAfterOrOnDateDay = now.Day >= date.Day;

            return (now.Year - date.Year - 1) + ((isNowMonthAfterDateMonth || (isNowMonthEqualToDateMonth && isNowDayAfterOrOnDateDay)) ? 1 : 0);
        }

        public static int MonthsSince(this DateTime date)
        {
            return ((DateTime.Now.Year - date.Year) * 12)
                + (DateTime.Now.Month - date.Month);
        }
    }

    public enum WeekdayOccurrence
    {
        First,
        Second,
        Third,
        Fourth,
        Last
    }
}
