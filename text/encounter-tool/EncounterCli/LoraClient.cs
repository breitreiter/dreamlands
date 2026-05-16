using System.Net.Http.Json;
using System.Text.Json;

namespace EncounterCli;

/// <summary>Calls a llama.cpp /v1/completions endpoint serving a base-model LoRA.</summary>
sealed class LoraClient(string baseUrl, string model = "lora")
{
    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };

    static readonly string[] StopSequences = ["\n[author:", "[author:"];

    // The model sometimes generates the expansion twice when EOS doesn't fire cleanly.
    // Detect exact doubling by checking if the first half of the text recurs verbatim.
    static string DeduplicateParagraphs(string text)
    {
        var mid = text.Length / 2;
        var first = text[..mid].Trim();
        if (first.Length > 20 && text[mid..].Contains(first[..Math.Min(20, first.Length)]))
            return first;
        return text;
    }

    public async Task<string?> CompleteAsync(string prompt, float temperature = 0.8f, float topP = 0.9f, float repeatPenalty = 1.15f, int nPredict = 300, CancellationToken ct = default)
    {
        var payload = new
        {
            model,
            prompt,
            n_predict = nPredict,
            temperature,
            top_p = topP,
            repeat_penalty = repeatPenalty,
            stop = StopSequences,
        };

        using var resp = await Http.PostAsJsonAsync(baseUrl.TrimEnd('/') + "/v1/completions", payload, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("text").GetString()?.Trim();
        return text != null ? DeduplicateParagraphs(text) : null;
    }
}
