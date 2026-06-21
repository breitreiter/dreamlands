// Forge configuration — loaded from a gitignored appsettings.json next to the
// binary (or --config <path>). Absent file => sensible defaults (imp:8080), so the
// free local stages run with no config on the imp network. Paid-gateway + imp-loom
// sections arrive in later phases; see appsettings.example.json.
namespace Forge;

using System.Text.Json;

public sealed class ForgeConfig
{
    public GlmConfig Glm { get; set; } = new();
    public GatewayConfig Gateway { get; set; } = new();

    private static readonly JsonSerializerOptions Opts = new() { PropertyNameCaseInsensitive = true };

    public static ForgeConfig Load(string? path = null)
    {
        path ??= Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path)) return new ForgeConfig();
        return JsonSerializer.Deserialize<ForgeConfig>(File.ReadAllText(path), Opts) ?? new ForgeConfig();
    }
}

public sealed class GlmConfig
{
    /// OpenAI-compatible base (…/v1) for the free GLM reasoning model on imp.
    public string Endpoint { get; set; } = "http://imp:8080/v1";

    /// "auto" = query /models and use the first id; or pin an explicit model id.
    public string Model { get; set; } = "auto";
}

public sealed class GatewayConfig
{
    /// The Cloudflare AI Gateway OpenAI-compatible chat-completions URL. PAID.
    /// Lives only in the gitignored appsettings.json — never a path outside the repo.
    public string Endpoint { get; set; } = "";

    /// Bearer token for the gateway. Gitignored config only; never logged.
    public string ApiKey { get; set; } = "";

    /// Default integrator (gateway model id). Swap per run with --model.
    public string IntegratorModel { get; set; } = "workers-ai/@cf/moonshotai/kimi-k2.6";
}
