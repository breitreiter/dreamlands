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
        var byCategory = new Dictionary<string, List<int>>();

        for (int i = 0; i < entries.Length; i++)
        {
            var e = entries[i];
            var qualifiedId = string.IsNullOrEmpty(e.Category) ? e.Id : $"{e.Category}/{e.Id}";
            encounters.Add(new
            {
                id = qualifiedId,
                category = e.Category,
                trigger = e.Trigger,
                title = e.Title ?? e.Id,
                body = "Test body.",
                requires = e.Requires ?? Array.Empty<string>(),
                choices = e.Choices ?? new[]
                {
                    new
                    {
                        optionText = "Continue",
                        single = new { text = "You continue.", mechanics = e.Mechanics ?? Array.Empty<string>() }
                    }
                }
            });
            byId[qualifiedId] = new { category = e.Category, encounterIndex = i };
            if (!byCategory.ContainsKey(e.Category))
                byCategory[e.Category] = new List<int>();
            byCategory[e.Category].Add(i);
        }

        var bundle = new { index = new { byId, byCategory }, encounters };
        var json = JsonSerializer.Serialize(bundle, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return EncounterBundle.FromJson(json);
    }

    internal static GameSession MakeSession(
        Dreamlands.Map.Map? map = null,
        EncounterBundle? bundle = null,
        CombatBundle? combatBundle = null,
        int playerX = 1,
        int playerY = 1)
    {
        map ??= MakeMap();
        bundle ??= MakeBundle();
        var player = PlayerState.NewGame("test", 42, Balance);
        player.X = playerX;
        player.Y = playerY;
        return new GameSession(player, map, bundle, Balance, new Random(42), combatBundle);
    }

    internal static CombatBundle MakeCombatBundle(params CombatEncounter[] fights) =>
        CombatBundle.FromEncounters(fights);

    internal static CombatEncounter MakeFight(
        string id,
        string category,
        string trigger = "road",
        bool persistent = false,
        string[]? requires = null,
        string[]? winMechanics = null,
        string[]? fleeMechanics = null)
    {
        var winMech = string.Join("\n", (winMechanics ?? []).Select(m => "+" + m));
        var fleeBlock = fleeMechanics == null
            ? ""
            : "* flee\nYou get away.\n" + string.Join("\n", fleeMechanics.Select(m => "+" + m));
        var fight = CmbParser.ParseString($"""
            [title Test Fight]
            [stats hp=10]

            * move Attack
              narration: It swings.

            * win
            It falls.
            {winMech}

            * lose
            You fall.

            {fleeBlock}
            """);
        fight.Id = id;
        fight.Category = category;
        fight.Trigger = trigger;
        fight.Persistent = persistent;
        if (requires != null) fight.Requires.AddRange(requires);
        return fight;
    }

    internal record BundleEntry(
        string Id,
        string Category,
        string? Title = null,
        string? Trigger = null,
        string[]? Mechanics = null,
        object[]? Choices = null,
        string[]? Requires = null);
}
