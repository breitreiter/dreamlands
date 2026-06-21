// GLM on imp (free, OpenAI-compatible) — port of the glm_* helpers in
// forge/clients.py. Reasoning models may emit a <think>…</think> preamble;
// StripThink keeps only the answer that follows.
namespace Forge;

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

public sealed record ChatMessage(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content);

public sealed class GlmClient
{
    // Prompt eval on imp is slow; mirror the old client's generous ceiling.
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(900) };

    private readonly string _chatUrl;
    private readonly string _modelsUrl;
    private readonly string _model;
    private string? _resolvedModel;

    public GlmClient(GlmConfig cfg)
    {
        var b = cfg.Endpoint.TrimEnd('/');
        _chatUrl = b + "/chat/completions";
        _modelsUrl = b + "/models";
        _model = cfg.Model;
    }

    public async Task<string> ModelIdAsync()
    {
        if (!string.IsNullOrEmpty(_model) && _model != "auto") return _model;
        if (_resolvedModel is not null) return _resolvedModel;
        using var resp = await Http.GetAsync(_modelsUrl);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        _resolvedModel = doc.RootElement.GetProperty("data")[0].GetProperty("id").GetString()
            ?? throw new InvalidOperationException("GLM /models returned no id");
        return _resolvedModel;
    }

    public async Task<string> ChatAsync(IReadOnlyList<ChatMessage> messages,
        double temperature = 0.6, int maxTokens = 2000)
    {
        var payload = new { model = await ModelIdAsync(), messages, temperature, max_tokens = maxTokens };
        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var resp = await Http.PostAsync(_chatUrl, content);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message")
            .GetProperty("content").GetString() ?? "";
    }

    public static string StripThink(string text)
    {
        const string close = "</think>";
        var i = text.IndexOf(close, StringComparison.Ordinal);
        return (i >= 0 ? text[(i + close.Length)..] : text).Trim();
    }
}
