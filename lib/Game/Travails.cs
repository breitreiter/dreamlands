using Dreamlands.Rules;

namespace Dreamlands.Game;

/// <summary>
/// Deterministic travel-cost engine (travails) — replaces travel conditions.
/// Exposure accrues into <see cref="PlayerState.TravailLedger"/> as the player walks
/// (AccrueStep) and camps (AccrueNight); spirits charge at threshold crossings, so
/// state is always current when an encounter interrupts mid-journey. Summarize()
/// flushes the ledger into the narrative summary shown at the tail end of a trip.
/// No RNG anywhere — route, gear, and Bushcraft tier fully determine the cost.
/// See plans/travel_travails.md.
/// </summary>
public static class Travails
{
    /// <summary>
    /// Accrue step-unit hazards for one step into <paramref name="biome"/>.
    /// Callers skip settlement nodes — those steps don't accrue.
    /// </summary>
    public static void AccrueStep(PlayerState state, string biome, BalanceData balance)
    {
        foreach (var hazard in balance.Hazards.Values)
            if (hazard.Unit == HazardUnit.Steps && hazard.Biome == biome)
                Accrue(state, hazard);
    }

    /// <summary>
    /// Accrue night-unit hazards for one night camped on the road.
    /// Settlement nights don't accrue — callers only invoke for wilderness camps.
    /// </summary>
    public static void AccrueNight(PlayerState state, BalanceData balance)
    {
        foreach (var hazard in balance.Hazards.Values)
            if (hazard.Unit == HazardUnit.Nights)
                Accrue(state, hazard);
    }

    /// <summary>
    /// Flush the ledger into a display summary and reset it for the next journey.
    /// Returns null when nothing accrued (clean trip, no exposure).
    /// </summary>
    public static TravailSummary? Summarize(PlayerState state, BalanceData balance)
    {
        var bushcraft = state.Skills.GetValueOrDefault(Skill.Bushcraft);
        List<TravailLine> lines = [];

        var tallies = state.TravailLedger
            .Where(kv => kv.Value.Units > 0 || kv.Value.Spared > 0)
            .OrderByDescending(kv => kv.Value.SpiritsCharged)
            .ThenBy(kv => kv.Key);

        foreach (var (hazardId, tally) in tallies)
        {
            if (!balance.Hazards.TryGetValue(hazardId, out var hazard)) continue;

            var sparedByGear = tally.Units == 0;
            var easedBySkill = !sparedByGear && bushcraft > SkillTier.Untrained;
            lines.Add(new TravailLine(
                hazard.Id, hazard.Name, tally.Units, tally.Spared, tally.SpiritsCharged,
                sparedByGear, easedBySkill,
                BuildText(hazard, tally, sparedByGear)));
        }

        state.TravailLedger.Clear();

        if (lines.Count == 0) return null;
        return new TravailSummary(lines, lines.Sum(l => l.SpiritsLost));
    }

    static void Accrue(PlayerState state, HazardDef hazard)
    {
        if (!state.TravailLedger.TryGetValue(hazard.Id, out var tally))
            state.TravailLedger[hazard.Id] = tally = new TravailTally();

        if (state.Pack.Any(i => i.DefId == hazard.MitigatingItemId))
        {
            tally.Spared++;
            return;
        }

        tally.Units++;

        var tier = state.Skills.GetValueOrDefault(Skill.Bushcraft);
        var perSpirit = hazard.UnitsPerSpirit[(int)tier];

        // Incremental charge: what the total exposure owes minus what's already paid.
        // (Tier or gear changing mid-journey can make this 0 or negative — never refund.)
        var owed = tally.Units / perSpirit - tally.SpiritsCharged;
        if (owed <= 0) return;

        // SpiritsCharged tracks owed (not floored) so thresholds don't re-fire at 0 spirits.
        state.Spirits = Math.Max(0, state.Spirits - owed);
        tally.SpiritsCharged += owed;
    }

    static string BuildText(HazardDef hazard, TravailTally tally, bool sparedByGear)
    {
        if (sparedByGear) return hazard.SparedPhrase;

        var exposure = hazard.Unit == HazardUnit.Nights
            ? $"{NightsPhrase(tally.Units)} on the road"
            : $"{hazard.ExposurePhrase} for {StepsDuration(tally.Units)}";
        var toll = tally.SpiritsCharged > 0
            ? $"lost {SpiritsPhrase(tally.SpiritsCharged)} to {hazard.CauseNoun}"
            : $"the {hazard.CauseNoun} took no toll";
        return $"{exposure}, {toll}.";
    }

    static string SpiritsPhrase(int n) => n == 1 ? "1 spirit" : $"{n} spirits";

    static string NightsPhrase(int n) => n switch
    {
        1 => "One night",
        2 => "Two nights",
        3 => "Three nights",
        4 => "Four nights",
        5 => "Five nights",
        6 => "Six nights",
        _ => $"{n} nights",
    };

    // One step = one time period; 5 periods = 1 day.
    static string StepsDuration(int steps) => steps switch
    {
        <= 1 => "a few hours",
        <= 3 => "half a day",
        <= 6 => "a day",
        <= 8 => "a day and a half",
        <= 12 => "two days",
        _ => $"{(steps + 2) / 5} days",
    };
}
