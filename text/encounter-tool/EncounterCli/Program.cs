namespace EncounterCli;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0)
            return PrintUsage();

        var command = args[0].ToLowerInvariant();
        var rest = args.Skip(1).ToArray();

        return command switch
        {
            "check" => CheckCommand.Run(rest),
            "bundle" => BundleCommand.Run(rest),
            "check-tactical" => TacticalCheckCommand.Run(rest),
            "bundle-tactical" => TacticalBundleCommand.Run(rest),
            "fixme" => FixmeCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "generate" => GenerateCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "haul-generate" => HaulGenerateCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "push" => PushCommand.Run(rest).GetAwaiter().GetResult(),
            "walk" => WalkCommand.Run(rest),
            "generate-tactical" => GenerateTacticalCommand.Run(rest),
            _ => PrintUsage()
        };
    }

    static int PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  encounter check [<path>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter bundle <path> [--out <dir>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter check-tactical [<path>]");
        Console.WriteLine("  encounter bundle-tactical <path> [--out <dir>]");
        Console.WriteLine("  encounter fixme <file.enc> [--config <path>] [--prompts-only]");
        Console.WriteLine("  encounter generate [--out <file>] [--config <path>] [--prompts-only]");
        Console.WriteLine("  encounter haul-generate [--config <path>] [--catalog <path>] [--prompts-only]");
        Console.WriteLine("  encounter push [<path>] [--world <name>]");
        Console.WriteLine("  encounter walk <arc-dir> [--skill combat=5] [--tag foo] [--item torch] [--quality guild=3] [--gold 50]");
        Console.WriteLine("  encounter generate-tactical <variant> <stat> <tier> [--out <file>] [--seed <n>] [--intent <name>]");
        Console.WriteLine();
        Console.WriteLine("generate looks for locale_guide.txt in the current directory and archetype");
        Console.WriteLine("pools in text/encounters/generation/v2/.");
        return 1;
    }
}
