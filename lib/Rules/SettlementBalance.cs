using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Dreamlands.Rules;

/// <summary>Storage tier definition.</summary>
public readonly record struct StorageTier(string Id, int Cost, int Slots);

/// <summary>Settlement service definition.</summary>
public sealed class ServiceDef
{
    public string Id { get; init; } = "";
    public string Availability { get; init; } = "common";
    public bool AlwaysAvailable { get; init; }
    public IReadOnlyList<StorageTier> StorageTiers { get; init; } = [];
}

/// <summary>Regional specialty definition.</summary>
public sealed class RegionalSpecialty
{
    public string Id { get; init; } = "";
    public IReadOnlyList<string> Sells { get; init; } = [];
    public double PriceModifier { get; init; } = 1.0;
}

/// <summary>Settlement balance data from settlements.yaml.</summary>
public sealed class SettlementBalance
{
    public IReadOnlyDictionary<string, ServiceDef> Services { get; init; } = new Dictionary<string, ServiceDef>();
    public IReadOnlyDictionary<string, RegionalSpecialty> RegionalSpecialties { get; init; } = new Dictionary<string, RegionalSpecialty>();

    internal static SettlementBalance Load(string balancePath)
    {
        var path = Path.Combine(balancePath, "settlements.yaml");
        if (!File.Exists(path)) return new SettlementBalance();

        var yaml = File.ReadAllText(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var doc = deserializer.Deserialize<SettlementDoc>(yaml);

        var services = new Dictionary<string, ServiceDef>();
        if (doc?.Settlements?.Services != null)
        {
            foreach (var (id, svc) in doc.Settlements.Services)
            {
                var storageTiers = new List<StorageTier>();
                if (svc.Tiers != null)
                {
                    foreach (var (tierId, tier) in svc.Tiers)
                        storageTiers.Add(new StorageTier(tierId, tier.Cost, tier.Slots));
                }

                services[id] = new ServiceDef
                {
                    Id = id,
                    Availability = svc.Availability ?? (svc.AlwaysAvailable ? "universal" : "common"),
                    AlwaysAvailable = svc.AlwaysAvailable,
                    StorageTiers = storageTiers,
                };
            }
        }

        var specialties = new Dictionary<string, RegionalSpecialty>();
        if (doc?.Settlements?.RegionalSpecialties != null)
        {
            foreach (var (id, spec) in doc.Settlements.RegionalSpecialties)
            {
                specialties[id] = new RegionalSpecialty
                {
                    Id = id,
                    Sells = spec.Sells ?? [],
                    PriceModifier = spec.PriceModifier,
                };
            }
        }

        return new SettlementBalance
        {
            Services = services,
            RegionalSpecialties = specialties,
        };
    }

    // DTOs
    class SettlementDoc { public SettlementsYaml? Settlements { get; set; } }
    class SettlementsYaml
    {
        public Dictionary<string, ServiceYaml>? Services { get; set; }
        public Dictionary<string, SpecialtyYaml>? RegionalSpecialties { get; set; }
    }
    class ServiceYaml
    {
        public bool AlwaysAvailable { get; set; }
        public string? Availability { get; set; }
        public string? Cost { get; set; }
        public Dictionary<string, StorageTierYaml>? Tiers { get; set; }
        public string? Access { get; set; }
        public List<object>? Services { get; set; }
        public int DailyLimit { get; set; }
    }
    class StorageTierYaml
    {
        public int Cost { get; set; }
        public int Slots { get; set; }
    }
    class SpecialtyYaml
    {
        public List<string>? Sells { get; set; }
        public double PriceModifier { get; set; } = 1.0;
    }
}
