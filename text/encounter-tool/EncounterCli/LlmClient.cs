using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Anthropic.SDK;

namespace EncounterCli;

/// <summary>Creates an IChatClient from nb-style appsettings and runs completions.</summary>
public sealed class LlmClient
{
    private readonly IChatClient _client;
    private const string SystemPrompt = "You are a helpful assistant that follows instructions precisely. Respond only with the requested output.";

    private LlmClient(IChatClient client)
    {
        _client = client;
    }

    public static LlmClient? TryCreate(string? configPath = null)
    {
        var config = LoadConfig(configPath);
        if (config == null) return null;

        var activeName = config["ActiveProvider"]?.Trim();
        if (string.IsNullOrEmpty(activeName))
        {
            Console.Error.WriteLine("ActiveProvider is not set in config.");
            return null;
        }

        var providers = config.GetSection("ChatProviders").GetChildren();
        var section = providers.FirstOrDefault(c =>
            string.Equals(c["Name"]?.Trim(), activeName, StringComparison.OrdinalIgnoreCase));
        if (section == null)
        {
            Console.Error.WriteLine($"No ChatProviders entry for '{activeName}' in config.");
            return null;
        }

        if (!string.Equals(activeName, "Anthropic", StringComparison.OrdinalIgnoreCase))
        {
            Console.Error.WriteLine($"Only Anthropic is supported. ActiveProvider is '{activeName}'.");
            return null;
        }

        var apiKey = section["ApiKey"]?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("ApiKey is missing for Anthropic in config.");
            return null;
        }

        var model = section["Model"]?.Trim() ?? Anthropic.SDK.Constants.AnthropicModels.Claude37Sonnet;
        var anthropicClient = new AnthropicClient(apiKey);
        var chatClient = new ChatClientBuilder(anthropicClient.Messages)
            .ConfigureOptions(options => options.ModelId = model)
            .Build();

        return new LlmClient(chatClient);
    }

    public async Task<string?> CompleteAsync(string userPrompt, string? systemPrompt = null, CancellationToken cancellationToken = default)
    {
        var messages = new[]
        {
            new ChatMessage(ChatRole.System, systemPrompt ?? SystemPrompt),
            new ChatMessage(ChatRole.User, userPrompt)
        };
        var options = new ChatOptions { MaxOutputTokens = 4096 };
        var response = await _client.GetResponseAsync(messages, options, cancellationToken);
        return response.Text;
    }

    private static IConfiguration? LoadConfig(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath))
        {
            configPath = Path.GetFullPath(configPath);
            if (!File.Exists(configPath))
            {
                Console.Error.WriteLine($"Config file not found: {configPath}");
                return null;
            }
            var dir = Path.GetDirectoryName(configPath)!;
            var name = Path.GetFileName(configPath);
            return new ConfigurationBuilder().SetBasePath(dir).AddJsonFile(name, optional: false).Build();
        }

        var defaultPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(defaultPath))
        {
            Console.Error.WriteLine($"No appsettings.json found at {defaultPath}. Use --config <path> or create appsettings.json in the project directory.");
            return null;
        }
        return new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false).Build();
    }
}
