using System.Text.Json;

namespace Dreamlands.Encounter;

public sealed class EncounterBundle
{
    public IReadOnlyList<Encounter> Encounters { get; }
    readonly Dictionary<string, BundleIndex> _byId;
    readonly Dictionary<string, IReadOnlyList<int>> _byCategory;

    EncounterBundle(List<Encounter> encounters, Dictionary<string, BundleIndex> byId,
        Dictionary<string, IReadOnlyList<int>> byCategory)
    {
        Encounters = encounters;
        _byId = byId;
        _byCategory = byCategory;
    }

    public Encounter? GetById(string id) =>
        _byId.TryGetValue(id, out var idx) ? Encounters[idx.EncounterIndex] : null;

    public IReadOnlyList<Encounter> GetByCategory(string category) =>
        _byCategory.TryGetValue(category, out var indices)
            ? indices.Select(i => Encounters[i]).ToList()
            : [];

    public IReadOnlyList<string> GetCategories() => _byCategory.Keys.ToList();

    public IReadOnlyList<Encounter> GetByTrigger(string trigger, string? biome = null, int? tier = null) =>
        Encounters
            .Where(e => string.Equals(e.Trigger, trigger, StringComparison.OrdinalIgnoreCase))
            .Where(e => biome == null || e.Category.Split('/').Contains(biome))
            .Where(e => tier == null || e.Tier == null || e.Tier == tier)
            .ToList();

    public static EncounterBundle Load(string path)
    {
        var json = File.ReadAllText(path);
        return FromJson(json);
    }

    public static EncounterBundle FromJson(string json)
    {
        var doc = JsonSerializer.Deserialize<BundleDto>(json, JsonOpts)
            ?? throw new InvalidOperationException("Failed to deserialize bundle JSON.");

        var encounters = new List<Encounter>();
        foreach (var e in doc.Encounters)
        {
            var choices = new List<Choice>();
            foreach (var c in e.Choices)
            {
                ConditionalOutcome? conditional = null;
                SingleOutcome? single = null;

                if (c.Conditional is { } cond)
                {
                    var branches = cond.Branches.Select(b => new ConditionalBranch
                    {
                        Condition = b.Condition,
                        Outcome = new OutcomePart { Text = b.Text, Mechanics = b.Mechanics }
                    }).ToList();

                    OutcomePart? fallback = cond.Fallback is { } fb
                        ? new OutcomePart { Text = fb.Text, Mechanics = fb.Mechanics }
                        : null;

                    conditional = new ConditionalOutcome
                    {
                        Preamble = cond.Preamble,
                        Branches = branches,
                        Fallback = fallback
                    };
                }
                else if (c.Single is { } s)
                {
                    single = new SingleOutcome
                    {
                        Part = new OutcomePart { Text = s.Text, Mechanics = s.Mechanics }
                    };
                }

                choices.Add(new Choice
                {
                    OptionText = c.OptionText,
                    OptionLink = c.OptionLink,
                    OptionPreview = c.OptionPreview,
                    Requires = c.Requires,
                    Conditional = conditional,
                    Single = single
                });
            }

            encounters.Add(new Encounter
            {
                Id = e.Id,
                Category = e.Category,
                Recurring = e.Recurring,
                Trigger = e.Trigger,
                Tier = e.Tier,
                Title = e.Title,
                Body = e.Body,
                Requires = e.Requires ?? [],
                Choices = choices
            });
        }

        var byId = new Dictionary<string, BundleIndex>();
        if (doc.Index.ById != null)
        {
            foreach (var (id, idx) in doc.Index.ById)
                byId[id] = new BundleIndex(idx.Category, idx.EncounterIndex);
        }

        var byCategory = new Dictionary<string, IReadOnlyList<int>>();
        if (doc.Index.ByCategory != null)
        {
            foreach (var (cat, indices) in doc.Index.ByCategory)
                byCategory[cat] = indices;
        }

        return new EncounterBundle(encounters, byId, byCategory);
    }

    static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // Private DTOs matching bundle JSON shape
    record BundleDto(IndexDto Index, List<EncounterDto> Encounters);
    record IndexDto(Dictionary<string, IndexEntryDto>? ById, Dictionary<string, List<int>>? ByCategory);
    record IndexEntryDto(string Category, int EncounterIndex);
    record EncounterDto(string Id, string Category, bool Recurring, string? Trigger, int? Tier, string Title, string Body, List<string>? Requires, List<ChoiceDto> Choices);
    record ChoiceDto(string OptionText, string? OptionLink, string? OptionPreview, string? Requires,
        ConditionalDto? Conditional, SingleDto? Single);
    record ConditionalDto(string Preamble, List<BranchDto> Branches, OutcomePartDto? Fallback);
    record BranchDto(string Condition, string Text, List<string> Mechanics);
    record SingleDto(string Text, List<string> Mechanics);
    record OutcomePartDto(string Text, List<string> Mechanics);
}

public record BundleIndex(string Category, int EncounterIndex);
