using System.Text.Json;
using System.Text.Json.Serialization;
using GameServer;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddSingleton<GameData>();
        services.AddSingleton<IGameStore>(sp =>
        {
            var cosmos = Environment.GetEnvironmentVariable("DREAMLANDS_COSMOS");
            if (!string.IsNullOrEmpty(cosmos))
            {
                var client = CosmosGameStore.CreateClient(cosmos);
                var container = client.GetContainer("dreamlands", "games");
                return new CosmosGameStore(container);
            }

            var savesPath = Environment.GetEnvironmentVariable("DREAMLANDS_SAVES");
            if (string.IsNullOrEmpty(savesPath))
            {
                var repoRoot = FindRepoRoot();
                savesPath = Path.Combine(repoRoot, "saves");
            }
            return new LocalFileStore(savesPath);
        });
        services.ConfigureHttpJsonOptions(opts =>
        {
            opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
    })
    .Build();

host.Run();

static string FindRepoRoot()
{
    var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
    while (dir != null)
    {
        if (File.Exists(Path.Combine(dir, "Dreamlands.sln"))) return dir;
        dir = Path.GetDirectoryName(dir);
    }
    return Directory.GetCurrentDirectory();
}
