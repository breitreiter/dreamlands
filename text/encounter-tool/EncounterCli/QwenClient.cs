using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace EncounterCli;

/// <summary>
/// Minimal OpenAI-compatible chat client. Defaults target a local qwen
/// llama.cpp server (e.g. http://imp:8080) running the Qwen3-30B model.
/// One system + one user message per call. No streaming, no tool calls.
/// </summary>
sealed class QwenClient(string baseUrl, string model = QwenClient.DefaultModel)
{
    public const string DefaultModel = "Qwen3-30B-A3B-Instruct-2507-UD-Q6_K_XL.gguf";

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromMinutes(3) };

    public async Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        double temperature = 0.6,
        int maxTokens = 1500)
    {
        var payload = new
        {
            model,
            temperature,
            max_tokens = maxTokens,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var resp = await Http.PostAsJsonAsync($"{baseUrl.TrimEnd('/')}/v1/chat/completions", payload);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"qwen request failed: {(int)resp.StatusCode} {resp.StatusCode} — {body}");

        var json = JsonNode.Parse(body);
        return json?["choices"]?[0]?["message"]?["content"]?.GetValue<string>();
    }
}
