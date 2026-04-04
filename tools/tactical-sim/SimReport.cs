namespace TacticalSim;

static class SimReport
{
    public static void PrintDetailed(List<RunVibes> runs, string label)
    {
        int n = runs.Count;
        int maxTurn = runs.Max(r => r.Turns.Count);

        Console.WriteLine();
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"  {label}  ({n:N0} runs)");
        Console.WriteLine(new string('=', 70));
        Console.WriteLine($"{"Turn",4}  {"Choice",7}  {"Tension",7}  {"Juice",7}  {"Weight",7}  {"Triumph",7}");
        Console.WriteLine($"{"────",4}  {"───────",7}  {"───────",7}  {"───────",7}  {"───────",7}  {"───────",7}");

        for (int t = 1; t <= maxTurn; t++)
        {
            var turns = runs.Where(r => r.Turns.Count >= t).Select(r => r.Turns[t - 1]).ToList();
            if (turns.Count == 0) break;
            double pctActive = (double)turns.Count / n;

            double avgChoice = turns.Average(v => v.Choice);
            double avgTension = turns.Average(v => v.Tension);
            double avgJuice = turns.Average(v => v.Juice);
            double avgWeight = turns.Average(v => v.Weight);
            double avgTriumph = turns.Average(v => v.Triumph);

            string active = pctActive < 0.95 ? $" ({pctActive:P0})" : "";
            Console.WriteLine(
                $"  {t,2}{active,5}" +
                $"  {avgChoice,7:F2}" +
                $"  {avgTension,7:F2}" +
                $"  {avgJuice,7:F2}" +
                $"  {avgWeight,+7:F2}" +
                $"  {avgTriumph,7:F2}");
        }

