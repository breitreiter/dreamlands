using System.Text.Json;

namespace DreamlandsCli;

record SessionData(string GameId, string Url);

static class Session
{
    static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    static string FilePath => Path.Combine(AppContext.BaseDirectory, ".session.json");

    public static SessionData? Load()
    {
        var path = FilePath;
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SessionData>(json, JsonOpts);
    }

    public static void Save(SessionData session)
    {
        var json = JsonSerializer.Serialize(session, JsonOpts);
        File.WriteAllText(FilePath, json);
    }
}
