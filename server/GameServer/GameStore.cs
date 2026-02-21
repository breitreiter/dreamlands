using System.Text.Json;
using Dreamlands.Game;

namespace GameServer;

public interface IGameStore
{
    Task<PlayerState?> Load(string gameId);
    Task Save(PlayerState state);
}

public class LocalFileStore : IGameStore
{
    static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    readonly string _dir;

    public LocalFileStore(string directory)
    {
        _dir = directory;
        Directory.CreateDirectory(_dir);
    }

    public async Task<PlayerState?> Load(string gameId)
    {
        var path = Path.Combine(_dir, $"{gameId}.json");
        if (!File.Exists(path)) return null;
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<PlayerState>(json, JsonOpts);
    }

    public async Task Save(PlayerState state)
    {
        var path = Path.Combine(_dir, $"{state.GameId}.json");
        var json = JsonSerializer.Serialize(state, JsonOpts);
        await File.WriteAllTextAsync(path, json);
    }
}
