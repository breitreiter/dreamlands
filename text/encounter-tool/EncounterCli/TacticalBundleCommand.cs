using System.Text.Json;
using Dreamlands.Tactical;

namespace EncounterCli;

static class TacticalBundleCommand
{
    public static int Run(string[] args)
    {
        string path = "";
        var outDir = ".";
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outDir = args[i + 1]; i++; }
            else if (!args[i].StartsWith('-')) path = args[i];
        }

        if (string.IsNullOrEmpty(path))
        {
            Console.Error.WriteLine("bundle-tactical requires <path> to the tactical directory.");
            return 1;
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Path not found: {path}");
            return 1;
        }

        var files = Directory.GetFiles(path, "*.tac", SearchOption.AllDirectories)
            .OrderBy(f => f).ToArray();
        if (files.Length == 0)
        {
            Console.Error.WriteLine($"No .tac files found under {path}");
            return 1;
        }

        var encounters = new List<object>();
        var groups = new List<object>();
        var encountersById = new Dictionary<string, int>();
        var groupsById = new Dictionary<string, int>();
        var encountersByCategory = new Dictionary<string, List<int>>();
        var errors = 0;

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var category = Path.GetDirectoryName(rel)?.Replace('\\', '/') ?? "";
            var shortId = Path.GetFileNameWithoutExtension(file);
            var id = string.IsNullOrEmpty(category) ? shortId : $"{category}/{shortId}";
            var text = File.ReadAllText(file);
            var result = TacticalParser.Parse(text);

            if (!result.IsSuccess)
            {
                Console.Error.WriteLine($"Skip {rel}: parse errors");
                foreach (var err in result.Errors)
                    Console.Error.WriteLine($"  {err}");
                errors++;
                continue;
            }

            if (result.Encounter is { } enc)
            {
                var doc = EncounterToJson(enc, id, category);
                encounters.Add(doc);
                encountersById[id] = encounters.Count - 1;
                if (!encountersByCategory.TryGetValue(category, out var list))
                {
                    list = [];
                    encountersByCategory[category] = list;
                }
                list.Add(encounters.Count - 1);
            }
            else if (result.Group is { } grp)
            {
                var doc = GroupToJson(grp, id, category);
                groups.Add(doc);
                groupsById[id] = groups.Count - 1;
            }
        }

        if (errors > 0)
            Console.Error.WriteLine($"{errors} file(s) skipped due to errors.");

        if (encounters.Count == 0 && groups.Count == 0)
        {
            Console.Error.WriteLine("No valid tactical files to bundle.");
            return 1;
        }

        outDir = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outDir);
        var bundle = new
        {
            index = new
            {
                encountersById,
                groupsById,
                encountersByCategory = encountersByCategory.ToDictionary(k => k.Key, v => (object)v.Value)
            },
            encounters,
            groups
        };
        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        var outPath = Path.Combine(outDir, "tactical.bundle.json");
        File.WriteAllText(outPath, json);
        Console.WriteLine($"Wrote {encounters.Count} encounter(s) + {groups.Count} group(s) to {outPath}");
        return 0;
    }

    static object EncounterToJson(TacticalEncounter enc, string id, string category) => new
    {
        id,
        category,
        title = enc.Title,
        body = enc.Body,
        variant = enc.Variant.ToString().ToLowerInvariant(),
        stat = enc.Stat,
        tier = enc.Tier,
        requires = enc.Requires,
        resistance = enc.Resistance,
        timerDraw = enc.TimerDraw,
        timers = enc.Timers.Select(t => new
        {
            name = t.Name,
            counterName = t.CounterName,
            effect = t.Effect.ToString().ToLowerInvariant(),
            amount = t.Amount,
            countdown = t.Countdown,
            conditionId = t.ConditionId
        }),
        openings = enc.Openings.Select(o => new
        {
            name = o.Name,
            archetype = o.Archetype,
            requires = o.Requires
        }),
        path = enc.Path.Select(o => new
        {
            name = o.Name,
            archetype = o.Archetype,
            requires = o.Requires
        }),
        approaches = enc.Approaches.Select(a => new
        {
            kind = a.Kind.ToString().ToLowerInvariant(),
            momentum = a.Momentum,
            timerCount = a.TimerCount,
            bonusOpenings = a.BonusOpenings
        }),
        failure = enc.Failure is { } f ? new
        {
            text = f.Text,
            mechanics = f.Mechanics
        } : null,
        success = enc.Success is { } su ? new
        {
            text = su.Text,
            mechanics = su.Mechanics
        } : null
    };

    static object GroupToJson(TacticalGroup grp, string id, string category) => new
    {
        id,
        category,
        title = grp.Title,
        body = grp.Body,
        tier = grp.Tier,
        requires = grp.Requires,
        branches = grp.Branches.Select(b => new
        {
            label = b.Label,
            encounterRef = b.EncounterRef,
            requires = b.Requires
        })
    };
}
