using System.Net.Http.Headers;
using System.Text;

namespace DreamlandsCli;

class GameClient(string baseUrl)
{
    readonly HttpClient _http = new() { BaseAddress = new Uri(baseUrl) };

    public async Task<string> NewGame()
    {
        var resp = await _http.PostAsync("/api/game/new", null);
        return await ReadResponse(resp);
    }

    public async Task<string> GetState(string gameId)
    {
        var resp = await _http.GetAsync($"/api/game/{gameId}");
        return await ReadResponse(resp);
    }

    public async Task<string> Action(string gameId, string actionJson)
    {
        var content = new StringContent(actionJson, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"/api/game/{gameId}/action", content);
        return await ReadResponse(resp);
    }

    public async Task<string> GetMarket(string gameId)
    {
        var resp = await _http.GetAsync($"/api/game/{gameId}/market");
        return await ReadResponse(resp);
    }

    public async Task<bool> IsReachable()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
            var resp = await _http.GetAsync("/api/game/healthcheck", cts.Token);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static async Task<string> ReadResponse(HttpResponseMessage resp)
    {
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            throw new GameClientException((int)resp.StatusCode, body);
        return body;
    }
}

class GameClientException(int statusCode, string body) : Exception($"HTTP {statusCode}: {body}")
{
    public int StatusCode => statusCode;
    public string Body => body;
}
