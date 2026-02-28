using System.Text.Json;
using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration.Tests;

internal static class Helpers
{
    static readonly BalanceData Balance = BalanceData.Default;

    internal static Dreamlands.Map.Map MakeMap(int size = 3)
    {
        var map = new Dreamlands.Map.Map(size, size);
        foreach (var node in map.AllNodes())
            node.Terrain = Terrain.Plains;
        return map;
    }

    internal static EncounterBundle MakeBundle(params BundleEntry[] entries)
    {
        var encounters = new List<object>();
        var byId = new Dictionary<string, object>();
        var byCategory = new Dictionary<string, List<string>>();

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            encounters.Add(new
            {
                id = e.Id,
                category = e.Category,
                title = e.Title ?? e.Id,
                body = "Test body.",
                choices = e.Choices ?? new[]
                {
                    new
                    {
                        optionText = "Continue",
                        single = new { text = "You continue.", mechanics = e.Mechanics ?? Array.Empty<string>() }
                    }
                }
            });
            byId[e.Id] = new { category = e.Category, encounterIndex = i };
            if (!byCategory.ContainsKey(e.Category))
                byCategory[e.Category] = new List<string>();
            byCategory[e.Category].Add(e.Id);
        }

        var bundle = new { index = new { byId, byCategory }, encounters };
        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return EncounterBundle.FromJson(json);
    }

    internal static GameSession MakeSession(
        Dreamlands.Map.Map? map = null,
        EncounterBundle? bundle = null,
        int playerX = 1,
        int playerY = 1)
    {
        map ??= MakeMap();
        bundle ??= MakeBundle();
        var player = PlayerState.NewGame("test", 42, Balance);
        player.X = playerX;
        player.Y = playerY;
        return new GameSession(player, map, bundle, Balance, new Random(42));
    }

    internal record BundleEntry(
        string Id,
        string Category,
        string? Title = null,
        string[]? Mechanics = null,
        object[]? Choices = null);
}
