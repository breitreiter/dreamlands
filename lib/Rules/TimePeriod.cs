namespace Dreamlands.Rules;

/// <summary>Named time periods for the skip_time mechanic.</summary>
public enum TimePeriod
{
    Morning,
    Afternoon,
    Evening,
    Night
}

/// <summary>Script and display metadata for a <see cref="TimePeriod"/>.</summary>
public readonly record struct TimePeriodInfo(TimePeriod Period, string ScriptName, string DisplayName);

/// <summary>Lookup and metadata utilities for <see cref="TimePeriod"/> values.</summary>
public static class TimePeriods
{
    /// <summary>All time periods in chronological order.</summary>
    public static IReadOnlyList<TimePeriodInfo> All { get; } = new TimePeriodInfo[]
    {
        new(TimePeriod.Morning,   "morning",   "Morning"),
        new(TimePeriod.Afternoon, "afternoon", "Afternoon"),
        new(TimePeriod.Evening,   "evening",   "Evening"),
        new(TimePeriod.Night,     "night",     "Night"),
    };

    private static readonly Dictionary<string, TimePeriod> ByScriptName =
        All.ToDictionary(i => i.ScriptName, i => i.Period);

    private static readonly Dictionary<TimePeriod, TimePeriodInfo> InfoByPeriod =
        All.ToDictionary(i => i.Period);

    /// <summary>Get metadata for a time period.</summary>
    public static TimePeriodInfo GetInfo(this TimePeriod period) => InfoByPeriod[period];

    /// <summary>The lowercase script name for use in encounter files.</summary>
    public static string ScriptName(this TimePeriod period) => InfoByPeriod[period].ScriptName;

    /// <summary>Look up a time period by its encounter-script name. Returns null if not recognised.</summary>
    public static TimePeriod? FromScriptName(string name) =>
        ByScriptName.TryGetValue(name, out var period) ? period : null;

    /// <summary>True if <paramref name="name"/> matches a known time period script name.</summary>
    public static bool IsValidScriptName(string name) =>
        ByScriptName.ContainsKey(name);
}
