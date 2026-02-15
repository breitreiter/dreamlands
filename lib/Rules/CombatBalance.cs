using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Enemy tier stats from combat.yaml.</summary>
public readonly record struct EnemyTier(int TypicalHp, int CombatTarget, int Damage);

/// <summary>Combat balance data from combat.yaml.</summary>
public sealed class CombatBalance
{
    public int StartingHealth { get; init; }
    public int MinorInjuryDamage { get; init; }
    public int ModerateInjuryDamage { get; init; }
    public int SevereInjuryDamage { get; init; }
    public int DefeatDamage { get; init; }
    public string DefeatCondition { get; init; } = "injured";
    public IReadOnlyDictionary<int, EnemyTier> EnemyTiers { get; init; } = new Dictionary<int, EnemyTier>();
    public int MinorTerror { get; init; }
    public int ModerateTerror { get; init; }
    public int MajorRevelation { get; init; }
    public int WitnessTheUnspeakable { get; init; }

    internal static CombatBalance Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "combat.yaml");
        if (!File.Exists(path)) return new CombatBalance();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<CombatDoc>(yaml);
        if (doc?.Combat == null) return new CombatBalance();
        var c = doc.Combat;

        var tiers = new Dictionary<int, EnemyTier>();
        if (c.EnemyDifficulty != null)
        {
            void AddTier(int tier, EnemyTierYaml? t)
            {
                if (t != null) tiers[tier] = new EnemyTier(t.TypicalHp, t.CombatTarget, t.Damage);
            }
            AddTier(1, c.EnemyDifficulty.Tier1);
            AddTier(2, c.EnemyDifficulty.Tier2);
            AddTier(3, c.EnemyDifficulty.Tier3);
            AddTier(4, c.EnemyDifficulty.Tier4);
        }

        return new CombatBalance
        {
            StartingHealth = c.PlayerStats?.StartingHealth ?? 20,
            MinorInjuryDamage = c.Damage?.MinorInjury ?? 3,
            ModerateInjuryDamage = c.Damage?.ModerateInjury ?? 5,
            SevereInjuryDamage = c.Damage?.SevereInjury ?? 8,
            DefeatDamage = c.Damage?.DefeatDamage ?? 5,
            DefeatCondition = c.Damage?.DefeatCondition ?? "injured",
            EnemyTiers = tiers,
            MinorTerror = c.SupernaturalDamage?.MinorTerror ?? 2,
            ModerateTerror = c.SupernaturalDamage?.ModerateTerror ?? 4,
            MajorRevelation = c.SupernaturalDamage?.MajorRevelation ?? 8,
            WitnessTheUnspeakable = c.SupernaturalDamage?.WitnessTheUnspeakable ?? 15,
        };
    }

    // DTOs
    class CombatDoc { public CombatYaml? Combat { get; set; } }
    class CombatYaml
    {
        public PlayerStatsYaml? PlayerStats { get; set; }
        public DamageYaml? Damage { get; set; }
        public EnemyDifficultyYaml? EnemyDifficulty { get; set; }
        public SupernaturalYaml? SupernaturalDamage { get; set; }
    }
    class PlayerStatsYaml { public int StartingHealth { get; set; } }
    class DamageYaml
    {
        public int MinorInjury { get; set; }
        public int ModerateInjury { get; set; }
        public int SevereInjury { get; set; }
        public int DefeatDamage { get; set; }
        public string? DefeatCondition { get; set; }
    }
    class EnemyDifficultyYaml
    {
        public EnemyTierYaml? Tier1 { get; set; }
        public EnemyTierYaml? Tier2 { get; set; }
        public EnemyTierYaml? Tier3 { get; set; }
        public EnemyTierYaml? Tier4 { get; set; }
    }
    class EnemyTierYaml
    {
        public int TypicalHp { get; set; }
        public int CombatTarget { get; set; }
        public int Damage { get; set; }
    }
    class SupernaturalYaml
    {
        public int MinorTerror { get; set; }
        public int ModerateTerror { get; set; }
        public int MajorRevelation { get; set; }
        public int WitnessTheUnspeakable { get; set; }
    }
}
