namespace CombatPrototype.Cmb;

public sealed class CmbEncounter
{
    public string Title { get; set; } = "";
    public string Image { get; set; } = "";
    public bool Repool { get; set; } = false;
    public MonsterStats Stats { get; set; } = new(0, 0, 0, new DiceRoll(0, 0, 0));
    public List<Hitbox> Hitboxes { get; set; } = new();
    public List<MonsterMove> Moves { get; set; } = new();
    public string Intro { get; set; } = "";
    public string WinText { get; set; } = "";
    public string LoseText { get; set; } = "";
    public List<MechanicLine> WinMechanics { get; set; } = new();
    public List<MechanicLine> LoseMechanics { get; set; } = new();

    public MonsterMove BasicMove =>
        Moves.FirstOrDefault(m => m.Timer == 0)
            ?? throw new InvalidOperationException($"{Title}: missing basic move (timer=0).");
}

public sealed record MonsterStats(int Hp, int Ac, int ToHit, DiceRoll Damage);

public sealed record DiceRoll(int Count, int Sides, int Modifier)
{
    public int Roll(Random rng)
    {
        int total = Modifier;
        for (int i = 0; i < Count; i++) total += rng.Next(1, Sides + 1);
        return total;
    }

    public int Min => Count + Modifier;
    public int Max => Count * Sides + Modifier;

    public override string ToString() =>
        Count == 0
            ? Modifier.ToString("+0;-0;0")
            : Modifier == 0
                ? $"{Count}d{Sides}"
                : $"{Count}d{Sides}{(Modifier >= 0 ? "+" : "")}{Modifier}";
}

public sealed record Hitbox(string Id, double Left, double Top, double Right, double Bottom);

public enum IntentClass { Attack, HeavyAttack, Defend, Pierce, Condition, Flee }

public sealed class MonsterMove
{
    public string Id { get; set; } = "";
    public IntentClass IntentClass { get; set; }
    public string IntentText { get; set; } = "";
    public int Timer { get; set; }
    public string Sprite { get; set; } = "";
    public string Anchor { get; set; } = "center";
    public string Narration { get; set; } = "";
    public List<MechanicLine> Mechanics { get; set; } = new();
    public bool IsBasic => Timer == 0;
}

public sealed record MechanicLine(string Verb, string Args)
{
    public override string ToString() => string.IsNullOrEmpty(Args) ? Verb : $"{Verb} {Args}";
}
