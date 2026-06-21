// The color provider we run: the gemma proxy-steer triplet on imp (loom), treated
// as an opaque service. Port of forge/color.py's ship→grind→pull orchestration.
//
// We ship a beats-job, run the "magic shell stuff" (grind), and pull back enriched
// beats. The steering rig itself (torch + LoRA logit-steering) stays Python on the
// GPU box and is never ported — this class knows only the IO contract. Free but
// SLOW: a full arc is an overnight run (the triplet owns the box).
namespace Forge;

using System.Diagnostics;
using System.Text.Json;

public sealed class ImpLoomColorProvider(ImpConfig cfg) : IColorProvider
{
    public Task<IReadOnlyList<ColorRecord>> EnrichAsync(string arc, IReadOnlyList<BeatJob> jobs)
    {
        var jobPath = Path.Combine(Path.GetTempPath(), $"{arc}.beats.json");
        File.WriteAllText(jobPath, JsonSerializer.Serialize(jobs, ColorJson.Opts));

        var inbox = $"{cfg.LoomInbox}/{arc}.beats.json";
        var remoteOut = $"{cfg.LoomOutbox}/{arc}.color.json";
        var localOut = Path.Combine(Path.GetTempPath(), $"{arc}.color.json");

        // Clear stale jobs so grind colours only this arc, then ship → grind → pull.
        Sh("ssh", [cfg.SshTarget, $"mkdir -p {cfg.LoomInbox} {cfg.LoomOutbox} && rm -f {cfg.LoomInbox}/*.beats.json"]);
        Sh("scp", ["-q", jobPath, $"{cfg.SshTarget}:{inbox}"]);
        Console.WriteLine("running grind on imp (the slow part — gemma triplet owns the box)...");
        Sh("ssh", [cfg.SshTarget, cfg.Grind], stream: true);
        Sh("scp", ["-q", $"{cfg.SshTarget}:{remoteOut}", localOut]);

        var records = JsonSerializer.Deserialize<List<ColorRecord>>(File.ReadAllText(localOut), ColorJson.Opts)
                      ?? [];
        return Task.FromResult<IReadOnlyList<ColorRecord>>(records);
    }

    private static void Sh(string file, string[] args, bool stream = false)
    {
        var psi = new ProcessStartInfo(file)
        {
            RedirectStandardOutput = !stream,
            RedirectStandardError = !stream,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);
        using var p = Process.Start(psi) ?? throw new InvalidOperationException($"failed to start {file}");
        p.WaitForExit();
        if (p.ExitCode != 0)
            throw new InvalidOperationException($"{file} {string.Join(' ', args)} exited {p.ExitCode}");
    }
}
