// Phase: .enc -> peer JSON skeleton. Port of forge/parse.py. Free, local, idempotent.
//
// Writes X.enc.json next to each X.enc. Re-running refreshes the beat skeleton from
// the (unchanged) source while PRESERVING any stage outputs and finals already
// accumulated — safe to re-run at any point without losing work.
namespace Forge;

public static class ParseCommand
{
    public static int Run(string[] args)
    {
        var paths = Cli.ExpandArgs(args, ".enc");
        if (paths.Count == 0)
        {
            Console.Error.WriteLine("usage: forge parse <file.enc> [...]   (dirs expand to *.enc)");
            return 2;
        }

        foreach (var ep in paths)
        {
            var pp = Peer.PeerPath(ep);
            var fresh = Peer.BuildSkeleton(ep);

            PeerDocument doc;
            string note;
            if (File.Exists(pp))
            {
                doc = Peer.Merge(Peer.Load(pp), fresh);
                var kept = doc.Beats.Count(b => b.Stages.Count > 0 || b.Final is not null);
                note = kept > 0 ? $"refreshed ({kept} beat(s) with retained work)" : "refreshed";
            }
            else
            {
                doc = fresh;
                note = "new";
            }

            Peer.Save(pp, doc);
            Console.WriteLine($"  {Path.GetFileName(ep)}: {doc.Beats.Count} beats -> {Path.GetFileName(pp)}  [{note}]");
        }
        return 0;
    }
}
