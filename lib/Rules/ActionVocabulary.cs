namespace Dreamlands.Rules;

/// <summary>Where a verb may appear in encounter script.</summary>
public enum VerbUsage
{
    /// <summary>Only valid as a control-flow keyword: <c>@check skill difficulty { }</c>.</summary>
    Condition,
    /// <summary>Only valid as a game command: <c>+verb args</c>.</summary>
    Mechanic,
}

/// <summary>The type expected for a single verb argument.</summary>
public enum ArgType
{
    /// <summary>Must be a valid <see cref="Skill"/> script name.</summary>
    Skill,
    /// <summary>Must be a valid <see cref="Difficulty"/> script name.</summary>
    Difficulty,
    /// <summary>Must be a valid <see cref="TimePeriod"/> script name.</summary>
    TimePeriod,
    /// <summary>A free-form identifier (item, tag, encounter, or condition id).</summary>
    Id,
    /// <summary>A positive integer.</summary>
    Int,
    /// <summary>A signed integer skill level (e.g. -2 to +4).</summary>
    SkillLevel,
    /// <summary>A signed integer (positive or negative).</summary>
    SignedInt,
    /// <summary>An item category name (e.g. "food"). Free-form for now.</summary>
    Category,
}

/// <summary>Defines one positional argument for a verb.</summary>
public readonly record struct ArgDef(string Name, ArgType Type);

/// <summary>A single verb in the encounter action vocabulary.</summary>
public sealed class ActionVerb
{
    public string Name { get; }
    public VerbUsage Usage { get; }
    public string Description { get; }
    public IReadOnlyList<ArgDef> Args { get; }

    /// <summary>Optional boolean flags that may follow positional args (e.g. no_sleep, no_meal).</summary>
    public IReadOnlySet<string> Flags { get; }

    private ActionVerb(string name, VerbUsage usage, string description, params ArgDef[] args)
        : this(name, usage, description, args, Array.Empty<string>()) { }

    private ActionVerb(string name, VerbUsage usage, string description, ArgDef[] args, string[] flags)
    {
        Name = name;
        Usage = usage;
        Description = description;
        Args = args;
        Flags = new HashSet<string>(flags);
    }

    // ── Conditions ──────────────────────────────────────────────

    public static readonly ActionVerb Check = new("check",
        VerbUsage.Condition, "Branch on a skill check",
        new ArgDef("skill", ArgType.Skill),
        new ArgDef("difficulty", ArgType.Difficulty));

    public static readonly ActionVerb Has = new("has",
        VerbUsage.Condition, "Branch on whether player has an item",
        new ArgDef("item_id", ArgType.Id));

    public static readonly ActionVerb Tag = new("tag",
        VerbUsage.Condition, "Branch on whether a world-state tag is set",
        new ArgDef("tag_id", ArgType.Id));

    public static readonly ActionVerb Meets = new("meets",
        VerbUsage.Condition, "Branch on whether total skill bonus meets a threshold",
        new ArgDef("skill", ArgType.Skill),
        new ArgDef("target", ArgType.Int));

    public static readonly ActionVerb Quality = new("quality",
        VerbUsage.Condition, "Branch on whether a quality meets a threshold",
        new ArgDef("quality_id", ArgType.Id),
        new ArgDef("threshold", ArgType.SignedInt));

    // ── Navigation ──────────────────────────────────────────────

    public static readonly ActionVerb Open = new("open",
        VerbUsage.Mechanic, "Navigate to another encounter",
        new ArgDef("encounter_id", ArgType.Id));

    // ── World state ─────────────────────────────────────────────

    public static readonly ActionVerb AddTag = new("add_tag",
        VerbUsage.Mechanic, "Set a world-state flag",
        new ArgDef("tag_id", ArgType.Id));

    public static readonly ActionVerb RemoveTag = new("remove_tag",
        VerbUsage.Mechanic, "Clear a world-state flag",
        new ArgDef("tag_id", ArgType.Id));

    public static readonly ActionVerb SetQuality = new("quality",
        VerbUsage.Mechanic, "Adjust a numeric quality by a signed amount",
        new ArgDef("quality_id", ArgType.Id),
        new ArgDef("amount", ArgType.SignedInt));

    // ── Inventory ───────────────────────────────────────────────

    public static readonly ActionVerb AddItem = new("add_item",
        VerbUsage.Mechanic, "Give player a specific item",
        new ArgDef("item_id", ArgType.Id));

    public static readonly ActionVerb AddRandomItems = new("add_random_items",
        VerbUsage.Mechanic, "Give player random items from a category",
        new ArgDef("count", ArgType.Int),
        new ArgDef("category", ArgType.Category));

    public static readonly ActionVerb LoseRandomItem = new("lose_random_item",
        VerbUsage.Mechanic, "Player loses a random item");

    public static readonly ActionVerb Equip = new("equip",
        VerbUsage.Mechanic, "Equip an item from Pack",
        new ArgDef("item_id", ArgType.Id));

