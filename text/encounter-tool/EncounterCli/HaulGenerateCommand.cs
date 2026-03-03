using System.Text;

namespace EncounterCli;

static class HaulGenerateCommand
{
    const string HaulDir = "/home/joseph/repos/dreamlands/text/hauls";
    const string HaulGenerationDir = HaulDir + "/generation";
    const string BiomeBriefsDir = "/home/joseph/repos/dreamlands/project/reference/biome-briefs";
    const string DefaultCatalogPath = HaulDir + "/haul_catalog.md";
    const string ExamplesPath = HaulDir + "/haul_fixme.md";
    const string SystemPrompt = "You are a narrative designer for a computer RPG. Follow instructions precisely. Output only the requested content.";

    record HaulEntry(string Name, string Origin, string Destination, string FlavorStory, string DeliveryFlavor, int HeaderLine);

    public static async Task<int> RunAsync(string[] args)
    {
        string? configPath = null;
        string catalogPath = DefaultCatalogPath;
        var promptsOnly = false;

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--config" && i + 1 < args.Length) { configPath = args[i + 1]; i++; }
            else if (args[i] == "--catalog" && i + 1 < args.Length) { catalogPath = args[i + 1]; i++; }
            else if (args[i] == "--prompts-only") promptsOnly = true;
        }

        // Validate files
        var promptPath = Path.Combine(HaulGenerationDir, "haul_prompt.md");
        var originVibesPath = Path.Combine(HaulGenerationDir, "origin_vibes.txt");
        var deliveryVibesPath = Path.Combine(HaulGenerationDir, "delivery_vibes.txt");

