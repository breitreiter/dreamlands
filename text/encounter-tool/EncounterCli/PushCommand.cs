using System.Diagnostics;

namespace EncounterCli;

static class PushCommand
{
    public static async Task<int> RunAsync(string[] args)
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

        var appName = Environment.GetEnvironmentVariable("DREAMLANDS_FUNCTION_APP");
        var functionKey = Environment.GetEnvironmentVariable("DREAMLANDS_FUNCTION_KEY");
        if (string.IsNullOrEmpty(appName) || string.IsNullOrEmpty(functionKey))
        {
            Console.Error.WriteLine("Set DREAMLANDS_FUNCTION_APP and DREAMLANDS_FUNCTION_KEY environment variables.");
            return 1;
        }

        var sw = Stopwatch.StartNew();

        // 1. Check
        Console.WriteLine("Checking encounters...");
        var checkResult = CheckCommand.Run(new[] { encounterPath });
        if (checkResult != 0)
        {
            Console.Error.WriteLine("Check failed — aborting push.");
            return 1;
        }

        // 2. Bundle
        Console.WriteLine("Bundling...");
        var bundleResult = BundleCommand.Run(new[] { encounterPath, "--out", worldDir });
        if (bundleResult != 0)
        {
            Console.Error.WriteLine("Bundle failed — aborting push.");
            return 1;
        }

        var bundlePath = Path.Combine(worldDir, "encounters.bundle.json");

        // 3. Upload via Azure CLI
        Console.WriteLine("Uploading bundle...");
        var azResult = await RunProcessAsync("az", new[]
        {
            "functionapp", "deploy",
            "--name", appName,
            "-g", "dreamlands-rg",
            "--src-path", bundlePath,
            "--target-path", "data/encounters.bundle.json",
            "--type", "static"
        });
        if (azResult != 0)
        {
            Console.Error.WriteLine("Azure upload failed.");
            return 1;
        }

        // 4. Reload
        Console.WriteLine("Reloading server bundle...");
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("x-functions-key", functionKey);
        var response = await http.PostAsync(
            $"https://{appName}.azurewebsites.net/api/ops/reload-bundle", null);
        if (!response.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Reload failed: {response.StatusCode}");
            return 1;
        }

        Console.WriteLine($"Done in {sw.Elapsed.TotalSeconds:F1}s");
        return 0;
    }

    static async Task<int> RunProcessAsync(string fileName, string[] arguments)
    {
        var psi = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        foreach (var arg in arguments)
            psi.ArgumentList.Add(arg);

        var proc = Process.Start(psi)!;
        // Drain output to avoid deadlocks
        var stdout = proc.StandardOutput.ReadToEndAsync();
        var stderr = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
        {
            var err = await stderr;
            if (!string.IsNullOrWhiteSpace(err))
                Console.Error.Write(err);
        }

        return proc.ExitCode;
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