    public static readonly ActionVerb Unequip = new("unequip",
        VerbUsage.Mechanic, "Unequip an item from a gear slot",
        new ArgDef("slot", ArgType.Id));

    public static readonly ActionVerb Discard = new("discard",
        VerbUsage.Mechanic, "Discard an item from inventory",
        new ArgDef("item_id", ArgType.Id));

    // ── Pack ──────────────────────────────────────────────────────

    public static readonly ActionVerb UpgradePack = new("upgrade_pack",
        VerbUsage.Mechanic, "Permanently increase pack capacity",
        new ArgDef("amount", ArgType.Int));

    // ── Gold ────────────────────────────────────────────────────

    public static readonly ActionVerb GiveGold = new("give_gold",
        VerbUsage.Mechanic, "Give player gold",
        new ArgDef("amount", ArgType.Int));

    public static readonly ActionVerb RemGold = new("rem_gold",
        VerbUsage.Mechanic, "Take player's gold",
        new ArgDef("amount", ArgType.Int));

    // ── Spirits ────────────────────────────────────────────────

    public static readonly ActionVerb DamageSpirits = new("damage_spirits",
        VerbUsage.Mechanic, "Reduce player spirits",
        new ArgDef("amount", ArgType.Int));

    public static readonly ActionVerb HealSpirits = new("heal_spirits",
        VerbUsage.Mechanic, "Restore player spirits",
        new ArgDef("amount", ArgType.Int));

    // ── Skills ──────────────────────────────────────────────────

    public static readonly ActionVerb IncreaseSkill = new("increase_skill",
        VerbUsage.Mechanic, "Boost a skill",
        new ArgDef("skill", ArgType.Skill),
        new ArgDef("amount", ArgType.Int));

    public static readonly ActionVerb DecreaseSkill = new("decrease_skill",
        VerbUsage.Mechanic, "Reduce a skill",
        new ArgDef("skill", ArgType.Skill),
        new ArgDef("amount", ArgType.Int));

    public static readonly ActionVerb SetSkill = new("set_skill",
        VerbUsage.Mechanic, "Set a skill to a specific level",
        new ArgDef("skill", ArgType.Skill),
        new ArgDef("level", ArgType.SkillLevel));

    // ── Conditions ──────────────────────────────────────────────

    public static readonly ActionVerb AddCondition = new("add_condition",
        VerbUsage.Mechanic, "Apply a status condition",
        new ArgDef("condition_id", ArgType.Id));

    public static readonly ActionVerb RemoveCondition = new("remove_condition",
        VerbUsage.Mechanic, "Remove a status condition",
        new ArgDef("condition_id", ArgType.Id));

    // ── Time ────────────────────────────────────────────────────

    public static readonly ActionVerb SkipTime = new("skip_time",
        VerbUsage.Mechanic, "Advance to a time of day",
        new[] { new ArgDef("period", ArgType.TimePeriod) },
        new[] { "no_sleep", "no_meal", "no_biome" });

    public static readonly ActionVerb AdvanceTime = new("advance_time",
        VerbUsage.Mechanic, "Advance time by N periods",
        new[] { new ArgDef("steps", ArgType.Int) },
        new[] { "no_sleep", "no_meal", "no_biome" });

    // ── Encounter ──────────────────────────────────────────────

    public static readonly ActionVerb Repool = new("repool",
        VerbUsage.Mechanic, "Return this encounter to the available pool");

    // ── Dungeon ─────────────────────────────────────────────────

    public static readonly ActionVerb FinishDungeon = new("finish_dungeon",
        VerbUsage.Mechanic, "Mark current dungeon as completed");

    public static readonly ActionVerb FleeDungeon = new("flee_dungeon",
        VerbUsage.Mechanic, "Exit dungeon without completing it");

    // ── Registry ────────────────────────────────────────────────

    /// <summary>All defined verbs.</summary>
    public static IReadOnlyList<ActionVerb> All { get; } = new ActionVerb[]
    {
        Check, Has, Tag, Meets, Quality,
        Open,
        AddTag, RemoveTag, SetQuality,
        AddItem, AddRandomItems, LoseRandomItem,
        Equip, Unequip, Discard, UpgradePack,
        GiveGold, RemGold,
        DamageSpirits, HealSpirits,
        IncreaseSkill, DecreaseSkill, SetSkill,
        AddCondition, RemoveCondition,
        SkipTime,
        Repool,
        FinishDungeon, FleeDungeon,
    };

    private static readonly Dictionary<(string Name, VerbUsage Usage), ActionVerb> ByNameAndUsage =
        All.ToDictionary(v => (v.Name, v.Usage));

    /// <summary>Look up a verb by its script name. Returns null if not recognised.</summary>
    public static ActionVerb? FromName(string name) =>
        ByNameAndUsage.TryGetValue((name, VerbUsage.Condition), out var c) ? c :
        ByNameAndUsage.TryGetValue((name, VerbUsage.Mechanic), out var m) ? m : null;

