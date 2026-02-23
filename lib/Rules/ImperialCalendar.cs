namespace Dreamlands.Rules;

public enum Month
{
    Hearthbound, Harrowe, Rainhold, Lambstide, Brighthold, Midsomme,
    Hayward, Windharrow, Wainrest, Duskfall, Yulecroft, Yearwane
}

public enum WeekDay
{
    Crownday, Turnday, Midmark, Aldersday, Hammerday, Graceday
}

/// <summary>
/// A calendar date in the Aldgate Reckoning.
/// Convert from the raw day counter on PlayerState via <see cref="ImperialCalendar.GetDate"/>.
/// </summary>
public readonly record struct CalendarDate(int Year, Month? Month, int DayOfMonth, WeekDay? WeekDay)
{
    public bool IsHollow => Month is null;

    public string Display =>
        IsHollow ? $"Hollow Day {DayOfMonth}, Year {Year}"
                 : $"{DayOfMonth} {Month}, Year {Year}";

    public string DisplayFull =>
        IsHollow ? $"Hollow Day {DayOfMonth}, Year {Year}"
                 : $"{WeekDay}, {DayOfMonth} {Month}, Year {Year}";
}

/// <summary>
/// The Imperial calendar: 12 months of 30 days (360) plus 5 Hollow Days = 365 days per year.
/// Weeks are 6 days. Hollow Days sit outside the month/week structure.
/// Day 1 = Crownday, 1 Hearthbound, Year 1.
/// </summary>
public static class ImperialCalendar
{
    public const int DaysPerMonth = 30;
    public const int MonthsPerYear = 12;
    public const int DaysPerWeek = 6;
    public const int HollowDays = 5;
    public const int MonthDaysPerYear = DaysPerMonth * MonthsPerYear; // 360
    public const int DaysPerYear = MonthDaysPerYear + HollowDays;     // 365

    private static readonly Month[] Months = (Month[])Enum.GetValues(typeof(Month));

    /// <summary>
    /// Convert a 1-based day counter into a calendar date.
    /// </summary>
    public static CalendarDate GetDate(int day)
    {
        int d = day - 1;
        int year = d / DaysPerYear + 1;
        int dayInYear = d % DaysPerYear; // 0..364

        if (dayInYear >= MonthDaysPerYear)
        {
            int hollowDay = dayInYear - MonthDaysPerYear + 1; // 1..5
            return new CalendarDate(year, null, hollowDay, null);
        }

        var month = Months[dayInYear / DaysPerMonth];
        int dayOfMonth = dayInYear % DaysPerMonth + 1; // 1..30
        var weekDay = (WeekDay)(dayInYear % DaysPerWeek);

        return new CalendarDate(year, month, dayOfMonth, weekDay);
    }
}
