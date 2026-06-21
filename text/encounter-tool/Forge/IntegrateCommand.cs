// Phase: peer JSON -> out/<arc>/<name>.enc, splicing approved finals.
// Port of forge/integrate.py.
//
// For each beat with approved==true, the FIXME line is replaced by its `final`
// prose (same indentation, FIXME prefix dropped). Unapproved beats keep their FIXME
// stub. With nothing approved, the output is byte-identical to the source .enc — the
// round-trip identity that proves the spine is lossless.
//
// The source .enc is the sibling of the peer JSON and is verified against
// source_sha; a drifted source aborts rather than splicing against stale line
// numbers.
namespace Forge;

public static class IntegrateCommand
{
    public static int Run(string[] args)
    {
        var outRoot = "out";
        var rest = new List<string>();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--out" && i + 1 < args.Length)
                outRoot = args[++i];
            else
                rest.Add(args[i]);
        }

        var paths = Cli.ExpandArgs(rest, ".enc.json");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("usage: forge integrate <file.enc.json> [...] [--out <dir>]   (dirs expand to *.enc.json)");
            return 2;
        }

        foreach (var pj in paths)
        {
            var rc = IntegrateOne(pj, outRoot);
            if (rc != 0) return rc;
        }
        return 0;
    }

    private static int IntegrateOne(string peerJson, string outRoot)
    {
        var doc = Peer.Load(peerJson);
        var parent = Path.GetDirectoryName(Path.GetFullPath(peerJson))!;
        var encPath = Path.Combine(parent, doc.Source);
        var content = File.ReadAllText(encPath);

        if (Peer.Sha(content) != doc.SourceSha)
        {
            Console.Error.WriteLine($"  ! {doc.Source}: source .enc has changed since parse — re-run forge parse first");
            return 1;
        }

        var lines = Beats.SplitLines(content);
        var spliced = 0;
        foreach (var b in doc.Beats)
        {
            if (b.Approved && !string.IsNullOrEmpty(b.Final))
            {
                lines[b.Line] = b.Indent + b.Final.Trim();
                spliced++;
            }
        }

        var arc = new DirectoryInfo(parent).Name;
        var outDir = Path.Combine(outRoot, arc);
        Directory.CreateDirectory(outDir);
        var outPath = Path.Combine(outDir, doc.Source);
        File.WriteAllText(outPath, string.Join('\n', lines) + "\n");

        Console.WriteLine($"  {doc.Source}: {spliced}/{doc.Beats.Count} beats spliced -> {outPath}");
        return 0;
    }
}
