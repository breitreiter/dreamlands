using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace EncounterCli;

/// <summary>
/// Calls Qwen3 via the OpenAI-compatible chat endpoint using a DSPy-optimized
/// few-shot program loaded from programsDir/{author}_program.json.
/// Defaults to a voices/ folder next to the executable.
/// </summary>
sealed class ExpandClient(string baseUrl, string? programsDir = null, string model = ExpandClient.DefaultModel)
{
    public const string DefaultModel = "Qwen3-30B-A3B-Instruct-2507-UD-Q6_K_XL.gguf";

    static readonly string DefaultProgramsDir =
        Path.Combine(AppContext.BaseDirectory, "voices");

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(120) };
    static readonly Regex ThinkPattern = new(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.Compiled);
    static readonly Regex ExpansionPattern = new(@"\[\[ ## expansion ## \]\]\s*(.*?)(?:\s*\[\[ ## completed ## \]\]|$)", RegexOptions.Singleline | RegexOptions.Compiled);

    readonly Dictionary<string, JsonNode> _programs = [];

    JsonNode LoadProgram(string author, string sceneType)
    {
        var key = $"{author}_{sceneType}";
        if (_programs.TryGetValue(key, out var cached)) return cached;
        var dir = programsDir ?? DefaultProgramsDir;
        var path = Path.Combine(dir, $"{author}_{sceneType}_program.json");
        if (!File.Exists(path))
            throw new FileNotFoundException($"Program not found: {path}. Run scripts/dspy_expand.py first.");
        var node = JsonNode.Parse(File.ReadAllText(path))!;
        _programs[key] = node;
        return node;
    }

    public List<object> BuildMessages(string author, string sceneType, string flat)
    {
        var program = LoadProgram(author, sceneType)["expand"]!;
        var instructions = program["signature"]!["instructions"]!.GetValue<string>();
        var demos = program["demos"]!.AsArray();

        // The system message uses {{field}} as literal template placeholders (not C# interpolation).
        var system =
            "Your input fields are:\n" +
            "1. `author` (str): author code: HPL, REH, or CAS\n" +
            "2. `scene_type` (str): scene register: mundane, action, horror, dread, wonder, or revelation\n" +
            "3. `flat` (str): plain prose passage to rewrite\n" +
            "Your output fields are:\n" +
            "1. `expansion` (str): passage rewritten in the author's style\n" +
            "All interactions will be structured in the following way, with the appropriate values filled in.\n\n" +
            "[[ ## author ## ]]\n{author}\n\n" +
            "[[ ## scene_type ## ]]\n{scene_type}\n\n" +
            "[[ ## flat ## ]]\n{flat}\n\n" +
            "[[ ## expansion ## ]]\n{expansion}\n\n" +
            "[[ ## completed ## ]]\n" +
            $"In adhering to this structure, your objective is: \n{instructions}";

        var messages = new List<object>
        {
            new { role = "system", content = system }
        };

        foreach (var demo in demos)
        {
            var demoScene = demo!["scene_type"]?.GetValue<string>() ?? "dread";
            messages.Add(new
            {
                role = "user",
                content = $"[[ ## author ## ]]\n{demo["author"]}\n\n" +
                          $"[[ ## scene_type ## ]]\n{demoScene}\n\n" +
                          $"[[ ## flat ## ]]\n{demo["flat"]}"
            });
            messages.Add(new
            {
                role = "assistant",
                content = $"[[ ## expansion ## ]]\n{demo["expansion"]}\n\n[[ ## completed ## ]]"
            });
        }

        messages.Add(new
        {
            role = "user",
            content = $"[[ ## author ## ]]\n{author}\n\n" +
                      $"[[ ## scene_type ## ]]\n{sceneType}\n\n" +
                      $"[[ ## flat ## ]]\n{flat}\n\n" +
                      "Respond with the corresponding output fields, starting with the field " +
                      "`[[ ## expansion ## ]]`, and then ending with the marker for `[[ ## completed ## ]]`."
        });

        return messages;
    }

    static string ParseExpansion(string raw)
    {
        var text = ThinkPattern.Replace(raw, "").Trim();
        var m = ExpansionPattern.Match(text);
        return m.Success ? m.Groups[1].Value.Trim() : text;
    }

    public async Task<string?> ExpandAsync(string author, string sceneType, string beat, float temperature = 0.7f, CancellationToken ct = default)
    {
        var messages = BuildMessages(author, sceneType, beat);
        var payload = new { model, messages, temperature, max_tokens = 500 };
        var url = baseUrl.TrimEnd('/') + "/v1/chat/completions";

        using var resp = await Http.PostAsJsonAsync(url, payload, ct);
        resp.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
        var raw = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim();
        return raw != null ? ParseExpansion(raw) : null;
    }
}
