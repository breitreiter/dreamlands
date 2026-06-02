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
            "colorize" => ColorizeCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "factual" => FactualCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "voice" => VoiceCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "critic" => CriticCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "generate" => GenerateCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "haul-generate" => HaulGenerateCommand.RunAsync(rest).GetAwaiter().GetResult(),
            "push" => PushCommand.Run(rest).GetAwaiter().GetResult(),
            "walk" => WalkCommand.Run(rest),
            _ => PrintUsage()
        };
    }

    static int PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  encounter check [<path>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter bundle <path> [--out <dir>] [--ext .enc,.txt]");
        Console.WriteLine("  encounter fixme <file.enc> [--config <path>] [--prompts-only]");
        Console.WriteLine("                  [--expand-url <url>] [--programs <dir>] [--author HPL|REH|CAS]");
        Console.WriteLine("                  [--scene mundane|action|horror|dread|wonder|revelation] [--candidates N]");
        Console.WriteLine("                  per-beat: FIXME(scene_type): e.g. FIXME(dread): or FIXME(horror):");
        Console.WriteLine("  encounter colorize <arc-dir> [--config <path>] [--force] [--prompts-only]");
        Console.WriteLine("                  [--no-filter] [--audit] [--only <scene>]...");
        Console.WriteLine("                  Generate a scene-level COLOR pool per .enc: GLM over-generates");
        Console.WriteLine("                  (steered by a <name>.lens.md / _lens.md), Haiku culls the slop.");
        Console.WriteLine("  encounter factual <arc-dir> [--qwen-url <url>] [--force] [--prompts-only]");
        Console.WriteLine("                  Integrate FIXME + curated COLOR bullets into FACTUAL prose blocks.");
        Console.WriteLine("  encounter voice <arc-dir> [--qwen-url <url>] [--authors HPL,REH] [--scene <name>]");
        Console.WriteLine("                  [--default-scene mundane] [--programs <dir>] [--temperature 0.7]");
        Console.WriteLine("                  [--force] [--prompts-only]");
        Console.WriteLine("                  Produce author-voiced variants of each FACTUAL block via voices/*.json.");
        Console.WriteLine("  encounter critic <arc-dir> [--config <path>] [--phase factual|voice|both]");
        Console.WriteLine("                  [--force] [--prompts-only]");
        Console.WriteLine("                  Cross-provider critique of FACTUAL and VOICED blocks via Anthropic SDK.");
        Console.WriteLine("  encounter generate [--out <file>] [--config <path>] [--prompts-only]");
        Console.WriteLine("  encounter haul-generate [--config <path>] [--catalog <path>] [--prompts-only]");
        Console.WriteLine("  encounter push [<path>] [--world <name>]");
        Console.WriteLine("  encounter walk <arc-dir> [--skill combat=5] [--tag foo] [--item torch] [--quality guild=3] [--gold 50]");
        Console.WriteLine();
        Console.WriteLine("generate looks for locale_guide.txt in the current directory and archetype");
        Console.WriteLine("pools in text/encounters/generation/v2/.");
        return 1;
    }
}
