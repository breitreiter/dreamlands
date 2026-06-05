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

    public Task<string> CompleteAsync(
        string systemPrompt, string userPrompt,
        double temperature, double topP, double minP,
        int maxTokens, bool enableThinking)
        => CompleteAsync(
            new[] { ("system", systemPrompt), ("user", userPrompt) },
            temperature, topP, minP, maxTokens, enableThinking);

    /// <summary>
    /// Multi-message variant: pass an ordered (role, content) list so callers can ride a
    /// few-shot example as a genuine prior user/assistant turn. Roles are "system",
    /// "user", "assistant".
    /// </summary>
    public async Task<string> CompleteAsync(
        IEnumerable<(string role, string content)> messages,
        double temperature, double topP, double minP,
        int maxTokens, bool enableThinking)
    {
        var body = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages.Select(m => new { role = m.role, content = m.content }).ToArray(),
            ["temperature"] = temperature,
            ["top_p"] = topP,
            ["max_tokens"] = maxTokens,
            ["chat_template_kwargs"] = new { enable_thinking = enableThinking },
            // Defensive: stop at any chat-template special token. A correctly-templated
            // instruct model stops on its own, but a base/mis-templated model will run on
            // and echo a whole synthetic transcript — clip it at the first turn marker.
            ["stop"] = new[] { "<|im_end|>", "<|im_start|>", "<|user|>", "<|assistant|>", "<|endoftext|>" },
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
