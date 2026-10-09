namespace Ironvale.Sim.Time;

/// <summary>Time constants (GDD v0.2 §1 + TDD §3.4). The sim only knows ticks, never seconds.</summary>
public static class SimTime
{
    /// <summary>Real-time pace only (the sim counts ticks): 4/s since GDD v0.3 D1 — 1 day = 10 s, 1 year = 60 min at 1x.</summary>
    public const int TicksPerSecondAt1x = 4;
    public const int TicksPerHour = 4;
    public const int HoursPerDay = 10;
    public const int TicksPerDay = TicksPerHour * HoursPerDay;          // 40  = 10 s at 1x
    public const int DaysPerWeek = 7;
    public const int DaysPerMonth = 30;
    public const int MonthsPerSeason = 3;
    public const int SeasonsPerYear = 4;
    public const int MonthsPerYear = MonthsPerSeason * SeasonsPerYear;  // 12
    public const int DaysPerYear = DaysPerMonth * MonthsPerYear;        // 360
    public const int TicksPerMonth = TicksPerDay * DaysPerMonth;        // 1 200
    public const int TicksPerSeason = TicksPerMonth * MonthsPerSeason;  // 3 600
    public const int TicksPerYear = TicksPerMonth * MonthsPerYear;      // 14 400 = 60 min at 1x
}

public enum Season { Spring, Summer, Autumn, Winter }

/// <summary>Calendar view of an absolute tick. Tick 0 = first day of spring, year 1.</summary>
public readonly struct Calendar
{
    public readonly long Tick;

    public Calendar(long tick) => Tick = tick;

    public long TotalDays => Tick / SimTime.TicksPerDay;
    public int Year => (int)(Tick / SimTime.TicksPerYear) + 1;
    public int MonthOfYear => (int)(Tick % SimTime.TicksPerYear / SimTime.TicksPerMonth);
    public int DayOfMonth => (int)(Tick % SimTime.TicksPerMonth / SimTime.TicksPerDay);
    public int HourOfDay => (int)(Tick % SimTime.TicksPerDay / SimTime.TicksPerHour);
    public Season Season => (Season)(MonthOfYear / SimTime.MonthsPerSeason);
    public Season PreviousSeason => (Season)(((int)Season + SimTime.SeasonsPerYear - 1) % SimTime.SeasonsPerYear);
    public bool IsWinter => Season == Season.Winter;

    public bool IsHourStart => Tick % SimTime.TicksPerHour == 0;
    public bool IsDayStart => Tick % SimTime.TicksPerDay == 0;
    public bool IsDayEnd => (Tick + 1) % SimTime.TicksPerDay == 0;
    public bool IsWeekStart => IsDayStart && TotalDays % SimTime.DaysPerWeek == 0;
    public bool IsMonthStart => Tick % SimTime.TicksPerMonth == 0;
    public bool IsSeasonStart => Tick % SimTime.TicksPerSeason == 0;
    public bool IsYearStart => Tick % SimTime.TicksPerYear == 0;

    public override string ToString() =>
        $"Y{Year} M{MonthOfYear + 1} D{DayOfMonth + 1} H{HourOfDay} ({Season})";
}
