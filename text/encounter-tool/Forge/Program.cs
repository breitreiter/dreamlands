using Forge;

if (args.Length == 0) return Usage();

try
{
    return args[0] switch
    {
        "parse" => ParseCommand.Run(args[1..]),
        "integrate" => IntegrateCommand.Run(args[1..]),
        "categorize" => await CategorizeCommand.RunAsync(args[1..]),
        "color" => await ColorCommand.RunAsync(args[1..]),
        "synthesis" => await SynthesisCommand.RunAsync(args[1..]),
        "weave" => await WeaveCommand.RunAsync(args[1..]),
        "review" => ReviewCommand.Run(args[1..]),
        "thread" => ThreadCommand.Run(args[1..]),
        "suggest-threads" => ThreadSuggestCommand.Run(args[1..]),
        "-h" or "--help" or "help" => Usage(),
        var cmd => Unknown(cmd),
    };
}
// Anything an author can fix by editing _threads.json or a choice label. One message,
// no stack trace — these are content errors, not crashes.
catch (ThreadPlanException e)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

static int Usage()
{
    Console.WriteLine("""
        forge — enrich .enc encounter files via the peer-JSON pipeline.

        Spine (free, local):
          parse <file.enc> [...]              .enc -> X.enc.json beat skeleton (idempotent)
          integrate <file.enc.json> [...]     peer JSON -> out/<arc>/<name>.enc
            [--out <dir>]                     output root (default: ./out)

        Stages:
          categorize <file.enc.json> [...]    tag each beat with one of six tones (GLM via router)
            [--force] [--dry-run] [--config <path>]
          color <file.enc.json> [...]         enrich each beat into a color bank (imp loom; free, SLOW)
            [--force] [--limit N] [--dry-run] [--provider imp|glm-hi-temp] [--config <path>]
          synthesis <file.enc.json> [...]     beat + color -> finished prose (PAID, via router)
            [--model ID] [--limit N] [--beats id,id] [--dry-run] [--no-lens] [--force] [--config <path>]
          weave <arc-dir>                     threaded story-so-far synthesis along one path (PAID)
            [--thread name] [--model ID] [--dry-run] [--force] [--config <path>]
          review <dir|file.enc.json>          pre-weave gate: stub+tone+color -> markdown (free, local)
            [--thread name] [--out <dir>]       --thread renders one path in reading order
          thread <arc-dir> [--thread name]    debug: print a thread's beat spine
            [--coverage]                        report beats no thread reaches (exit 1 if any)

        Path args may be files or directories (dirs expand to their *.enc / *.enc.json).
        Threads are per-arc content: `_threads.json` beside the arc's bibles.
        """);
    return 0;
}

static int Unknown(string cmd)
{
    Console.Error.WriteLine($"forge: unknown command '{cmd}' (try: forge help)");
    return 2;
}
