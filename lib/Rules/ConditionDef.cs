namespace Dreamlands.Rules;

/// <summary>Definition of a status condition.</summary>
public sealed class ConditionDef
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Type { get; init; }
    public string Biome { get; init; } = "none";
    public string Tier { get; init; } = "none";
    public string Drains { get; init; } = "health";
    public Magnitude DrainMagnitude { get; init; }
    public double OvernightChance { get; init; }
    public IReadOnlyList<string> ResistedBy { get; init; } = [];
    public string? CuredBy { get; init; }
    public string? AutoClearOnExit { get; init; }
    public string? Foreshadow { get; init; }

    internal static IReadOnlyDictionary<string, ConditionDef> All { get; } = BuildAll();

    static Dictionary<string, ConditionDef> BuildAll() => new()
    {
        // Physical (drain health)
        ["injured"] = new()
        {
            Id = "injured", Name = "Injured", Biome = "none", Tier = "none",
            Drains = "health", DrainMagnitude = Magnitude.Medium, CuredBy = "bandages",
        },
        ["thirsty"] = new()
        {
            Id = "thirsty", Name = "Thirsty", Biome = "scrub", Tier = "any",
            OvernightChance = 1.0, AutoClearOnExit = "scrub",
            Drains = "health", DrainMagnitude = Magnitude.Small, ResistedBy = ["canteen"],
        },
        ["cold"] = new()
        {
            Id = "cold", Name = "Cold", Biome = "mountains", Tier = "any",
            OvernightChance = 1.0, AutoClearOnExit = "mountains",
            Drains = "health", DrainMagnitude = Magnitude.Small, ResistedBy = ["warm_clothing"],
        },

        // Mental (drain spirits)
        ["hungry"] = new()
        {
            Id = "hungry", Name = "Hungry", Biome = "none", Tier = "none",
            Drains = "spirits", DrainMagnitude = Magnitude.Small, CuredBy = "eating",
        },
        ["exhausted"] = new()
        {
            Id = "exhausted", Name = "Exhausted", Biome = "any", Tier = "any",
            OvernightChance = 0.5,
            Drains = "spirits", DrainMagnitude = Magnitude.Small,
            ResistedBy = ["sturdy_boots", "balanced_meal"], CuredBy = "settlement_rest",
        },
        ["haunted"] = new()
        {
            Id = "haunted", Name = "Haunted", Biome = "plains", Tier = "2",
            OvernightChance = 0.35,
            Drains = "spirits", DrainMagnitude = Magnitude.Medium,
            ResistedBy = ["warding_talisman"], CuredBy = "temple_visit",
        },
        ["lost"] = new()
        {
            Id = "lost", Name = "Lost", Biome = "any", Tier = "any",
            OvernightChance = 0.3,
            Drains = "spirits", DrainMagnitude = Magnitude.Trivial, ResistedBy = ["map_kit"],
        },

        // Disease (drain health, only one active at a time)
        ["swamp_fever"] = new()
        {
            Id = "swamp_fever", Name = "Swamp Fever", Type = "disease",
            Biome = "swamp", Tier = "2", OvernightChance = 0.4,
            Foreshadow = "You feel feverish and your joints ache.",
            Drains = "health", DrainMagnitude = Magnitude.Small,
            ResistedBy = ["mosquito_netting"], CuredBy = "fever_tonic",
        },
        ["infested"] = new()
        {
            Id = "infested", Name = "Infested", Type = "disease",
            Biome = "forest", Tier = "2", OvernightChance = 0.35,
            Foreshadow = "Something burrowed in during the night. You can feel it moving.",
            Drains = "health", DrainMagnitude = Magnitude.Small,
            ResistedBy = ["treated_bedroll"], CuredBy = "purgative",
        },
        ["rot_lung"] = new()
        {
            Id = "rot_lung", Name = "Rot Lung", Type = "disease",
            Biome = "scrub", Tier = "2", OvernightChance = 0.3,
            Foreshadow = "You wake slick with sweat, coughing dust.",
            Drains = "health", DrainMagnitude = Magnitude.Small,
            ResistedBy = ["dust_mask"], CuredBy = "expectorant",
        },
        ["river_flux"] = new()
        {
            Id = "river_flux", Name = "River Flux", Type = "disease",
            Biome = "any", Tier = "2", OvernightChance = 0.15,
            Foreshadow = "Your stomach turns. Something in the water.",
            Drains = "health", DrainMagnitude = Magnitude.Small,
            ResistedBy = ["purifying_tablets"], CuredBy = "gut_remedy",
        },

        // Tier 3
        ["withering"] = new()
        {
            Id = "withering", Name = "Withering", Type = "disease",
            Biome = "plains", Tier = "3", OvernightChance = 0.3,
            Foreshadow = "Your teeth ache and there's blood on your shirt. The air here tastes like a copper coin.",
            Drains = "health", DrainMagnitude = Magnitude.Large,
            ResistedBy = ["golem_suit"], CuredBy = "silver_elixir",
        },
        ["forgotten"] = new()
        {
            Id = "forgotten", Name = "Forgotten", Type = "disease",
            Biome = "swamp", Tier = "3", OvernightChance = 0.3,
            Foreshadow = "For a long moment, you're not sure who you are or how you got here. The haze clears, but your memories seem unfamiliar.",
            Drains = "spirits", DrainMagnitude = Magnitude.Large,
            ResistedBy = ["diary"], AutoClearOnExit = "swamp",
        },
    };
}
