namespace Dreamlands.Rules;

/// <summary>One approach in a 3-way picker skill check.</summary>
public sealed record Approach(
    string Id,           // lowercase token matching encounter file usage (e.g. "charm")
    string DisplayLabel, // player-facing label (e.g. "Charm")
    string Noun,         // connector noun form (e.g. "charm")
    string Gerund,       // connector gerund form (e.g. "charming")
    string IconHint      // SVG file hint for UI (Phase 6)
);

/// <summary>
/// Static approach rosters for every picker-capable skill.
/// Approach order is canonical (Index 0/1/2); the correct and wrong ids are per-encounter.
/// </summary>
public static class ApproachRoster
{
    // ── Negotiation ──────────────────────────────────────────────────────────

    static readonly Approach NegCharm    = new("charm",    "Charm",     "charm",     "charming",    "charm.svg");
    static readonly Approach NegReason   = new("reason",   "Reason",    "reasoning", "reasoning",   "brain.svg");
    static readonly Approach NegThreaten = new("threaten", "Threaten",  "threat",    "threatening", "barbute.svg");

    // ── Cunning ──────────────────────────────────────────────────────────────

    static readonly Approach CunHide   = new("hide",   "Hide",       "concealment", "hiding",     "cloak-dagger.svg");
    static readonly Approach CunBluff  = new("bluff",  "Bluff",      "bluff",       "bluffing",   "crown.svg");
    static readonly Approach CunScheme = new("scheme", "Scheme",     "scheme",      "scheming",   "one-eyed.svg");

    // ── Bushcraft ────────────────────────────────────────────────────────────

    static readonly Approach BshPush    = new("push",    "Push",    "determination", "pushing through", "dodge.svg");
    static readonly Approach BshPlan    = new("plan",    "Plan",    "planning",       "planning",        "compass.svg");
    static readonly Approach BshReroute = new("reroute", "Reroute", "rerouting",      "rerouting",       "treasure-map.svg");

    // ── Combat ───────────────────────────────────────────────────────────────

    static readonly Approach CmbRush       = new("rush",       "Rush",       "rush",       "rushing in",     "sword-brandish.svg");
    static readonly Approach CmbStrategize = new("strategize", "Strategize", "strategy",   "strategizing",   "one-eyed.svg");
    static readonly Approach CmbOutlast    = new("outlast",    "Outlast",    "endurance",  "outlasting",     "checked-shield.svg");

    // ── Rosters by skill ──────────────────────────────────────────────────────

    static readonly IReadOnlyList<Approach> NegotiationApproaches =
        new[] { NegCharm, NegReason, NegThreaten };

    static readonly IReadOnlyList<Approach> CunningApproaches =
        new[] { CunHide, CunBluff, CunScheme };

    static readonly IReadOnlyList<Approach> BushcraftApproaches =
        new[] { BshPush, BshPlan, BshReroute };

    static readonly IReadOnlyList<Approach> CombatApproaches =
        new[] { CmbRush, CmbStrategize, CmbOutlast };

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>All three approaches for the given picker skill.</summary>
    public static IReadOnlyList<Approach> GetApproaches(Skill skill) => skill switch
    {
        Skill.Negotiation => NegotiationApproaches,
        Skill.Cunning     => CunningApproaches,
        Skill.Bushcraft   => BushcraftApproaches,
        Skill.Combat      => CombatApproaches,
        _ => throw new ArgumentException($"No approach roster for skill {skill}"),
    };

    /// <summary>Resolve one approach by id within a skill's roster (case-insensitive). Returns null if not found.</summary>
    public static Approach? GetApproach(Skill skill, string id)
    {
        var roster = GetApproaches(skill);
        foreach (var a in roster)
            if (string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase))
                return a;
        return null;
    }

    /// <summary>
    /// Return the neutral approach (the one that is neither correct nor wrong for this check).
    /// </summary>
    public static Approach GetNeutral(Skill skill, string correctId, string wrongId)
    {
        var roster = GetApproaches(skill);
        foreach (var a in roster)
            if (!string.Equals(a.Id, correctId, StringComparison.OrdinalIgnoreCase)
             && !string.Equals(a.Id, wrongId,   StringComparison.OrdinalIgnoreCase))
                return a;
        // Fallback — shouldn't happen with valid authored data
        return roster[0];
    }
}
