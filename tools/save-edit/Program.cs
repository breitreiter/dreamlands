using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace SaveEdit;

static class Program
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    static int Run(string[] args)
    {
        // Parse optional --file <path> anywhere in args
        string? fileOverride = null;
        var rest = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--file" && i + 1 < args.Length)
            {
                fileOverride = args[++i];
            }
            else
            {
                rest.Add(args[i]);
            }
        }

        if (rest.Count == 0)
        {
            PrintUsage();
            return 1;
        }

        var path = fileOverride ?? FindLatestSave();
        Console.WriteLine($"save: {path}");

        var cmd = rest[0];
        return cmd switch
        {
            "show"        => Show(path),
            "list"        => Show(path),
            "add"         => MutateConditions(path, rest, add: true),
            "remove"      => MutateConditions(path, rest, add: false),
            "rm"          => MutateConditions(path, rest, add: false),
            _             => Unknown(cmd),
        };
    }

    static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"unknown command: {cmd}");
        PrintUsage();
        return 1;
    }

    static void PrintUsage()
    {
        Console.Error.WriteLine("usage: save-edit [--file <path>] <command> [args]");
        Console.Error.WriteLine("commands:");
        Console.Error.WriteLine("  show                    print player summary + active conditions");
        Console.Error.WriteLine("  add <condition>...      add one or more conditions");
        Console.Error.WriteLine("  remove <condition>...   remove one or more conditions");
    }

    static string FindLatestSave()
    {
        var dir = Environment.GetEnvironmentVariable("DREAMLANDS_SAVES");
        if (string.IsNullOrEmpty(dir))
        {
            var repoRoot = FindRepoRoot();
            dir = Path.Combine(repoRoot, "saves");
        }
        if (!Directory.Exists(dir))
            throw new InvalidOperationException($"saves directory not found: {dir}");

        var latest = new DirectoryInfo(dir)
            .GetFiles("*.json")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"no save files in {dir}");

        return latest.FullName;
    }

    static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Dreamlands.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("could not locate repo root (Dreamlands.sln)");
    }

    static JsonObject Load(string path)
    {
        var text = File.ReadAllText(path);
        return JsonNode.Parse(text)?.AsObject()
            ?? throw new InvalidOperationException("save file is not a JSON object");
    }

    static void Save(string path, JsonObject obj)
    {
        File.WriteAllText(path, obj.ToJsonString(JsonOpts));
    }

    static int Show(string path)
    {
        var save = Load(path);
        var name = save["name"]?.GetValue<string>() ?? "?";
        var day = save["day"]?.GetValue<int>() ?? 0;
        var hp = save["health"]?.GetValue<int>() ?? 0;
        var maxHp = save["maxHealth"]?.GetValue<int>() ?? 0;
        var sp = save["spirits"]?.GetValue<int>() ?? 0;
        var maxSp = save["maxSpirits"]?.GetValue<int>() ?? 0;

        Console.WriteLine($"{name}  day {day}  hp {hp}/{maxHp}  spirits {sp}/{maxSp}");

        var conds = (save["activeConditions"] as JsonArray)?.Select(n => n?.GetValue<string>() ?? "").ToList()
                    ?? new List<string>();
        if (conds.Count == 0)
            Console.WriteLine("active conditions: (none)");
        else
            Console.WriteLine("active conditions: " + string.Join(", ", conds));
        return 0;
    }

    static int MutateConditions(string path, List<string> rest, bool add)
    {
        if (rest.Count < 2)
        {
            Console.Error.WriteLine($"usage: {rest[0]} <condition> [<condition>...]");
            return 1;
        }

        var save = Load(path);
        var arr = save["activeConditions"] as JsonArray;
        if (arr == null)
        {
            arr = new JsonArray();
            save["activeConditions"] = arr;
        }

        var existing = new HashSet<string>(
            arr.Select(n => n?.GetValue<string>() ?? "").Where(s => s.Length > 0));

        foreach (var cond in rest.Skip(1))
        {
            if (add)
            {
                if (existing.Add(cond))
                    Console.WriteLine($"+ {cond}");
                else
                    Console.WriteLine($"= {cond} (already present)");
            }
            else
            {
                if (existing.Remove(cond))
                    Console.WriteLine($"- {cond}");
                else
                    Console.WriteLine($"? {cond} (not present)");
            }
        }

        // Rebuild array preserving sorted order for stable diffs
        var rebuilt = new JsonArray();
        foreach (var c in existing.OrderBy(s => s, StringComparer.Ordinal))
            rebuilt.Add(c);
        save["activeConditions"] = rebuilt;

        Save(path, save);
        Console.WriteLine($"saved: {path}");
        return 0;
    }
}