    /// <summary>Look up a verb by its script name and expected usage. Returns null if not recognised.</summary>
    public static ActionVerb? FromName(string name, VerbUsage usage) =>
        ByNameAndUsage.TryGetValue((name, usage), out var verb) ? verb : null;

    /// <summary>True if <paramref name="name"/> matches a known verb.</summary>
    public static bool IsValidName(string name) =>
        FromName(name) != null;

    /// <summary>
    /// Validate a full action string (e.g. "check combat hard" or "damage_health small").
    /// Returns null on success, or an error message describing the problem.
    /// </summary>
    public static string? Validate(string action, VerbUsage expectedUsage)
    {
        var tokens = Tokenize(action);
        if (tokens.Count == 0)
            return "Empty action.";

        var verbName = tokens[0];
        var verb = FromName(verbName, expectedUsage);
        if (verb == null)
        {
            // Check if verb exists with opposite usage for a better error message
            var otherUsage = expectedUsage == VerbUsage.Condition ? VerbUsage.Mechanic : VerbUsage.Condition;
            var other = FromName(verbName, otherUsage);
            if (other != null)
            {
                var expected = expectedUsage == VerbUsage.Condition ? "condition" : "mechanic";
                var actual = other.Usage == VerbUsage.Condition ? "condition" : "mechanic";
                return $"'{verbName}' is a {actual} but was used as a {expected}.";
            }
            return $"Unknown verb '{verbName}'.";
        }

        var argTokens = tokens.GetRange(1, Math.Min(verb.Args.Count, tokens.Count - 1));
        if (argTokens.Count != verb.Args.Count)
            return $"'{verbName}' expects {verb.Args.Count} argument(s) but got {tokens.Count - 1}.";

        for (int i = 0; i < verb.Args.Count; i++)
        {
            var def = verb.Args[i];
            var value = argTokens[i];
            var err = ValidateArg(def, value);
            if (err != null)
                return $"'{verbName}' argument '{def.Name}': {err}";
        }

        // Validate any trailing flags.
        var trailing = tokens.GetRange(1 + verb.Args.Count, tokens.Count - 1 - verb.Args.Count);
        if (trailing.Count > 0 && verb.Flags.Count == 0)
            return $"'{verbName}' does not accept flags, but got: {string.Join(", ", trailing)}.";

        var seen = new HashSet<string>();
        foreach (var flag in trailing)
        {
            if (!verb.Flags.Contains(flag))
                return $"'{verbName}': unknown flag '{flag}'. Valid flags: {string.Join(", ", verb.Flags)}.";
            if (!seen.Add(flag))
                return $"'{verbName}': duplicate flag '{flag}'.";
        }

        return null;
    }

    /// <summary>
    /// Split an action string into tokens, respecting double-quoted strings.
    /// <c>open "The Ghosts"</c> yields <c>["open", "The Ghosts"]</c>.
    /// </summary>
    public static List<string> Tokenize(string action)
    {
        var tokens = new List<string>();
        var span = action.AsSpan().Trim();
        while (span.Length > 0)
        {
            if (span[0] == '"')
            {
                span = span[1..];
                var close = span.IndexOf('"');
                if (close < 0)
                {
                    tokens.Add(span.ToString());
                    break;
                }
                tokens.Add(span[..close].ToString());
                span = span[(close + 1)..].TrimStart();
            }
            else
            {
                var space = span.IndexOf(' ');
                if (space < 0)
                {
                    tokens.Add(span.ToString());
                    break;
                }
                tokens.Add(span[..space].ToString());
                span = span[(space + 1)..].TrimStart();
            }
        }
        return tokens;
    }

    private static string? ValidateArg(ArgDef def, string value)
    {
        return def.Type switch
        {
            ArgType.Skill => Skills.IsValidScriptName(value)
                ? null : $"'{value}' is not a valid skill. Expected one of: {string.Join(", ", Skills.All.Select(s => s.ScriptName))}.",
            ArgType.Difficulty => Difficulties.IsValidScriptName(value)
                ? null : $"'{value}' is not a valid difficulty. Expected one of: {string.Join(", ", Difficulties.All.Select(d => d.ScriptName))}.",
            ArgType.TimePeriod => TimePeriods.IsValidScriptName(value)
                ? null : $"'{value}' is not a valid time period. Expected one of: {string.Join(", ", TimePeriods.All.Select(t => t.ScriptName))}.",
            ArgType.Id => string.IsNullOrWhiteSpace(value)
                ? "identifier must not be empty." : null,
            ArgType.Int => int.TryParse(value, out var n) && n > 0
                ? null : $"'{value}' is not a positive integer.",
            ArgType.SkillLevel => int.TryParse(value, out _)
                ? null : $"'{value}' is not a valid integer.",
            ArgType.SignedInt => int.TryParse(value, out _)
                ? null : $"'{value}' is not a valid integer.",
            ArgType.Category => string.IsNullOrWhiteSpace(value)
                ? "category must not be empty." : null,
            _ => $"unknown argument type {def.Type}.",
        };
    }
}
