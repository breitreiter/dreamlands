using System.Text.Json;

namespace Dreamlands.Tactical;

public sealed class TacticalBundle
{
    public IReadOnlyList<TacticalEncounter> Encounters { get; }
    public IReadOnlyList<TacticalGroup> Groups { get; }

    readonly Dictionary<string, int> _encountersById;
    readonly Dictionary<string, int> _groupsById;
    readonly Dictionary<string, IReadOnlyList<int>> _encountersByCategory;

    TacticalBundle(
        List<TacticalEncounter> encounters,
        List<TacticalGroup> groups,
        Dictionary<string, int> encountersById,
        Dictionary<string, int> groupsById,
        Dictionary<string, IReadOnlyList<int>> encountersByCategory)
    {
        Encounters = encounters;
        Groups = groups;
        _encountersById = encountersById;
        _groupsById = groupsById;
        _encountersByCategory = encountersByCategory;
    }

    public TacticalEncounter? GetEncounterById(string id) =>
        _encountersById.TryGetValue(id, out var idx) ? Encounters[idx] : null;

    public TacticalGroup? GetGroupById(string id) =>
        _groupsById.TryGetValue(id, out var idx) ? Groups[idx] : null;

    public IReadOnlyList<TacticalEncounter> GetByCategory(string category) =>
        _encountersByCategory.TryGetValue(category, out var indices)
            ? indices.Select(i => Encounters[i]).ToList()
            : [];

    public IReadOnlyList<string> GetCategories() => _encountersByCategory.Keys.ToList();

    public static TacticalBundle Load(string path)
    {
        var json = File.ReadAllText(path);
        return FromJson(json);
    }

    public static TacticalBundle FromJson(string json)
    {
        var doc = JsonSerializer.Deserialize<BundleDto>(json, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize tactical bundle JSON.");

        var encounters = new List<TacticalEncounter>();
        foreach (var e in doc.Encounters)
        {
            encounters.Add(new TacticalEncounter
            {
                Id = e.Id,
                Category = e.Category,
                Title = e.Title,
                Body = e.Body,
                Variant = Enum.Parse<Variant>(e.Variant, ignoreCase: true),
                Intent = e.Intent,
                Stat = e.Stat,
                Tier = e.Tier,
                Requires = e.Requires ?? [],
                Resistance = e.Resistance,
                Momentum = e.Momentum,
                QueueDepth = e.QueueDepth,
                TimerDraw = e.TimerDraw,
                Timers = e.Timers.Select(t => new TimerDef(
                    t.Name,
                    Enum.Parse<TimerEffect>(t.Effect, ignoreCase: true),
                    t.Amount,
                    t.Countdown,
                    t.CounterName)).ToList(),
                Openings = e.Openings.Select(o => new OpeningDef(
                    o.Name,
                    new OpeningCost(ParseSnakeEnum<CostKind>(o.CostKind), o.CostAmount),
                    new OpeningEffect(ParseSnakeEnum<EffectKind>(o.EffectKind), o.EffectAmount),
                    o.Requires)).ToList(),
                Path = e.Path?.Select(o => new OpeningDef(
                    o.Name,
                    new OpeningCost(ParseSnakeEnum<CostKind>(o.CostKind), o.CostAmount),
                    new OpeningEffect(ParseSnakeEnum<EffectKind>(o.EffectKind), o.EffectAmount),
                    o.Requires)).ToList() ?? [],
                Approaches = e.Approaches?.Select(a => new ApproachDef(
                    Enum.Parse<ApproachKind>(a.Kind, ignoreCase: true),
                    a.Momentum,
                    a.TimerCount,
                    a.BonusOpenings)).ToList() ?? [],
                Failure = e.Failure is { } f
                    ? new FailureOutcome(f.Text, f.Mechanics ?? [])
                    : null,
            });
        }

        var groups = new List<TacticalGroup>();
        foreach (var g in doc.Groups)
        {
            groups.Add(new TacticalGroup
            {
                Id = g.Id,
                Category = g.Category,
                Title = g.Title,
                Body = g.Body,
                Tier = g.Tier,
                Requires = g.Requires ?? [],
                Branches = g.Branches.Select(b => new BranchDef(
                    b.Label, b.Intent, b.EncounterRef, b.Requires)).ToList(),
            });
        }

        var encountersById = new Dictionary<string, int>();
        var groupsById = new Dictionary<string, int>();
        var encountersByCategory = new Dictionary<string, IReadOnlyList<int>>();

        if (doc.Index.EncountersById != null)
            foreach (var (id, idx) in doc.Index.EncountersById)
                encountersById[id] = idx;

        if (doc.Index.GroupsById != null)
            foreach (var (id, idx) in doc.Index.GroupsById)
                groupsById[id] = idx;

        if (doc.Index.EncountersByCategory != null)
            foreach (var (cat, indices) in doc.Index.EncountersByCategory)
                encountersByCategory[cat] = indices;

        return new TacticalBundle(encounters, groups, encountersById, groupsById, encountersByCategory);
    }

    // "stop_timer" -> "StopTimer" etc.
    static T ParseSnakeEnum<T>(string value) where T : struct, Enum =>
        Enum.Parse<T>(value.Replace("_", ""), ignoreCase: true);

    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Private DTOs matching bundle JSON shape
    record BundleDto(IndexDto Index, List<EncounterDto> Encounters, List<GroupDto> Groups);
    record IndexDto(
        Dictionary<string, int>? EncountersById,
        Dictionary<string, int>? GroupsById,
        Dictionary<string, List<int>>? EncountersByCategory);
    record EncounterDto(
        string Id, string Category, string Title, string Body, string Variant,
        string? Intent, string? Stat, int? Tier, List<string>? Requires,
        int Resistance, int? Momentum, int? QueueDepth,
        int TimerDraw, List<TimerDto> Timers, List<OpeningDto> Openings,
        List<OpeningDto>? Path, List<ApproachDto>? Approaches, FailureDto? Failure);
    record TimerDto(string Name, string? CounterName, string Effect, int Amount, int Countdown);
    record OpeningDto(string Name, string CostKind, int CostAmount, string EffectKind, int EffectAmount, string? Requires);
    record ApproachDto(string Kind, int Momentum, int TimerCount, int BonusOpenings);
    record FailureDto(string Text, List<string>? Mechanics);
    record GroupDto(
        string Id, string Category, string Title, string Body,
        int? Tier, List<string>? Requires, List<BranchDto> Branches);
    record BranchDto(string Label, string? Intent, string EncounterRef, string? Requires);
}
