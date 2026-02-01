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
            "fixme" => FixmeCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "generate" => GenerateCommand.RunAsync(rest).GetAwaiter().GetResult(),
            _ => PrintUsage()
        };
    }

    static int PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  encounter check [<path>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter bundle <path> [--out <dir>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter fixme <file.enc> [--config <path>] [--prompts-only]");
        Console.WriteLine("  encounter generate [--template <file>] [--out <file>] [--config <path>] [--prompts-only]");
        Console.WriteLine();
        Console.WriteLine("generate looks for locale_guide.txt in the current directory and oracle");
        Console.WriteLine("fragments in /home/joseph/repos/dreamlands/text/encounters/generation/.");
        return 1;
    }
}
