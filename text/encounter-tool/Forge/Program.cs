using Forge;

if (args.Length == 0) return Usage();

return args[0] switch
{
    "parse" => ParseCommand.Run(args[1..]),
    "integrate" => IntegrateCommand.Run(args[1..]),
    "categorize" => await CategorizeCommand.RunAsync(args[1..]),
    "synthesis" => await SynthesisCommand.RunAsync(args[1..]),
    "-h" or "--help" or "help" => Usage(),
    var cmd => Unknown(cmd),
};

static int Usage()
{
    Console.WriteLine("""
        forge — enrich .enc encounter files via the peer-JSON pipeline.

        Spine (free, local):
          parse <file.enc> [...]              .enc -> X.enc.json beat skeleton (idempotent)
          integrate <file.enc.json> [...]     peer JSON -> out/<arc>/<name>.enc
            [--out <dir>]                     output root (default: ./out)

        Stages:
          categorize <file.enc.json> [...]    tag each beat with one of six tones (GLM on imp)
            [--force] [--dry-run] [--config <path>]
          synthesis <file.enc.json> [...]     beat + color -> finished prose (PAID gateway)
            [--model ID] [--limit N] [--beats id,id] [--dry-run] [--no-lens] [--force] [--config <path>]

        Path args may be files or directories (dirs expand to their *.enc / *.enc.json).
        """);
    return 0;
}

static int Unknown(string cmd)
{
    Console.Error.WriteLine($"forge: unknown command '{cmd}' (try: forge help)");
    return 2;
}
