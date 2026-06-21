// Cloudflare AI Gateway (paid) — port of the cloudflare_* helpers in
// forge/clients.py. The synthesis integrator runs through here. Both this and GLM
// are OpenAI-compatible and may emit a <think>…</think> preamble; StripThink
// (GlmClient) keeps only the answer.
namespace Forge;

using System.Text;
using System.Text.Json;

/// Cloudflare's daily free neuron allocation (internalCode 4006) is spent. No retry
/// clears this until the daily UTC reset, so callers must abort the whole run —
/// never back off into it or plow on to the next beat/model.
public sealed class QuotaExhaustedException(string message) : Exception(message);

/// The model returned no answer — a think-only response with nothing after
/// </think>, or a soft refusal. Surfaced as a real failure so callers never
/// silently store a blank cell.
public sealed class EmptyCompletionException(string message) : Exception(message);

public sealed class GatewayClient
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(600) };

    private readonly string _url;
    private readonly string _key;

    public GatewayClient(GatewayConfig cfg)
    {
        if (string.IsNullOrWhiteSpace(cfg.Endpoint) || string.IsNullOrWhiteSpace(cfg.ApiKey))
            throw new InvalidOperationException(
                "Gateway endpoint/key missing — set Gateway.Endpoint and Gateway.ApiKey in a "
                + "gitignored appsettings.json (copy appsettings.example.json).");
        _url = cfg.Endpoint;
        _key = cfg.ApiKey;
    }

    /// One chat completion through the gateway. Retries with exponential backoff on
    /// 429/5xx (honours numeric Retry-After); QuotaExhausted on the daily free-neuron
    /// cap (4006), which is not transient.
    public async Task<string> ChatAsync(string model, IReadOnlyList<ChatMessage> messages,
        double temperature = 0.8, int maxTokens = 8000, int retries = 6)
    {
        var json = JsonSerializer.Serialize(new { model, messages, temperature, max_tokens = maxTokens });
        var delay = 5;
        for (var attempt = 0; attempt < retries; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, _url);
            // A real User-Agent is load-bearing: Cloudflare's WAF 403s the default
            // agent as a bot before auth is even evaluated.
            req.Headers.TryAddWithoutValidation("User-Agent", "curl/8.5.0");
            req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {_key}");
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
            // Daily free-tier neuron cap (4006): backing off just re-hits the same
            // wall until the UTC reset — fail fast, let the caller stop.
            if (code == 429 && (body.Contains("4006") || body.Contains("daily free allocation")))
                throw new QuotaExhaustedException(body.Trim().Length > 0 ? body.Trim()
                    : "Cloudflare daily free neuron allocation exhausted");

            if (code is 429 or 500 or 502 or 503 && attempt < retries - 1)
            {
                var ra = resp.Headers.RetryAfter?.Delta?.TotalSeconds;
                await Task.Delay(TimeSpan.FromSeconds(ra is not null ? (int)ra.Value : delay));
                delay = Math.Min(delay * 2, 60);
                continue;
            }
            resp.EnsureSuccessStatusCode(); // other codes / final attempt: throw
        }
        throw new HttpRequestException("gateway: retries exhausted");
    }

    /// ChatAsync + StripThink, retrying when the answer comes back empty (a
    /// think-only response or soft refusal). Raises EmptyCompletion if every attempt
    /// is blank — so a refusal surfaces as a failure, never a silently-stored "".
    public async Task<string> CompleteAsync(string model, IReadOnlyList<ChatMessage> messages, int emptyRetries = 3)
    {
        for (var attempt = 0; attempt < emptyRetries; attempt++)
        {
            var text = GlmClient.StripThink(await ChatAsync(model, messages));
            if (text.Trim().Length > 0) return text;
            if (attempt < emptyRetries - 1) await Task.Delay(TimeSpan.FromSeconds(3));
        }
        throw new EmptyCompletionException($"{model} returned an empty answer after {emptyRetries} attempts");
    }
}
