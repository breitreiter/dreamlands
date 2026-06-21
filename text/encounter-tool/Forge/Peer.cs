// Port of forge/peer.py — build / merge / load / save the peer JSON sidecar.
//
// The .enc is never modified; the peer (X.enc.json) is the work. parse builds the
// beat skeleton from the (unchanged) source while preserving any stage outputs and
// finals already accumulated; integrate is the only writer of .enc and writes only
// to out/.
namespace Forge;

using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

public static class Peer
{
    public const int Schema = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        // ensure_ascii=False parity: keep smart quotes etc. as literal UTF-8.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// sha256 hex of the .enc text at parse time — integrate's drift guard.
    public static string Sha(string text)
        => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    /// X.enc -> X.enc.json (sibling sidecar).
    public static string PeerPath(string encPath) => encPath + ".json";

    public static PeerDocument BuildSkeleton(string encPath)
    {
        var content = File.ReadAllText(encPath);
        return new PeerDocument
        {
            Schema = Schema,
            Source = Path.GetFileName(encPath),
            SourceSha = Sha(content),
            Speaker = null,
            Beats = [.. Beats.Parse(content).Select(b => new PeerBeat
            {
                Line = b.Line,
                Indent = b.Indent,
                Tone = b.Tone,
                Choice = b.Choice,
                Original = b.Original,
                Stages = [],
                Final = null,
                Approved = false,
            })],
        };
    }

    /// Carry accumulated work forward onto a freshly-built skeleton. Beats are
    /// matched by source line; a beat whose `original` text still matches keeps its
    /// stages/final/approved. A changed line resets to the fresh stub.
    public static PeerDocument Merge(PeerDocument old, PeerDocument fresh)
    {
        var byLine = old.Beats.ToDictionary(b => b.Line);
        foreach (var b in fresh.Beats)
        {
            if (byLine.TryGetValue(b.Line, out var prev) && prev.Original == b.Original)
            {
                b.Stages = prev.Stages;
                b.Final = prev.Final;
                b.Approved = prev.Approved;
            }
        }
        fresh.Speaker = old.Speaker ?? fresh.Speaker;
        return fresh;
    }

    public static PeerDocument Load(string path)
        => JsonSerializer.Deserialize<PeerDocument>(File.ReadAllText(path), JsonOptions)
           ?? throw new InvalidDataException($"{path}: not a peer JSON document");

    public static void Save(string path, PeerDocument doc)
        => File.WriteAllText(path, JsonSerializer.Serialize(doc, JsonOptions) + "\n");
}
