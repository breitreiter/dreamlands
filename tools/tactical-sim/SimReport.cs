using Dreamlands.Orchestration;

namespace TacticalSim;

static class SimReport
{
    public static void Print(
        List<(string Bot, string Profile, string Encounter, List<RunResult> Results)> data,
        bool verbose)
    {
        // Header
        Console.WriteLine(
            $"{"Bot",-14} {"Profile",-32} {"Encounter",-24} {"Win%",6} {"Res%",5} {"Ctrl%",5} " +
            $"{"SpCost",7} {"Sp p10",6} {"Sp p90",6} {"Turns",6} {"T p10",5} {"T p90",5}");
        Console.WriteLine(new string('─', 140));

        string lastEncounter = "";
        foreach (var (bot, profile, encounter, results) in data)
        {
            if (encounter != lastEncounter)
            {
                if (lastEncounter != "")
                    Console.WriteLine();
                lastEncounter = encounter;
            }

            int total = results.Count;
            int wins = results.Count(r => r.Outcome != TacticalFinishReason.SpiritsLoss);
            int resKills = results.Count(r => r.Outcome == TacticalFinishReason.ResistanceKill);
            int ctrlKills = results.Count(r => r.Outcome == TacticalFinishReason.ControlKill);

            var spiritsCosts = results.Where(r => r.Outcome != TacticalFinishReason.SpiritsLoss)
                .Select(r => r.SpiritsStart - r.SpiritsEnd).OrderBy(x => x).ToList();
            var turns = results.Select(r => r.Turns).OrderBy(x => x).ToList();

            double winPct = 100.0 * wins / total;
            double resPct = total > 0 ? 100.0 * resKills / total : 0;
            double ctrlPct = total > 0 ? 100.0 * ctrlKills / total : 0;

            string spMean = spiritsCosts.Count > 0 ? $"{spiritsCosts.Average():F1}" : "-";
            string spP10 = spiritsCosts.Count > 0 ? $"{Percentile(spiritsCosts, 10)}" : "-";
            string spP90 = spiritsCosts.Count > 0 ? $"{Percentile(spiritsCosts, 90)}" : "-";

            double turnsMean = turns.Average();
            int turnsP10 = Percentile(turns, 10);
            int turnsP90 = Percentile(turns, 90);

            Console.WriteLine(
                $"{bot,-14} {profile,-32} {encounter,-24} {winPct,5:F1}% {resPct,4:F0}% {ctrlPct,4:F0}%  " +
                $"{spMean,6} {spP10,6} {spP90,6} {turnsMean,5:F1} {turnsP10,5} {turnsP90,5}");
        }

        if (verbose)
            PrintVerbose(data);
    }

    static void PrintVerbose(List<(string Bot, string Profile, string Encounter, List<RunResult> Results)> data)
    {
        Console.WriteLine();
        Console.WriteLine("=== Detailed Outcome Distribution ===");
        Console.WriteLine();

        foreach (var (bot, profile, encounter, results) in data)
        {
            int total = results.Count;
            var byOutcome = results.GroupBy(r => r.Outcome)
                .OrderByDescending(g => g.Count());

            Console.WriteLine($"{bot} | {profile} | {encounter}");
            foreach (var group in byOutcome)
                Console.WriteLine($"  {group.Key,-20} {group.Count(),5} ({100.0 * group.Count() / total:F1}%)");

            var wins = results.Where(r => r.Outcome != TacticalFinishReason.SpiritsLoss).ToList();
            if (wins.Count > 0)
            {
                var costs = wins.Select(r => r.SpiritsStart - r.SpiritsEnd).OrderBy(x => x).ToList();
                Console.WriteLine($"  Spirits cost on win:  min={costs[0]}  median={Percentile(costs, 50)}  max={costs[^1]}");
            }
            Console.WriteLine();
        }
    }

    static int Percentile(List<int> sorted, int p)
    {
        if (sorted.Count == 0) return 0;
        double index = (p / 100.0) * (sorted.Count - 1);
        int lower = (int)Math.Floor(index);
        return sorted[Math.Min(lower, sorted.Count - 1)];
    }
}
