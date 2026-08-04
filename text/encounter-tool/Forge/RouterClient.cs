// One client for every LLM Forge uses, fronted by minrouter on imp:8086. It replaces
// the old fragile trio: GlmClient (direct imp:8080, needing a manual `swap-model`),
// GatewayClient (direct Cloudflare with a WAF User-Agent hack + embedded key), and
// their bespoke quota handling. Each call names an upstream (imp-glmchat, cf, …); the
// router auto-loads local profiles, holds every provider credential, and enforces
// per-provider daily caps — so Forge carries no provider key and no swap-model dance.
//
// Both upstreams Forge uses (imp profiles, Cloudflare's compat shim) return the
// OpenAI chat-completions shape, so one request/response path covers them. Reasoning
// models may emit a <think>…</think> preamble (or a separate reasoning_content field
// the router already strips from `content`); StripThink keeps only the answer.
namespace Forge;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

/// An upstream's daily cap is spent — the router answers 429 with `daily limit for
/// '<upstream>' reached (N/day)`. Retrying can't clear it until the cap resets, so
/// callers must abort the whole run, never back off into it. (Was Cloudflare's 4006
/// neuron cap; the router now fronts every provider behind one signal.)
public sealed class QuotaExhaustedException(string message) : Exception(message);

/// The model returned no answer — a think-only response with nothing after </think>,
/// or a soft refusal. Surfaced as a real failure so callers never silently store a
/// blank cell.
public sealed class EmptyCompletionException(string message) : Exception(message);

public sealed class RouterClient
{
    // Prompt eval on the local imp profiles is slow; keep a generous ceiling.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(900) };

    private readonly string _base;   // e.g. http://imp:8086
    private readonly string _token;  // mr_ bearer

    public RouterClient(RouterConfig cfg)
    {
        _base = cfg.BaseUrl.TrimEnd('/');
        // Prefer the environment (the token already lives there); fall back to a
        // gitignored config override. Never a path outside the repo, never logged.
        _token = !string.IsNullOrWhiteSpace(cfg.ApiKey)
            ? cfg.ApiKey
            : Environment.GetEnvironmentVariable(cfg.TokenEnv) ?? "";
        if (string.IsNullOrWhiteSpace(_token))
            throw new InvalidOperationException(
                $"minrouter token missing — set ${cfg.TokenEnv} in the environment "
                + "(or Router.ApiKey in a gitignored appsettings.json).");
    }

    // The provider path that follows /x/<upstream>. imp profiles speak the OpenAI /v1
    // dialect; Cloudflare's compat shim lives at /compat.
    private static string PathFor(string upstream) =>
        upstream == "cf" ? "compat/chat/completions" : "v1/chat/completions";

    /// Resolve "auto" to the upstream's first reported model id (imp profiles report
    /// the one loaded model). Any explicit id — including cf's `workers-ai/@cf/…`
    /// strings — passes straight through.
    public async Task<string> ModelIdAsync(string upstream, string model)
    {
        if (!string.IsNullOrEmpty(model) && model != "auto") return model;
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_base}/x/{upstream}/v1/models");
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_token}");
        using var resp = await Http.SendAsync(req);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString()
            ?? throw new InvalidOperationException($"{upstream} /models returned no id");
    }

    /// One chat completion through the router. Retries with exponential backoff on
    /// 429/5xx (honours numeric Retry-After); QuotaExhausted on a spent daily cap,
    /// which is not transient — the caller must stop.
    public async Task<string> ChatAsync(string upstream, string model,
        IReadOnlyList<ChatMessage> messages,
        double temperature = 0.8, int maxTokens = 8000, int retries = 6)
    {
        var url = $"{_base}/x/{upstream}/{PathFor(upstream)}";
        var json = JsonSerializer.Serialize(new { model, messages, temperature, max_tokens = maxTokens });
        var delay = 5;
        for (var attempt = 0; attempt < retries; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url);
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_token}");
            req.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message")
                    .GetProperty("content").GetString() ?? "";
            }

            var body = await resp.Content.ReadAsStringAsync();
            var code = (int)resp.StatusCode;
            // The router's own cap message: backing off just re-hits the wall until
            // the cap resets. Fail fast, let the caller stop the whole run.
            if (code == 429 && body.Contains("daily limit", StringComparison.OrdinalIgnoreCase))
                throw new QuotaExhaustedException(body.Trim().Length > 0 ? body.Trim()
                    : $"{upstream}: daily cap exhausted");

            // 504 belongs here with the rest of the gateway family: a long kimi
            // completion behind the router times out at the edge often enough that
            // dropping the beat on the first one just means re-running (and paying)
            // later. Observed on the 2026-08-04 smoke test.
            if (code is 429 or 500 or 502 or 503 or 504 && attempt < retries - 1)
            {
                var ra = resp.Headers.RetryAfter?.Delta?.TotalSeconds;
                await Task.Delay(TimeSpan.FromSeconds(ra is not null ? (int)ra.Value : delay));
                delay = Math.Min(delay * 2, 60);
                continue;
            }
            resp.EnsureSuccessStatusCode(); // other codes / final attempt: throw
        }
        throw new HttpRequestException($"{upstream}: retries exhausted");
    }

    /// ChatAsync + StripThink, retrying when the answer comes back empty (a think-only
    /// response or soft refusal). Raises EmptyCompletion if every attempt is blank — so
    /// a refusal surfaces as a failure, never a silently-stored "".
    public async Task<string> CompleteAsync(string upstream, string model,
        IReadOnlyList<ChatMessage> messages, int emptyRetries = 3)
    {
        for (var attempt = 0; attempt < emptyRetries; attempt++)
        {
            var text = StripThink(await ChatAsync(upstream, model, messages));
            if (text.Trim().Length > 0) return text;
            if (attempt < emptyRetries - 1) await Task.Delay(TimeSpan.FromSeconds(3));
        }
        throw new EmptyCompletionException($"{model} returned an empty answer after {emptyRetries} attempts");
    }

    public static string StripThink(string text)
    {
        const string close = "</think>";
        var i = text.IndexOf(close, StringComparison.Ordinal);
        return (i >= 0 ? text[(i + close.Length)..] : text).Trim();
    }
}
