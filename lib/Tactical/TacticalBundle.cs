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

    /// <summary>
    /// Resolves a short name relative to a category, falling back to exact ID match.
    /// Mirrors EncounterSelection.ResolveNavigation for the tactical bundle.
    /// </summary>
    public TacticalEncounter? ResolveNavigation(string name, string? category)
    {
        // Try exact qualified match first
        if (_encountersById.TryGetValue(name, out var idx))
            return Encounters[idx];

        // Try category-relative: "Road Toll Chase" in category "plains/tier1" → "plains/tier1/Road Toll Chase"
        if (category != null)
        {
            var qualified = $"{category}/{name}";
            if (_encountersById.TryGetValue(qualified, out idx))
                return Encounters[idx];
        }

        return null;
    }

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
                Stat = e.Stat,
                Tier = e.Tier,
                Requires = e.Requires ?? [],
                Timers = e.Timers.Select(t => new TimerDef(
                    t.Name,
                    Enum.Parse<TimerEffect>(t.Effect, ignoreCase: true),
                    t.Amount,
                    t.Countdown,
                    t.Resistance,
                    t.CounterName,
                    t.ConditionId,
                    t.TicksTimerName)).ToList(),
                Openings = e.Openings.Select(o => new OpeningDef(
                    o.Name, o.Archetype, o.Requires)).ToList(),
                Approaches = e.Approaches?.Select(a => new ApproachDef(
                    Enum.Parse<ApproachKind>(a.Kind, ignoreCase: true))).ToList() ?? [],
                Failure = e.Failure is { } f
                    ? new FailureOutcome(f.Text, f.Mechanics ?? [])
                    : null,
                Success = e.Success is { } s
                    ? new SuccessOutcome(s.Text, s.Mechanics ?? [])
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
                    b.Label, b.EncounterRef, b.Requires)).ToList(),
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

    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Private DTOs matching bundle JSON shape
    record BundleDto(IndexDto Index, List<EncounterDto> Encounters, List<GroupDto> Groups);
    record IndexDto(
        Dictionary<string, int>? EncountersById,
        Dictionary<string, int>? GroupsById,
        Dictionary<string, List<int>>? EncountersByCategory);
    record EncounterDto(
        string Id, string Category, string Title, string Body,
        string? Stat, int? Tier, List<string>? Requires,
        List<TimerDto> Timers, List<OpeningDto> Openings,
        List<ApproachDto>? Approaches, FailureDto? Failure, SuccessDto? Success);
    record TimerDto(string Name, string? CounterName, string Effect, int Amount, int Countdown, int Resistance, string? ConditionId, string? TicksTimerName);
    record OpeningDto(string Name, string Archetype, string? Requires);
    record ApproachDto(string Kind);
    record FailureDto(string Text, List<string>? Mechanics);
    record SuccessDto(string Text, List<string>? Mechanics);
    record GroupDto(
        string Id, string Category, string Title, string Body,
        int? Tier, List<string>? Requires, List<BranchDto> Branches);
    record BranchDto(string Label, string EncounterRef, string? Requires);
}
