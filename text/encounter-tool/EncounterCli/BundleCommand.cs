using System.Text.Json;
using Dreamlands.Encounter;

namespace EncounterCli;

static class BundleCommand
{
    public static int Run(string[] args)
    {
        string path = "";
        var outDir = ".";
        var exts = new[] { ".enc" };
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length) { outDir = args[i + 1]; i++; }
            else if (args[i] == "--ext" && i + 1 < args.Length)
            {
                exts = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (exts.Length == 0) exts = new[] { ".enc" };
                i++;
            }
            else if (!args[i].StartsWith('-')) path = args[i];
        }

        if (string.IsNullOrEmpty(path))
        {
            Console.Error.WriteLine("bundle requires <path> to the encounters directory.");
            return 1;
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Path not found: {path}");
            return 1;
        }

        var files = exts.SelectMany(ext => Directory.GetFiles(path, "*" + ext, SearchOption.AllDirectories))
            .Distinct().OrderBy(f => f).ToArray();
        if (files.Length == 0)
        {
            Console.Error.WriteLine($"No encounter files ({string.Join(", ", exts)}) found under {path}");
            return 1;
        }

        var encounters = new List<object>();
        var byId = new Dictionary<string, object>();
        var byCategory = new Dictionary<string, List<string>>();
        var errors = 0;

        foreach (var file in files)
        {
            var rel = Path.GetRelativePath(path, file);
            var category = Path.GetDirectoryName(rel) ?? "";
            var id = Path.GetFileNameWithoutExtension(file);
            var text = File.ReadAllText(file);
            var result = EncounterParser.Parse(text);
            if (!result.IsSuccess)
            {
                Console.Error.WriteLine($"Skip {rel}: parse errors");
                foreach (var err in result.Errors)
                    Console.Error.WriteLine($"  {err}");
                errors++;
                continue;
            }

            var enc = result.Encounter!;
            var doc = EncounterToJson(enc, id, category);
            encounters.Add(doc);
            byId[id] = new { category, encounterIndex = encounters.Count - 1 };
            if (!byCategory.TryGetValue(category, out var list))
            {
                list = new List<string>();
                byCategory[category] = list;
            }
            list.Add(id);
        }

        if (errors > 0)
            Console.Error.WriteLine($"{errors} file(s) skipped due to errors.");

        if (encounters.Count == 0)
        {
            Console.Error.WriteLine("No valid encounters to bundle.");
            return 1;
        }

        outDir = Path.GetFullPath(outDir);
        Directory.CreateDirectory(outDir);
        var bundle = new
        {
            index = new
            {
                byId,
                byCategory = byCategory.ToDictionary(k => k.Key, v => (object)v.Value)
            },
            encounters
        };
        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { WriteIndented = true });
        var outPath = Path.Combine(outDir, "encounters.bundle.json");
        File.WriteAllText(outPath, json);
        Console.WriteLine($"Wrote {encounters.Count} encounters to {outPath}");
        return 0;
    }

    static object EncounterToJson(Encounter enc, string id, string category)
    {
        var choices = new List<object>();
        foreach (var c in enc.Choices)
        {
            if (c.Conditional != null)
            {
                var branches = c.Conditional.Branches.Select(b => new
                {
                    condition = b.Condition,
                    text = b.Outcome.Text,
                    mechanics = b.Outcome.Mechanics
                }).ToList();

                object? fallback = c.Conditional.Fallback is { } fb
                    ? new { text = fb.Text, mechanics = fb.Mechanics }
                    : null;

                choices.Add(new
                {
                    optionText = c.OptionText,
                    optionLink = c.OptionLink,
                    optionPreview = c.OptionPreview,
                    requires = c.Requires,
                    conditional = new
                    {
                        preamble = c.Conditional.Preamble,
                        branches,
                        fallback
                    }
                });
            }
            else
            {
                choices.Add(new
                {
                    optionText = c.OptionText,
                    optionLink = c.OptionLink,
                    optionPreview = c.OptionPreview,
                    requires = c.Requires,
                    single = new
                    {
                        text = c.Single!.Part.Text,
                        mechanics = c.Single.Part.Mechanics
                    }
                });
            }
        }
        return new
        {
            id,
            category,
            title = enc.Title,
            body = enc.Body,
            choices
        };
    }
}