        foreach (var (name, path) in new[]
        {
            ("haul_prompt.md", promptPath),
            ("origin_vibes.txt", originVibesPath),
            ("delivery_vibes.txt", deliveryVibesPath),
            ("catalog", catalogPath),
            ("examples", ExamplesPath),
        })
        {
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"File not found: {name} (expected at {path})");
                return 1;
            }
        }

        if (!Directory.Exists(BiomeBriefsDir))
        {
            Console.Error.WriteLine($"Biome briefs directory not found: {BiomeBriefsDir}");
            return 1;
        }

        // Load resources
        var promptTemplate = File.ReadAllText(promptPath);
        var originVibes = File.ReadAllLines(originVibesPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        var deliveryVibes = File.ReadAllLines(deliveryVibesPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

        if (originVibes.Length == 0 || deliveryVibes.Length == 0)
        {
            Console.Error.WriteLine("One or more vibe files are empty.");
            return 1;
        }

        var examples = ParseExamples(File.ReadAllLines(ExamplesPath));
        if (examples.Count == 0)
        {
            Console.Error.WriteLine("No examples found in haul_fixme.md.");
            return 1;
        }

        var catalogLines = File.ReadAllLines(catalogPath);
        var entries = ParseCatalog(catalogLines);
        var blanks = entries.Where(e => string.IsNullOrWhiteSpace(e.FlavorStory)).ToList();

        Console.WriteLine($"Catalog: {entries.Count} entries, {blanks.Count} blank.");
        if (blanks.Count == 0)
        {
            Console.WriteLine("Nothing to generate.");
            return 0;
        }

        // Load biome briefs
        var biomeBriefs = new Dictionary<string, string>();
        foreach (var file in Directory.GetFiles(BiomeBriefsDir, "*.md"))
        {
            var biome = Path.GetFileNameWithoutExtension(file);
            biomeBriefs[biome] = File.ReadAllText(file);
        }

        var random = new Random();
        LlmClient? client = null;

        foreach (var blank in blanks)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {blank.Name} ---");
            Console.WriteLine($"    {blank.Origin} → {blank.Destination}");

            if (!biomeBriefs.TryGetValue(blank.Origin, out var originBrief))
            {
                Console.Error.WriteLine($"No biome brief for '{blank.Origin}'. Skipping.");
                continue;
            }
            if (!biomeBriefs.TryGetValue(blank.Destination, out var destBrief))
            {
                Console.Error.WriteLine($"No biome brief for '{blank.Destination}'. Skipping.");
                continue;
            }

            while (true)
            {
                var originVibe = originVibes[random.Next(originVibes.Length)];
                var deliveryVibe = deliveryVibes[random.Next(deliveryVibes.Length)];

                Console.WriteLine();
                Console.WriteLine($"  Origin vibe:   {originVibe}");
                Console.WriteLine($"  Delivery vibe: {deliveryVibe}");
                Console.Write("\nProceed? [Y/n/r] ");

                var key = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
                if (key == "n")
                    return 0;
                if (key == "r")
                    continue;

                // Assemble prompt
                var selectedExamples = SelectExamples(examples, random, 3, 4);
                var examplesText = FormatExamples(selectedExamples);

                var prompt = promptTemplate
                    .Replace("{{ORIGIN_VIBE}}", originVibe)
                    .Replace("{{DELIVERY_VIBE}}", deliveryVibe)
                    .Replace("{{ITEM_NAME}}", blank.Name)
                    .Replace("{{ORIGIN_BIOME}}", blank.Origin)
                    .Replace("{{DESTINATION_BIOME}}", blank.Destination)
                    .Replace("{{ORIGIN_BRIEF}}", originBrief)
                    .Replace("{{DESTINATION_BRIEF}}", destBrief)
                    .Replace("{{EXAMPLES}}", examplesText);

                if (promptsOnly)
                {
                    Console.WriteLine();
                    Console.WriteLine("--- Assembled Prompt ---");
                    Console.WriteLine(prompt);
                    return 0;
                }

                client ??= LlmClient.TryCreate(configPath);
                if (client == null)
                    return 1;

                Console.WriteLine();
                Console.WriteLine("Generating...");

                string? response;
                try
                {
                    response = await client.CompleteAsync(prompt, SystemPrompt);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error: {ex.Message}");
                    return 1;
                }

                if (string.IsNullOrWhiteSpace(response))
                {
                    Console.Error.WriteLine("No response from LLM.");
                    return 1;
                }

                response = StripCodeFences(response);
                var (flavorStory, deliveryFlavor) = ParseResponse(response);

                if (string.IsNullOrWhiteSpace(flavorStory) || string.IsNullOrWhiteSpace(deliveryFlavor))
                {
                    Console.WriteLine();
                    Console.WriteLine("Could not parse response. Raw output:");
                    Console.WriteLine(response);
                    Console.Write("\n[r]etry / [n] quit? ");
                    var retry = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
                    if (retry == "n") return 0;
                    continue;
                }

                Console.WriteLine();
                Console.WriteLine($"  Flavor story:   {flavorStory}");
                Console.WriteLine($"  Delivery flavor: {deliveryFlavor}");
                Console.Write("\nAccept? [Y/n/r/e] ");

                var accept = Console.ReadLine()?.Trim().ToLowerInvariant() ?? "";
                if (accept == "n")
                    return 0;
                if (accept == "r")
                    continue;

                if (accept == "e")
                {
                    Console.Write("Flavor story (enter to keep): ");
                    var editFs = Console.ReadLine()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(editFs)) flavorStory = editFs;

                    Console.Write("Delivery flavor (enter to keep): ");
                    var editDf = Console.ReadLine()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(editDf)) deliveryFlavor = editDf;
                }

                // Write back to catalog
                catalogLines = File.ReadAllLines(catalogPath);
                WriteToCatalog(catalogLines, blank.HeaderLine, blank.Name, flavorStory, deliveryFlavor);
                File.WriteAllLines(catalogPath, catalogLines);
                Console.WriteLine($"  Written to catalog.");
                break;
            }
        }

        Console.WriteLine();
        Console.WriteLine("Done.");
        return 0;
    }

    static List<HaulEntry> ParseCatalog(string[] lines)
    {
        var entries = new List<HaulEntry>();
        string? name = null, origin = null, dest = null, flavorStory = null, deliveryFlavor = null;
        int headerLine = -1;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith("### "))
            {
                if (name != null)
                    entries.Add(new HaulEntry(name, origin ?? "", dest ?? "", flavorStory ?? "", deliveryFlavor ?? "", headerLine));

                name = line[4..].Trim();
                headerLine = i;
                origin = dest = flavorStory = deliveryFlavor = null;
            }
            else if (line.StartsWith("- **Origin biome**: "))
                origin = line["- **Origin biome**: ".Length..].Trim();
            else if (line.StartsWith("- **Destination biome**: "))
                dest = line["- **Destination biome**: ".Length..].Trim();
            else if (line.StartsWith("- **Flavor story**: "))
                flavorStory = line["- **Flavor story**: ".Length..].Trim();
            else if (line.StartsWith("- **Flavor story**:"))
                flavorStory = line["- **Flavor story**:".Length..].Trim();
            else if (line.StartsWith("- **Delivery flavor**: "))
                deliveryFlavor = line["- **Delivery flavor**: ".Length..].Trim();
            else if (line.StartsWith("- **Delivery flavor**:"))
                deliveryFlavor = line["- **Delivery flavor**:".Length..].Trim();
        }

        if (name != null)
            entries.Add(new HaulEntry(name, origin ?? "", dest ?? "", flavorStory ?? "", deliveryFlavor ?? "", headerLine));

        return entries;
    }

    static List<HaulEntry> ParseExamples(string[] lines)
    {
        // Same format as catalog — just parse all entries that have non-empty flavor
        var all = ParseCatalog(lines);
        return all.Where(e => !string.IsNullOrWhiteSpace(e.FlavorStory) && !string.IsNullOrWhiteSpace(e.DeliveryFlavor)).ToList();
    }

    static void WriteToCatalog(string[] lines, int headerLine, string name, string flavorStory, string deliveryFlavor)
    {
        // Find the flavor story and delivery flavor lines after the header
        for (int i = headerLine + 1; i < lines.Length && i < headerLine + 10; i++)
        {
            if (lines[i].StartsWith("- **Flavor story**"))
                lines[i] = $"- **Flavor story**: {flavorStory}";
            else if (lines[i].StartsWith("- **Delivery flavor**"))
                lines[i] = $"- **Delivery flavor**: {deliveryFlavor}";
        }
    }

    static List<HaulEntry> SelectExamples(List<HaulEntry> pool, Random random, int min, int max)
    {
        var count = random.Next(min, max + 1);
        count = Math.Min(count, pool.Count);
        return pool.OrderBy(_ => random.Next()).Take(count).ToList();
    }

    static string FormatExamples(List<HaulEntry> examples)
    {
        var sb = new StringBuilder();
        foreach (var ex in examples)
        {
            sb.AppendLine($"### {ex.Name}");
            sb.AppendLine($"- Origin: {ex.Origin} → Destination: {ex.Destination}");
            sb.AppendLine($"- Flavor story: {ex.FlavorStory}");
            sb.AppendLine($"- Delivery flavor: {ex.DeliveryFlavor}");
            sb.AppendLine();
        }
        return sb.ToString().TrimEnd();
    }

    static (string flavorStory, string deliveryFlavor) ParseResponse(string text)
    {
        string flavorStory = "", deliveryFlavor = "";

        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("Flavor story:", StringComparison.OrdinalIgnoreCase))
                flavorStory = trimmed["Flavor story:".Length..].Trim();
            else if (trimmed.StartsWith("Delivery flavor:", StringComparison.OrdinalIgnoreCase))
                deliveryFlavor = trimmed["Delivery flavor:".Length..].Trim();
        }

        return (flavorStory, deliveryFlavor);
    }

    static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```"))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline > 0)
                trimmed = trimmed[(firstNewline + 1)..];
        }
        if (trimmed.EndsWith("```"))
            trimmed = trimmed[..^3].TrimEnd();
        return trimmed;
    }
}
