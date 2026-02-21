namespace EncounterCli;

static class FixQuotesCommand
{
    private static readonly (char From, char To)[] Replacements =
    [
        ('\u201C', '"'),  // left double quote
        ('\u201D', '"'),  // right double quote
        ('\u2018', '\''), // left single quote
        ('\u2019', '\''), // right single quote / apostrophe
    ];

    public static int Run(string[] args)
    {
        var path = "encounters";
        var exts = new[] { ".enc" };
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--ext" && i + 1 < args.Length)
            {
                exts = args[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (exts.Length == 0) exts = new[] { ".enc" };
                i++;
            }
            else if (!args[i].StartsWith('-')) path = args[i];
        }

        path = Path.GetFullPath(path);
        if (!Directory.Exists(path))
        {
            Console.Error.WriteLine($"Path not found: {path}");
            return 1;
        }

        var files = exts.SelectMany(ext => Directory.GetFiles(path, "*" + ext, SearchOption.AllDirectories))
            .Distinct().OrderBy(f => f).ToArray();

        var fixedCount = 0;
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            var original = text;

            foreach (var (from, to) in Replacements)
                text = text.Replace(from, to);

            if (text != original)
            {
                File.WriteAllText(file, text);
                var rel = Path.GetRelativePath(path, file);
                Console.WriteLine($"  Fixed {rel}");
                fixedCount++;
            }
        }

        Console.WriteLine(fixedCount == 0
            ? "No curly quotes found."
            : $"\nFixed {fixedCount} file(s).");
        return 0;
    }
}
