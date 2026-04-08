using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace EncounterCli;

static class WalkCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: encounter walk <arc-dir> [--skill combat=5] [--tag foo] [--item torch] [--quality guild=3] [--gold 50]");
            return 1;
        }

        var dir = args[0];
        if (!Directory.Exists(dir))
        {
            Console.Error.WriteLine($"Directory not found: {dir}");
            return 1;
        }

        // Parse starting state flags
        var startSkills = new Dictionary<string, int>();
        var startTags = new List<string>();
        var startItems = new List<string>();
        var startQualities = new Dictionary<string, int>();
        int? startGold = null;

        for (int i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--skill" when i + 1 < args.Length:
                    ParseKeyValue(args[++i], startSkills);
                    break;
                case "--tag" when i + 1 < args.Length:
                    startTags.Add(args[++i]);
                    break;
                case "--item" when i + 1 < args.Length:
                    startItems.Add(args[++i]);
                    break;
                case "--quality" when i + 1 < args.Length:
                    ParseKeyValue(args[++i], startQualities);
                    break;
                case "--gold" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var g)) startGold = g;
                    break;
            }
        }

        // Load all .enc files
        var encounters = new Dictionary<string, Encounter>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(dir, "*.enc"))
        {
            var text = File.ReadAllText(file);
            var result = EncounterParser.Parse(text);
            if (result.Errors.Count > 0)
            {
                Console.Error.WriteLine($"Parse errors in {Path.GetFileName(file)}:");
                foreach (var err in result.Errors)
                    Console.Error.WriteLine($"  Line {err.Line}: {err.Message}");
            }
            if (result.Encounter != null)
            {
                var shortId = Path.GetFileNameWithoutExtension(file);
                encounters[shortId] = result.Encounter;
            }
        }

        if (!encounters.ContainsKey("Start"))
        {
            Console.Error.WriteLine("No Start.enc found in directory.");
            return 1;
        }

        // Create player state
        var balance = BalanceData.Default;
        var state = PlayerState.NewGame("walk", 0, balance);
        var rng = new Random(42);

        // Apply starting flags
        foreach (var (name, level) in startSkills)
        {
            var skill = Skills.FromScriptName(name);
            if (skill != null) state.Skills[skill.Value] = level;
            else Console.Error.WriteLine($"Unknown skill: {name}");
        }
        foreach (var tag in startTags) state.Tags.Add(tag);
        foreach (var item in startItems) state.Pack.Add(new ItemInstance(item, item));
        foreach (var (id, val) in startQualities) state.Qualities[id] = val;
        if (startGold.HasValue) state.Gold = startGold.Value;

        // REPL loop
        var currentId = "Start";
        while (true)
        {
            if (!encounters.TryGetValue(currentId, out var encounter))
            {
                Console.WriteLine($"\n  Could not find encounter: {currentId}");
                Console.WriteLine("  Available: " + string.Join(", ", encounters.Keys.Order()));
                break;
            }

            // Print encounter
            Console.WriteLine();
            Console.WriteLine($"═══ {encounter.Title} ═══");
            if (!string.IsNullOrWhiteSpace(encounter.Body))
            {
                Console.WriteLine();
                PrintWrapped(encounter.Body);
            }

            if (encounter.Choices.Count == 0)
            {
                Console.WriteLine("\n  (no choices — end of chain)");
                break;
            }

            // Show choices with lock state
            Console.WriteLine();
            Console.WriteLine("choices:");
            var gated = Choices.GetAllWithLockState(encounter, state, balance);
            for (int i = 0; i < gated.Count; i++)
            {
                var g = gated[i];
                var label = g.Choice.OptionPreview ?? g.Choice.OptionText;
                if (g.Locked)
                    Console.WriteLine($"  {i + 1}. {label}  \u001b[90m[LOCKED: {g.Choice.Requires}]\u001b[0m");
                else
                    Console.WriteLine($"  {i + 1}. {label}");
            }

            // Prompt for choice
            Console.Write($"\nChoose (1-{gated.Count}, or q to quit): ");
            var input = Console.ReadLine()?.Trim();
            if (input is null or "q" or "Q") break;
            if (!int.TryParse(input, out var choiceNum) || choiceNum < 1 || choiceNum > gated.Count)
            {
                Console.WriteLine("  Invalid choice.");
                continue;
            }

            var chosen = gated[choiceNum - 1];
            if (chosen.Locked)
                Console.WriteLine($"  \u001b[33m(overriding: {chosen.Choice.Requires})\u001b[0m");

            // Resolve the outcome
            var choice = chosen.Choice;
            string? preamble = null;
            string outcomeText;
            IReadOnlyList<string> mechanics;

            if (choice.Single != null)
            {
                outcomeText = choice.Single.Part.Text;
                mechanics = choice.Single.Part.Mechanics;
            }
            else if (choice.Conditional != null)
            {
                preamble = string.IsNullOrEmpty(choice.Conditional.Preamble) ? null : choice.Conditional.Preamble;

                // Evaluate branches with interactive check override
                OutcomePart? matched = null;
                foreach (var branch in choice.Conditional.Branches)
                {
                    var tokens = ActionVerb.Tokenize(branch.Condition);
                    bool passed;

                    if (tokens.Count >= 3 && tokens[0] is "check")
                    {
                        // Prompt author for pass/fail
                        Console.Write($"\n  @if {branch.Condition} — pass or fail? (p/f): ");
                        var pf = Console.ReadLine()?.Trim().ToLowerInvariant();
                        passed = pf is "p" or "pass";
                    }
                    else if (tokens.Count >= 3 && tokens[0] is "meets")
                    {
                        // Prompt author for pass/fail
                        Console.Write($"\n  @if {branch.Condition} — pass or fail? (p/f): ");
                        var pf = Console.ReadLine()?.Trim().ToLowerInvariant();
                        passed = pf is "p" or "pass";
                    }
                    else
                    {
                        // Evaluate against current state
                        passed = Conditions.Evaluate(branch.Condition, state, balance, rng);
                        var status = passed ? "\u001b[32mtrue\u001b[0m" : "\u001b[90mfalse\u001b[0m";
                        Console.WriteLine($"\n  @if {branch.Condition} → {status}");
                    }

                    if (passed)
                    {
                        matched = branch.Outcome;
                        break;
                    }
                }

                if (matched != null)
                {
                    outcomeText = matched.Text;
                    mechanics = matched.Mechanics;
                }
                else if (choice.Conditional.Fallback != null)
                {
                    Console.WriteLine("  @else");
                    outcomeText = choice.Conditional.Fallback.Text;
                    mechanics = choice.Conditional.Fallback.Mechanics;
                }
                else
                {
                    outcomeText = "";
                    mechanics = [];
                }
            }
            else
            {
                outcomeText = "";
                mechanics = [];
            }

            // Print outcome
            if (!string.IsNullOrWhiteSpace(preamble))
            {
                Console.WriteLine();
                PrintWrapped(preamble);
            }
            if (!string.IsNullOrWhiteSpace(outcomeText))
            {
                Console.WriteLine();
                PrintWrapped(outcomeText);
            }

            // Apply mechanics and print them
            if (mechanics.Count > 0)
            {
                Console.WriteLine();
                var results = Mechanics.Apply(mechanics, state, balance, rng);
                foreach (var m in mechanics)
                    Console.WriteLine($"  \u001b[36m+{m}\u001b[0m");

                // Check for navigation or terminal verbs
                string? nextId = null;
                foreach (var result in results)
                {
                    switch (result)
                    {
                        case MechanicResult.Navigation nav:
                            nextId = nav.EncounterId;
                            break;
                        case MechanicResult.DungeonFinished:
                            Console.WriteLine("\n  ═══ Arc finished ═══");
                            PrintStateSummary(state);
                            return 0;
                        case MechanicResult.DungeonFled:
                            Console.WriteLine("\n  ═══ Arc fled ═══");
                            PrintStateSummary(state);
                            return 0;
                    }
                }

                if (nextId != null)
                {
                    currentId = nextId;
                    PrintStateSidebar(state);
                    continue;
                }
            }

            // Show accumulated state
            PrintStateSidebar(state);

            // No navigation — encounter ends here
            Console.WriteLine("\n  (no +open — end of chain)");
            PrintStateSummary(state);
            break;
        }

        return 0;
    }

    static void PrintWrapped(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(line))
                Console.WriteLine();
            else
                Console.WriteLine($"  {line}");
        }
    }

    static void PrintStateSidebar(PlayerState state)
    {
        var parts = new List<string>();
        if (state.Tags.Count > 0)
            parts.Add($"tags=[{string.Join(", ", state.Tags.Order())}]");
        if (state.Qualities.Count > 0)
            parts.Add($"qualities=[{string.Join(", ", state.Qualities.OrderBy(q => q.Key).Select(q => $"{q.Key}={q.Value}"))}]");
        if (state.Pack.Count > 0)
            parts.Add($"items=[{string.Join(", ", state.Pack.Select(i => i.DefId))}]");
        if (state.Haversack.Count > 0)
            parts.Add($"haversack=[{string.Join(", ", state.Haversack.Select(i => i.DefId))}]");
        if (state.ActiveConditions.Count > 0)
            parts.Add($"conditions=[{string.Join(", ", state.ActiveConditions.OrderBy(c => c))}]");

        if (parts.Count > 0)
            Console.WriteLine($"\n  \u001b[90mstate: {string.Join("  ", parts)}\u001b[0m");
    }

    static void PrintStateSummary(PlayerState state)
    {
        Console.WriteLine("\n  Final state:");
        Console.WriteLine($"    Health: {state.Health}/{state.MaxHealth}  Spirits: {state.Spirits}/{state.MaxSpirits}  Gold: {state.Gold}");
        if (state.Tags.Count > 0)
            Console.WriteLine($"    Tags: {string.Join(", ", state.Tags.Order())}");
        if (state.Qualities.Count > 0)
            Console.WriteLine($"    Qualities: {string.Join(", ", state.Qualities.OrderBy(q => q.Key).Select(q => $"{q.Key}={q.Value}"))}");
        if (state.Pack.Count > 0)
            Console.WriteLine($"    Pack: {string.Join(", ", state.Pack.Select(i => i.DefId))}");
        if (state.Haversack.Count > 0)
            Console.WriteLine($"    Haversack: {string.Join(", ", state.Haversack.Select(i => i.DefId))}");
        if (state.ActiveConditions.Count > 0)
            Console.WriteLine($"    Conditions: {string.Join(", ", state.ActiveConditions.OrderBy(c => c))}");

        var skills = state.Skills.Where(s => s.Value != 0).OrderBy(s => s.Key);
        if (skills.Any())
            Console.WriteLine($"    Skills: {string.Join(", ", skills.Select(s => $"{s.Key}={s.Value}"))}");
    }

    static void ParseKeyValue(string arg, Dictionary<string, int> dict)
    {
        var eq = arg.IndexOf('=');
        if (eq > 0 && int.TryParse(arg[(eq + 1)..], out var val))
            dict[arg[..eq]] = val;
        else
            Console.Error.WriteLine($"Invalid key=value: {arg}");
    }
}
