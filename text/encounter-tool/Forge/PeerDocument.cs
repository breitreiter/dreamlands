// The peer JSON document — one sidecar (X.enc.json) per .enc, the single spine of
// the pipeline. Port of forge/peer.py's schema (v1).
//
// The .enc is read-only input and is never modified. Its peer accumulates, per
// beat, the original FIXME stub plus every phase's output. A final `integrate` pass
// splices approved finals back into a fresh .enc under out/.
//
// Phases read the stage keys they need and write only their own — a missing
// upstream key is a legible "run that phase first," never a silent skip. `Stages`
// is held as an opaque JsonObject here so this spine stays agnostic to each phase's
// payload shape (color, synthesis, synthesis_sofar, critic, ...).
namespace Forge;

using System.Text.Json.Nodes;

public sealed class PeerDocument
{
    public int Schema { get; set; } = Peer.Schema;
    public string Source { get; set; } = "";
    public string SourceSha { get; set; } = "";
    public string? Speaker { get; set; }
    public List<PeerBeat> Beats { get; set; } = [];
}

public sealed class PeerBeat
{
    public int Line { get; set; }
    public string Indent { get; set; } = "";
    public string? Tone { get; set; }
    public string? Choice { get; set; }
    public string Original { get; set; } = "";
    public JsonObject Stages { get; set; } = [];
    public string? Final { get; set; }
    public bool Approved { get; set; }
}
