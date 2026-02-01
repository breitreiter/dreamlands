namespace Dreamlands.Rules;

/// <summary>Named magnitudes for damage, healing, and skill adjustments.</summary>
public enum Magnitude
{
    Trivial,
    Small,
    Medium,
    Large,
    Huge
}

/// <summary>Script and display metadata for a <see cref="Magnitude"/>.</summary>
public readonly record struct MagnitudeInfo(Magnitude Magnitude, string ScriptName, string DisplayName);

/// <summary>Lookup and metadata utilities for <see cref="Magnitude"/> values.</summary>
public static class Magnitudes
{
    /// <summary>All magnitudes in ascending order.</summary>
    public static IReadOnlyList<MagnitudeInfo> All { get; } = new MagnitudeInfo[]
    {
        new(Magnitude.Trivial, "trivial", "Trivial"),
        new(Magnitude.Small,   "small",   "Small"),
        new(Magnitude.Medium,  "medium",  "Medium"),
        new(Magnitude.Large,   "large",   "Large"),
        new(Magnitude.Huge,    "huge",    "Huge"),
    };

    private static readonly Dictionary<string, Magnitude> ByScriptName =
        All.ToDictionary(i => i.ScriptName, i => i.Magnitude);

    private static readonly Dictionary<Magnitude, MagnitudeInfo> InfoByMagnitude =
        All.ToDictionary(i => i.Magnitude);

    /// <summary>Get metadata for a magnitude.</summary>
    public static MagnitudeInfo GetInfo(this Magnitude magnitude) => InfoByMagnitude[magnitude];

    /// <summary>The lowercase script name for use in encounter files.</summary>
    public static string ScriptName(this Magnitude magnitude) => InfoByMagnitude[magnitude].ScriptName;

    /// <summary>Look up a magnitude by its encounter-script name. Returns null if not recognised.</summary>
    public static Magnitude? FromScriptName(string name) =>
        ByScriptName.TryGetValue(name, out var magnitude) ? magnitude : null;

    /// <summary>True if <paramref name="name"/> matches a known magnitude script name.</summary>
    public static bool IsValidScriptName(string name) =>
        ByScriptName.ContainsKey(name);
}
