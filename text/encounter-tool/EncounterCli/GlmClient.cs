using System.Net.Http.Json;
using System.Text.Json;

namespace EncounterCli;

/// <summary>
/// Raw-HTTP chat client for the local GLM-4.5-Air server (llama.cpp on imp:8080).
/// Unlike <see cref="QwenClient"/> this POSTs a hand-built body so it can send the
/// two knobs GLM needs and the typed SDK can't: <c>min_p</c> and
/// <c>chat_template_kwargs.enable_thinking</c>. GLM is a reasoning model — its
/// chain-of-thought lands in <c>reasoning_content</c>, not <c>content</c>, so a
/// thinking-on call with a small <c>max_tokens</c> can return empty content; we
/// keep thinking off for color generation. Prompt eval on this box is slow
/// (~2 min for a few-K-token prompt), hence the long default timeout.
/// </summary>
sealed class GlmClient(string endpoint, string model, string apiKey = "local", int timeoutSeconds = 600)
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(900) };

    public async Task<string> CompleteAsync(
        string systemPrompt, string userPrompt,
        double temperature, double topP, double minP,
        int maxTokens, bool enableThinking)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt },
            },
            ["temperature"] = temperature,
            ["top_p"] = topP,
            ["max_tokens"] = maxTokens,
            ["chat_template_kwargs"] = new { enable_thinking = enableThinking },
        };
        if (minP > 0) body["min_p"] = minP;

        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint.TrimEnd('/') + "/chat/completions")
        {
            Content = JsonContent.Create(body),
        };
        req.Headers.Add("Authorization", $"Bearer {(string.IsNullOrEmpty(apiKey) ? "local" : apiKey)}");
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds > 0 ? timeoutSeconds : 600));

        var resp = await Http.SendAsync(req, cts.Token);
        var json = await resp.Content.ReadAsStringAsync(cts.Token);
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"GLM request failed: {(int)resp.StatusCode} {resp.StatusCode} — {json[..Math.Min(json.Length, 600)]}");

        using var doc = JsonDocument.Parse(json);
        var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
        return msg.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";
    }
}