        PrintSummary(runs);
    }

    public static void PrintCompact(List<RunVibes> runs, string label)
    {
        int n = runs.Count;
        var allTurns = runs.SelectMany(r => r.Turns).ToList();
        double avgLen = runs.Average(r => r.Turns.Count);
        double avgJuice = allTurns.Count > 0 ? allTurns.Average(t => t.Juice) : 0;
        double avgChoice = allTurns.Count > 0 ? allTurns.Average(t => t.Choice) : 0;
        double oofRate = (double)runs.Count(r => r.Turns.Any(t => t.Weight <= -0.7)) / n;
        double clickerRate = (double)ClickerRuns(runs) / n;
        double droughtRate = (double)DroughtRuns(runs) / n;

        Console.WriteLine(
            $"  {label,-50}" +
            $"  len={avgLen,4:F1}" +
            $"  juice={avgJuice:F2}" +
            $"  choice={avgChoice:F2}" +
            $"  oof={oofRate,4:P0}" +
            $"  clicker={clickerRate,4:P0}" +
            $"  drought={droughtRate,4:P0}");
    }

    static void PrintSummary(List<RunVibes> runs)
    {
        int n = runs.Count;
        var allTurns = runs.SelectMany(r => r.Turns).ToList();
        double avgLen = runs.Average(r => r.Turns.Count);
        double avgJuice = allTurns.Count > 0 ? allTurns.Average(t => t.Juice) : 0;
        double avgChoice = allTurns.Count > 0 ? allTurns.Average(t => t.Choice) : 0;
        var spiritsList = runs.Select(r => r.SpiritsSpent).ToList();
        int oofRuns = runs.Count(r => r.Turns.Any(t => t.Weight <= -0.7));
        int clickerRuns = ClickerRuns(runs);
        int droughtRuns = DroughtRuns(runs);

        Console.WriteLine();
        Console.WriteLine($"  Avg length:    {avgLen:F1} turns");
        Console.WriteLine($"  Avg juice:     {avgJuice:F2}");
        Console.WriteLine($"  Avg choice:    {avgChoice:F2}");
        Console.WriteLine($"  Spirits lost:  {spiritsList.Average():F1} avg, {spiritsList.Max()} worst");
        Console.WriteLine($"  Oof rate:      {(double)oofRuns / n:P1} (weight <= -0.7)");
        Console.WriteLine($"  Clicker rate:  {(double)clickerRuns / n:P1} (choice < 0.15 for 3+ turns)");
        Console.WriteLine($"  Juice drought: {(double)droughtRuns / n:P1} (juice < 0.25 for 3+ turns)");
    }

    static int ClickerRuns(List<RunVibes> runs) =>
        runs.Count(r => HasStreak(r.Turns, t => t.Choice < 0.15, 3));

    static int DroughtRuns(List<RunVibes> runs) =>
        runs.Count(r => HasStreak(r.Turns, t => t.Juice < 0.25, 3));

    static bool HasStreak(List<TurnVibe> turns, Func<TurnVibe, bool> pred, int length)
    {
        int streak = 0;
        foreach (var t in turns)
        {
            streak = pred(t) ? streak + 1 : 0;
            if (streak >= length) return true;
        }
        return false;
    }

    // ── Trace mode ──────────────────────────────────────────────

    public static void PrintTrace(TraceResult trace, string label)
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"  TRACE: {label}");
        Console.WriteLine(new string('=', 80));

        foreach (var t in trace.Turns)
        {
            var v = t.Vibe;
            Console.WriteLine();
            Console.WriteLine($"  ── Turn {v.Turn} ──────────────────────────────────────────────");
            Console.WriteLine($"  State:  M={t.Momentum}  Sp={t.Spirits}  Clock={t.Clock}  Resist={t.Resistance}/{t.ResistanceMax}");

            // Challenges
            foreach (var (name, resistance, cleared) in t.Challenges)
            {
                string status = cleared ? "CLEARED" : $"resist={resistance}";
                Console.WriteLine($"  Challenge:  {name} [{status}]");
            }

            // Hand
            Console.WriteLine($"  Hand ({t.Hand.Count} cards):");
            for (int i = 0; i < t.Hand.Count; i++)
            {
                var c = t.Hand[i];
                string afford = c.Affordable ? " " : "X";
                string cost = c.CostKind == "Free" ? "free" : $"{c.CostKind} {c.CostAmount}";
                string effect = $"{c.EffectKind} {c.EffectAmount}";
                string marker = v.Action == "play" && c.Name == v.CardPlayed ? " <──" : "";
                Console.WriteLine($"    [{afford}] {c.Name,-35} {cost,-14} → {effect}{marker}");
            }

            // Decision
            Console.ForegroundColor = v.Action switch
            {
                "press" => ConsoleColor.Yellow,
                "force" => ConsoleColor.Red,
                "stuck" => ConsoleColor.DarkRed,
                _ => ConsoleColor.Green,
            };
            Console.WriteLine($"  Decision: {v.Action.ToUpper()} — {t.Reasoning}");
            Console.ResetColor();

            if (v.Action == "play")
                Console.WriteLine($"  Played:   {v.CardPlayed}");

            // Vibe scores
            Console.Write("  Vibe:    ");
            WriteScore("choice", v.Choice);
            WriteScore("tension", v.Tension);
            WriteScore("juice", v.Juice);
            WriteScore("weight", v.Weight, signed: true);
            if (v.Triumph > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(" TRIUMPH!");
                Console.ResetColor();
            }
            Console.WriteLine();

            if (v.SpiritsLost > 0)
                Console.WriteLine($"  Cost:     -{v.SpiritsLost} spirits");
        }

        // Outcome
        Console.WriteLine();
        Console.ForegroundColor = trace.Won ? ConsoleColor.Green : ConsoleColor.Red;
        Console.WriteLine($"  ── Result: {trace.FinishReason} ── ({trace.Turns.Count} turns, {trace.SpiritsSpent} spirits spent)");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void WriteScore(string name, double value, bool signed = false)
    {
        var color = value switch
        {
            >= 0.7 => ConsoleColor.Green,
            >= 0.4 => ConsoleColor.Yellow,
            _ when value < -0.3 => ConsoleColor.Red,
            _ => ConsoleColor.DarkGray,
        };
        Console.ForegroundColor = color;
        Console.Write(signed ? $" {name}={value:+0.00;-0.00}" : $" {name}={value:F2}");
        Console.ResetColor();
    }
}
