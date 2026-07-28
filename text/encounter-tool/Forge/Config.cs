// Forge configuration — loaded from a gitignored appsettings.json next to the binary
// (or --config <path>). Absent file => sensible defaults (the router on imp:8086, the
// loom on joseph@imp), so Forge runs on the imp network with no config file: the only
// secret, the minrouter token, comes from the $MINROUTER_KEY environment variable.
namespace Forge;

using System.Text.Json;

public sealed class ForgeConfig
{
    public RouterConfig Router { get; set; } = new();
    public ImpConfig Imp { get; set; } = new();

    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static ForgeConfig Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new ForgeConfig();
        return JsonSerializer.Deserialize<ForgeConfig>(File.ReadAllText(path), Opts) ?? new ForgeConfig();
    }
}

/// minrouter on imp — the one gateway for every LLM Forge calls. It fronts the local
/// imp GLM profiles (auto-loaded on demand) and the paid Cloudflare workers-ai models
/// behind a single bearer token, holding each provider's own credentials.
public sealed class RouterConfig
{
    /// Base URL of the router. Provider paths hang off /x/<upstream>/… beneath it.
    public string BaseUrl { get; set; } = "http://imp:8086";

    /// Environment variable holding the mr_ bearer token. Read from the environment so
    /// the secret never lands in a config file.
    public string TokenEnv { get; set; } = "MINROUTER_KEY";

    /// Optional gitignored override if the token can't come from the environment.
    /// Leave empty to use $MINROUTER_KEY. Never logged; never a path outside the repo.
    public string ApiKey { get; set; } = "";

    /// Upstream + model for the free tone-tagging pass (categorize). The router
    /// auto-loads the glmchat profile on this upstream; "auto" resolves the loaded id.
    public string CategorizeUpstream { get; set; } = "imp-glmchat";
    public string CategorizeModel { get; set; } = "auto";

    /// Upstream + model for the paid reasoning integrator (synthesis / weave). Swap the
    /// model per run with --model; the cf upstream serves Cloudflare's workers-ai ids.
    public string SynthesisUpstream { get; set; } = "cf";
    public string IntegratorModel { get; set; } = "workers-ai/@cf/moonshotai/kimi-k2.6";
}

public sealed class ImpConfig
{
    /// The opaque loom color service on imp. We ship a beats-job to LoomInbox, run
    /// Grind, and pull the enriched beats from LoomOutbox. The steering internals are
    /// undocumented here by design — magic shell stuff, beats in, enriched beats out.
    /// Not fronted by the router: it is a torch+LoRA steering script, not an OpenAI
    /// endpoint, so the color stage still drives it over ssh.
    public string SshTarget { get; set; } = "joseph@imp";
    public string LoomInbox { get; set; } = "~/loom-io/inbox";
    public string LoomOutbox { get; set; } = "~/loom-io/outbox";
    public string Grind { get; set; } = "~/repos/loom/bin/grind";
}
