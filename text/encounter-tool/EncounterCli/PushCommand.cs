using System.Diagnostics;

namespace EncounterCli;

static class PushCommand
{
    public static async Task<int> Run(string[] args)
    {
        var encounterPath = "";
        var world = "production";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--world" && i + 1 < args.Length) { world = args[i + 1]; i++; }
            else if (!args[i].StartsWith('-')) encounterPath = args[i];
        }

        var repoRoot = FindRepoRoot();
        if (repoRoot == null)
        {
            Console.Error.WriteLine("Could not find Dreamlands.sln — run from within the repo.");
            return 1;
        }

        if (string.IsNullOrEmpty(encounterPath))
            encounterPath = Path.Combine(repoRoot, "text/encounters");
        else
            encounterPath = Path.GetFullPath(encounterPath);

        var worldDir = Path.Combine(repoRoot, "worlds", world);

        var sw = Stopwatch.StartNew();

        // 1. Check
        Console.WriteLine("Checking encounters...");
        var checkResult = CheckCommand.Run(new[] { encounterPath });
        if (checkResult != 0)
        {
            Console.Error.WriteLine("Check failed — aborting.");
            return 1;
        }

        // 2. Bundle
        Console.WriteLine("Bundling...");
        var bundleResult = BundleCommand.Run(new[] { encounterPath, "--out", worldDir });
        if (bundleResult != 0)
        {
            Console.Error.WriteLine("Bundle failed.");
            return 1;
        }

        // 3. Reload on local GameServer
        Console.WriteLine("Reloading GameServer bundle...");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var resp = await http.PostAsync("http://localhost:7071/api/ops/reload-bundle", null);
            if (resp.IsSuccessStatusCode)
                Console.WriteLine("GameServer reloaded.");
            else
                Console.WriteLine($"Reload returned {(int)resp.StatusCode} — is GameServer running?");
        }
        catch (Exception)
        {
            Console.WriteLine("Could not reach GameServer — reload skipped.");
        }

        Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F1}s");
        return 0;
    }

    static string? FindRepoRoot()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "Dreamlands.sln"))) return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }
}
