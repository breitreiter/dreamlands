using System.Text.Json;
using System.Text.Json.Serialization;

namespace CombatPrototype.Combat;

public sealed class Loadout
{
    [JsonPropertyName("ac")]              public int Ac              { get; set; } = 13;
    [JsonPropertyName("attackBonus")]     public int AttackBonus     { get; set; } = 7;
    [JsonPropertyName("damageBonus")]     public int DamageBonus     { get; set; } = 7;
    [JsonPropertyName("damageDieSize")]   public int DamageDieSize   { get; set; } = 8;
    [JsonPropertyName("bushcraft")]       public int Bushcraft       { get; set; } = 3;
    [JsonPropertyName("cunning")]         public int Cunning         { get; set; } = 3;
    [JsonPropertyName("startingSpirits")] public int StartingSpirits { get; set; } = 20;
    [JsonPropertyName("startingHealth")]  public int StartingHealth  { get; set; } = 4;

    public static Loadout LoadOrDefault(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "Player.json");
        if (!File.Exists(path)) return new Loadout();
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Loadout>(json, JsonOpts) ?? new Loadout();
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
