using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Encounter;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;

namespace EncounterCli;

static class WalkTacticalCommand
{
    public static int Run(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: encounter walk-tac <file.tac> [--skill combat=5] [--tag foo] [--item torch] [--quality guild=3] [--gold 50] [--seed N]");
            return 1;
        }

        var file = args[0];
        if (!File.Exists(file))
        {
            Console.Error.WriteLine($"File not found: {file}");
            return 1;
        }

        // Parse flags
        var startSkills = new Dictionary<string, int>();
        var startTags = new List<string>();
        var startItems = new List<string>();
        var startQualities = new Dictionary<string, int>();
        int? startGold = null;
        int seed = 42;

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
                case "--seed" when i + 1 < args.Length:
                    if (int.TryParse(args[++i], out var s)) seed = s;
                    break;
            }
        }

        // Parse the .tac file
        var source = File.ReadAllText(file);
        var result = TacticalParser.Parse(source);

        if (result.Errors.Count > 0)
        {
            Console.Error.WriteLine("Parse errors:");
            foreach (var err in result.Errors)
                Console.Error.WriteLine($"  {err}");
            return 1;
        }

        if (result.Encounter == null)
        {
            Console.Error.WriteLine("File parsed as a group (branches:), not an encounter. walk-tac only supports encounters.");
            return 1;
        }

        var encounter = result.Encounter;

        // Create minimal session
        var balance = BalanceData.Default;
        var map = new Dreamlands.Map.Map(1, 1);
        map[0, 0].Terrain = Terrain.Plains;
        var bundle = EncounterBundle.FromJson("""{"index":{"byId":{},"byCategory":{}},"encounters":[]}""");
        var player = PlayerState.NewGame("walk-tac", seed, balance);

        // Apply starting state
        foreach (var (name, level) in startSkills)
        {
            var skill = Skills.FromScriptName(name);
            if (skill != null) player.Skills[skill.Value] = level;
            else Console.Error.WriteLine($"Unknown skill: {name}");
        }
        foreach (var tag in startTags) player.Tags.Add(tag);
        foreach (var item in startItems) player.Pack.Add(new ItemInstance(item, item));
        foreach (var (id, val) in startQualities) player.Qualities[id] = val;
        if (startGold.HasValue) player.Gold = startGold.Value;

        var session = new GameSession(player, map, bundle, balance, new Random(seed));
        var state = new TacticalState();

        // Print encounter header
        Console.WriteLine();
        Console.WriteLine($"═══ {encounter.Title} ═══");
        if (encounter.Stat != null)
            Console.WriteLine($"\u001b[90m[stat: {encounter.Stat}]\u001b[0m");
        if (!string.IsNullOrWhiteSpace(encounter.Body))
        {
            Console.WriteLine();
            PrintWrapped(encounter.Body);
        }

        // Print encounter overview
        Console.WriteLine();
        Console.WriteLine($"\u001b[90mclock: {encounter.Clock} turns\u001b[0m");
        Console.WriteLine("\u001b[90mchallenges:\u001b[0m");
        foreach (var c in encounter.Challenges)
        {
            var counter = c.CounterName != null ? $" [counter: {c.CounterName}]" : "";
            Console.WriteLine($"  \u001b[90m• {c.Name}: resistance {c.Resistance}{counter}\u001b[0m");
        }

        // Begin
        var step = TacticalRunner.Begin(session, encounter, state);

        // REPL loop
        while (true)
        {
            switch (step)
            {
                case TacticalStep.ChooseApproach ca:
                    step = HandleChooseApproach(ca, session, encounter, state);
                    break;

                case TacticalStep.ShowTurn st:
                    step = HandleShowTurn(st, session, encounter, state);
                    break;

                case TacticalStep.Finished fin:
                    HandleFinished(fin, encounter, session);
                    return 0;

                default:
                    Console.WriteLine("Unknown step type.");
                    return 1;
            }

            if (step == null) return 0; // user quit
        }
    }

    static TacticalStep? HandleChooseApproach(
        TacticalStep.ChooseApproach ca,
        GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        Console.WriteLine();
        Console.WriteLine("Choose your approach:");
        for (int i = 0; i < ca.Approaches.Count; i++)
        {
            var a = ca.Approaches[i];
            var desc = a.Kind == ApproachKind.Aggressive
                ? "+2 momentum/turn, draw 1 card"
                : "+1 momentum/turn, draw 2 cards";
            Console.WriteLine($"  {i + 1}. {a.Kind}  \u001b[90m({desc})\u001b[0m");
        }

        Console.Write($"\nChoose (1-{ca.Approaches.Count}, or q to quit): ");
        var input = Console.ReadLine()?.Trim();
        if (input is null or "q" or "Q") return null;
        if (!int.TryParse(input, out var choice) || choice < 1 || choice > ca.Approaches.Count)
        {
            Console.WriteLine("  Invalid choice, defaulting to Aggressive.");
            choice = 1;
        }

        return TacticalRunner.ApplyApproach(session, encounter, state, ca.Approaches[choice - 1].Kind);
    }

    static TacticalStep? HandleShowTurn(
        TacticalStep.ShowTurn st,
        GameSession session, TacticalEncounter encounter, TacticalState state)
    {
        var data = st.Data;

        Console.WriteLine();
        Console.WriteLine($"── Turn {data.Turn} ──────────────────────────────");

        // Status line
        Console.Write($"  Spirits: {data.PlayerSpirits}  Momentum: {data.Momentum}  Clock: {data.Clock}");
        Console.WriteLine();

        // Challenge status
        for (int i = 0; i < data.Challenges.Count; i++)
        {
            var c = data.Challenges[i];
            if (c.Cleared)
            {
                Console.WriteLine($"  \u001b[90m  ✓ {c.Name}\u001b[0m");
                continue;
            }
            var marker = i == data.CurrentChallengeIndex ? " ◆" : "";
            Console.WriteLine($"  \u001b[90m  {c.Name}{marker}: resistance {c.Resistance}/{c.MaxResistance}\u001b[0m");
        }

        // Show openings
        Console.WriteLine();
        Console.WriteLine("Openings:");
        for (int i = 0; i < data.Openings.Count; i++)
        {
            var o = data.Openings[i];
            var cost = FormatCost(o);
            var effect = FormatEffect(o);
            Console.WriteLine($"  {i + 1}. {o.Name}  \u001b[90m{cost} → {effect}\u001b[0m");
        }

        // Extra actions
        var tb = session.Balance.Tactical;
        var extras = new List<string>();
        if (!data.DigUsed && state.Momentum >= tb.PressAdvantageCost)
            extras.Add($"p = Press Advantage (spend {tb.PressAdvantageCost} momentum, draw 2)");
        if (!data.DigUsed && session.Player.Spirits >= tb.ForceOpeningCost)
            extras.Add($"f = Force Opening (spend {tb.ForceOpeningCost} spirits, draw 2)");

        if (extras.Count > 0)
        {
            Console.WriteLine();
            foreach (var e in extras)
                Console.WriteLine($"  \u001b[90m{e}\u001b[0m");
        }

        // Prompt
        Console.Write($"\nAction (1-{data.Openings.Count}{(extras.Count > 0 ? ", p, f" : "")}, or q): ");
        var input = Console.ReadLine()?.Trim()?.ToLowerInvariant();
        if (input is null or "q") return null;

        if (input == "p" && !data.DigUsed)
            return TacticalRunner.Act(session, encounter, state, TacticalAction.PressAdvantage);
        if (input == "f" && !data.DigUsed)
            return TacticalRunner.Act(session, encounter, state, TacticalAction.ForceOpening);

        if (int.TryParse(input, out var idx) && idx >= 1 && idx <= data.Openings.Count)
        {
            var opening = data.Openings[idx - 1];
            // Check affordability
            if (opening.CostKind == CostKind.Momentum && state.Momentum < opening.CostAmount)
            {
                Console.WriteLine($"  \u001b[33mNot enough momentum ({state.Momentum}/{opening.CostAmount})\u001b[0m");
                return new TacticalStep.ShowTurn(data); // re-show same turn
            }
            if (opening.CostKind == CostKind.Spirits && session.Player.Spirits < opening.CostAmount)
            {
                Console.WriteLine($"  \u001b[33mNot enough spirits ({session.Player.Spirits}/{opening.CostAmount})\u001b[0m");
                return new TacticalStep.ShowTurn(data);
            }
            return TacticalRunner.Act(session, encounter, state, TacticalAction.TakeOpening, idx - 1);
        }

        Console.WriteLine("  Invalid input.");
        return new TacticalStep.ShowTurn(data); // re-show
    }

    static void HandleFinished(TacticalStep.Finished fin, TacticalEncounter encounter, GameSession session)
    {
        Console.WriteLine();

        var (label, color) = fin.Reason switch
        {
            TacticalFinishReason.ResistanceKill => ("VICTORY (resistance depleted)", "\u001b[32m"),
            TacticalFinishReason.ControlKill => ("VICTORY (challenge cancelled)", "\u001b[32m"),
            TacticalFinishReason.SpiritsLoss => ("DEFEAT (spirits exhausted)", "\u001b[31m"),
            TacticalFinishReason.ClockExpired => ("DEFEAT (clock expired)", "\u001b[31m"),
            _ => ("FINISHED", "\u001b[0m")
        };
        Console.WriteLine($"{color}═══ {label} ═══\u001b[0m");

        bool isVictory = fin.Reason is TacticalFinishReason.ResistanceKill or TacticalFinishReason.ControlKill;

        if (!isVictory && encounter.Failure != null && !string.IsNullOrWhiteSpace(encounter.Failure.Text))
        {
            Console.WriteLine();
            PrintWrapped(encounter.Failure.Text);
        }

        if (isVictory && encounter.Success != null && !string.IsNullOrWhiteSpace(encounter.Success.Text))
        {
            Console.WriteLine();
            PrintWrapped(encounter.Success.Text);
        }

        if (fin.FailureResults is { Count: > 0 })
        {
            Console.WriteLine();
            Console.WriteLine("  \u001b[31mFailure mechanics:\u001b[0m");
            foreach (var r in fin.FailureResults)
                Console.WriteLine($"    \u001b[31m{r}\u001b[0m");
        }

        if (fin.SuccessResults is { Count: > 0 })
        {
            Console.WriteLine();
            Console.WriteLine("  \u001b[32mSuccess mechanics:\u001b[0m");
            foreach (var r in fin.SuccessResults)
                Console.WriteLine($"    \u001b[32m{r}\u001b[0m");
        }

        // Final state
        var player = session.Player;
        Console.WriteLine();
        Console.WriteLine("  Final state:");
        Console.WriteLine($"    Health: {player.Health}/{player.MaxHealth}  Spirits: {player.Spirits}/{player.MaxSpirits}  Gold: {player.Gold}");
        if (player.ActiveConditions.Count > 0)
            Console.WriteLine($"    Conditions: {string.Join(", ", player.ActiveConditions.OrderBy(c => c.Key).Select(c => $"{c.Key}({c.Value})"))}");
    }

    static string FormatCost(OpeningSnapshot o) => o.CostKind switch
    {
        CostKind.Free => "free",
        CostKind.Momentum => $"{o.CostAmount} momentum",
        CostKind.Spirits => $"{o.CostAmount} spirits",
        CostKind.Tick => "tick",
        _ => "?"
    };

    static string FormatEffect(OpeningSnapshot o) => o.EffectKind switch
    {
        EffectKind.Damage => $"damage {o.EffectAmount}",
        EffectKind.StopTimer => "cancel challenge",
        EffectKind.Momentum => $"+{o.EffectAmount} momentum",
        _ => "?"
    };

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

    static void ParseKeyValue(string arg, Dictionary<string, int> dict)
    {
        var eq = arg.IndexOf('=');
        if (eq > 0 && int.TryParse(arg[(eq + 1)..], out var val))
            dict[arg[..eq]] = val;
        else
            Console.Error.WriteLine($"Invalid key=value: {arg}");
    }
}
