using System.Text.Json;
using System.Text.Json.Serialization;
using Dreamlands.Encounter;
using Dreamlands.Flavor;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using GameServer;

var builder = WebApplication.CreateBuilder(args);


// Configure JSON to use camelCase and string enums
builder.Services.AddCors();
builder.Services.ConfigureHttpJsonOptions(opts =>
{
    opts.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    opts.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});

var app = builder.Build();

// ── Load shared read-only game data ──

// Find repo root (walk up from assembly location looking for Dreamlands.sln)
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

string? ParseArg(string[] a, string flag)
{
    for (int i = 0; i < a.Length - 1; i++)
        if (a[i] == flag) return a[i + 1];
    return null;
}

var repoRoot = FindRepoRoot();
var apiVersion = File.ReadAllText(Path.Combine(repoRoot, "api-version")).Trim();
var mapPath = ParseArg(args, "--map")
    ?? Environment.GetEnvironmentVariable("DREAMLANDS_MAP")
    ?? Path.Combine(repoRoot, "worlds/production/map.json");
var bundlePath = ParseArg(args, "--bundle")
    ?? Environment.GetEnvironmentVariable("DREAMLANDS_BUNDLE")
    ?? Path.Combine(repoRoot, "worlds/production/encounters.bundle.json");
var noEncounters = args.Contains("--no-encounters");
var noCamp = args.Contains("--no-camp");

Map map;
EncounterBundle bundle;
try
{
    map = MapSerializer.Load(mapPath);
    bundle = EncounterBundle.Load(bundlePath);
    app.Logger.LogInformation("Loaded map ({Width}x{Height}) and {Count} encounters",
        map.Width, map.Height, bundle.Encounters.Count);
    if (noEncounters)
        app.Logger.LogInformation("Overworld encounters SUPPRESSED (--no-encounters)");
    if (noCamp)
        app.Logger.LogInformation("End-of-day camp SUPPRESSED (--no-camp)");
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "Failed to load game data");
    return 1;
}

var balance = BalanceData.Default;
var store = new LocalFileStore(Path.Combine(repoRoot, "saves"));

// ── CORS for local dev ──
app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

// ── Helper: reconstruct session from persisted state ──

GameSession BuildSession(PlayerState player)
{
    var rng = new Random(player.Seed + player.VisitedNodes.Count);
    var session = new GameSession(player, map, bundle, balance, rng);

    // Restore active encounter from persisted state
    if (player.CurrentEncounterId is { } encId)
    {
        var enc = bundle.GetById(encId);
        if (enc != null)
        {
            session.Mode = SessionMode.InEncounter;
            session.CurrentEncounter = enc;
        }
        else
        {
            player.CurrentEncounterId = null;
        }
    }
    else if (player.PendingEndOfDay && !noCamp)
    {
        session.Mode = SessionMode.Camp;
    }
    else if (player.PendingEndOfDay && noCamp)
    {
        player.PendingEndOfDay = false;
    }
    return session;
}

StatusInfo BuildStatus(PlayerState p) => new()
{
    Name = p.Name,
    Bio = p.Bio,
    Health = p.Health,
    MaxHealth = p.MaxHealth,
    Spirits = p.Spirits,
    MaxSpirits = p.MaxSpirits,
    Gold = p.Gold,
    Time = p.Time.ToString(),
    Day = p.Day,
    Conditions = p.ActiveConditions.Select(kv =>
    {
        var def = balance.Conditions.GetValueOrDefault(kv.Key);
        var flavor = balance.ConditionFlavors.GetValueOrDefault(kv.Key);
        return new ConditionInfo
        {
            Id = kv.Key,
            Name = def?.Name ?? kv.Key,
            Stacks = kv.Value,
            Description = flavor?.Ongoing ?? "",
        };
    }).ToList(),
    Skills = Skills.All.Select(si =>
    {
        var level = p.Skills.GetValueOrDefault(si.Skill);
        return new SkillInfoDto
        {
            Id = si.ScriptName,
            Name = si.DisplayName,
            Level = level,
            Formatted = FormatSkillLevel(level),
            Flavor = SkillFlavor.Get(si.Skill, level),
        };
    }).ToList(),
};

NodeInfo BuildNodeInfo(Node node, PlayerState p)
{
    List<string>? services = null;
    if (node.Poi?.Kind == PoiKind.Settlement)
    {
        var isChapterhouse = node == map.StartingCity;
        services = ["market", "bank", isChapterhouse ? "chapterhouse" : "inn"];
    }

    return new()
    {
        X = node.X,
        Y = node.Y,
        Terrain = node.Terrain.ToString().ToLowerInvariant(),
        Region = node.Region?.Name,
        RegionTier = node.Region?.Tier,
        Description = node.Description,
        Poi = node.Poi != null ? new PoiInfo
        {
            Kind = node.Poi.Kind.ToString().ToLowerInvariant(),
            Name = node.Poi.Name ?? node.Poi.Type,
            DungeonId = node.Poi.DungeonId,
            DungeonCompleted = node.Poi.DungeonId != null
                ? p.CompletedDungeons.Contains(node.Poi.DungeonId) : null,
            Services = services,
        } : null,
    };
}

List<ExitInfo> BuildExits(GameSession session) =>
    Movement.GetExits(session).Select(e => new ExitInfo
    {
        Direction = e.Dir.ToString().ToLowerInvariant(),
        Terrain = e.Target.Terrain.ToString().ToLowerInvariant(),
        Poi = e.Target.Poi?.Name ?? e.Target.Poi?.Kind.ToString(),
    }).ToList();

ItemInfo BuildItemInfo(ItemInstance i, int playerX = 0, int playerY = 0)
{
    var def = balance.Items.GetValueOrDefault(i.DefId);
    return new ItemInfo
    {
        DefId = i.DefId,
        Name = i.DisplayName,
        Description = i.Description ?? (def != null ? FormatItemDescription(def) : null),
        Type = def?.Type.ToString().ToLowerInvariant() ?? "",
        Cost = def?.Cost != null ? balance.Character.CostMagnitudes.GetValueOrDefault(def.Cost.Value) : null,
        SkillModifiers = def?.SkillModifiers.ToDictionary(kv => kv.Key.ScriptName(), kv => kv.Value) ?? [],
        ResistModifiers = def?.ResistModifiers.ToDictionary(
            kv => kv.Key,
            kv => balance.Character.ResistBonusMagnitudes.GetValueOrDefault(kv.Value)) ?? [],
        ForagingBonus = def?.ForagingBonus ?? 0,
        Cures = def?.Cures.ToList() ?? [],
        IsEquippable = def?.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots,
        DestinationName = i.DestinationName,
        DestinationHint = i.DestinationX != null && i.DestinationY != null
            ? HaulGeneration.BuildRelativeHint(playerX, playerY, i.DestinationX.Value, i.DestinationY.Value)
            : i.DestinationHint,
        Payout = i.Payout,
    };
}

InventoryInfo BuildInventory(PlayerState p) => new()
{
    Pack = p.Pack.Select(i => BuildItemInfo(i, p.X, p.Y)).ToList(),
    PackCapacity = p.PackCapacity,
    Haversack = p.Haversack.Select(i => BuildItemInfo(i, p.X, p.Y)).ToList(),
    HaversackCapacity = p.HaversackCapacity,
    Equipment = new EquipmentInfo
    {
        Weapon = p.Equipment.Weapon != null ? BuildItemInfo(p.Equipment.Weapon) : null,
        Armor = p.Equipment.Armor != null ? BuildItemInfo(p.Equipment.Armor) : null,
        Boots = p.Equipment.Boots != null ? BuildItemInfo(p.Equipment.Boots) : null,
    },
};

MechanicsInfo BuildMechanics(PlayerState p)
{
    var resistances = new List<MechanicLine>();
    var encounterChecks = new List<MechanicLine>();
    var other = new List<MechanicLine>();

    // Resistances: for each condition, compute total resist bonus
    foreach (var (condId, condDef) in balance.Conditions)
    {
        if (condId is "disheartened" or "hungry") continue;
        // Map condition to its resist skill (mirrors SkillChecks.RollResist)
        var resistSkill = condId switch
        {
            "freezing" or "thirsty" or "lost" or "poisoned" => (Skill?)Skill.Bushcraft,
            "injured" => Skill.Combat,
            _ => null,
        };

        var skillBonus = resistSkill != null ? p.Skills.GetValueOrDefault(resistSkill.Value) : 0;
        var gearBonus = SkillChecks.GetResistBonus(condId, p, balance);
        var total = skillBonus + gearBonus;

        var source = resistSkill switch
        {
            { } s => $"{s.GetInfo().DisplayName} + gear",
            null => "Gear",
        };

        resistances.Add(new MechanicLine
        {
            Label = condDef.Name,
            Value = $"+{total}",
            Source = source,
        });
    }

    // Encounter checks: per-skill total (base + item bonus)
    foreach (var si in Skills.All)
    {
        if (si.Skill == Skill.Luck) continue;
        var skillLevel = p.Skills.GetValueOrDefault(si.Skill);
        var itemBonus = SkillChecks.GetItemBonus(si.Skill, p, balance);
        var total = skillLevel + itemBonus;

        var source = $"{si.DisplayName} + gear";

        encounterChecks.Add(new MechanicLine
        {
            Label = si.DisplayName,
            Value = FormatSkillLevel(total),
            Source = source,
        });
    }

    // Other: special computed bonuses
    var mercantile = p.Skills.GetValueOrDefault(Skill.Mercantile)
                   + SkillChecks.GetItemBonus(Skill.Mercantile, p, balance);
    other.Add(new MechanicLine
    {
        Label = "Better prices",
        Value = $"{mercantile}%",
        Source = "Mercantile + gear",
    });

    var luckLevel = p.Skills.GetValueOrDefault(Skill.Luck);
    var luckChances = balance.Character.LuckRerollChance;
    var rerollChance = luckChances[Math.Min(Math.Max(luckLevel, 0), luckChances.Count - 1)];
    other.Add(new MechanicLine
    {
        Label = "Reroll any failure",
        Value = $"{rerollChance}%",
        Source = "Luck",
    });

    // Foraging bonus: bushcraft skill + weapon foraging bonus
    var totalForaging = p.Skills.GetValueOrDefault(Skill.Bushcraft)
                      + SkillChecks.GetForagingBonus(p, balance);
    other.Add(new MechanicLine
    {
        Label = "Foraging checks",
        Value = FormatSkillLevel(totalForaging),
        Source = "Bushcraft + gear",
    });

    return new MechanicsInfo
    {
        Resistances = resistances,
        EncounterChecks = encounterChecks,
        Other = other,
    };
}

string ModeString(SessionMode m) => m switch
{
    SessionMode.Exploring => "exploring",
    SessionMode.InEncounter => "encounter",

    SessionMode.Camp => "camp",
    SessionMode.GameOver => "game_over",
    _ => m.ToString().ToLowerInvariant(),
};

GameResponse BuildInventoryResponse(GameSession s, PlayerState p) => new()
{
    Mode = ModeString(s.Mode),
    Status = BuildStatus(p),
    Node = BuildNodeInfo(s.CurrentNode, p),
    Inventory = BuildInventory(p),
    Mechanics = BuildMechanics(p),
};

EncounterInfo BuildEncounterInfo(Encounter encounter, List<Choice> choices) => new()
{
    Id = encounter.Id,
    Category = encounter.Category,
    Title = encounter.Title,
    Body = encounter.Body,
    Choices = choices.Select((c, i) => new ChoiceInfo
    {
        Index = i,
        Label = c.OptionLink ?? c.OptionText,
        Preview = c.OptionPreview,
    }).ToList(),
};

List<MechanicResultInfo> BuildMechanicResults(List<MechanicResult> results) =>
    results.Where(r => r is not MechanicResult.Navigation).Select(r => new MechanicResultInfo
    {
        Type = r.GetType().Name,
        Description = r switch
        {
            MechanicResult.HealthChanged h => $"Health {(h.Delta >= 0 ? "+" : "")}{h.Delta} ({h.NewValue})",
            MechanicResult.SpiritsChanged s => $"Spirits {(s.Delta >= 0 ? "+" : "")}{s.Delta} ({s.NewValue})",
            MechanicResult.GoldChanged g => $"Gold {(g.Delta >= 0 ? "+" : "")}{g.Delta} ({g.NewValue})",
            MechanicResult.SkillChanged sk => $"{sk.Skill.ScriptName()} {(sk.Delta >= 0 ? "+" : "")}{sk.Delta} ({FormatSkillLevel(sk.NewValue)})",
            MechanicResult.ItemGained ig => $"Gained: {ig.DisplayName}",
            MechanicResult.ItemLost il => $"Lost: {il.DisplayName}",
            MechanicResult.ItemEquipped ie => $"Equipped: {ie.DisplayName} ({ie.Slot})",
            MechanicResult.ItemUnequipped iu => $"Unequipped: {iu.DisplayName} ({iu.Slot})",
            MechanicResult.TagAdded t => $"Tag: {t.TagId}",
            MechanicResult.TagRemoved t => $"Tag removed: {t.TagId}",
            MechanicResult.ConditionAdded c => c.Stacks > 1 ? $"Condition: {c.ConditionId} x{c.Stacks}" : $"Condition: {c.ConditionId}",
            MechanicResult.ConditionRemoved c => $"Condition removed: {c.ConditionId}",
            MechanicResult.TimeAdvanced ta => $"Time: {ta.NewPeriod}, Day {ta.NewDay}",
            MechanicResult.Navigation n => $"Navigate to: {n.EncounterId}",
            MechanicResult.DungeonFinished => "Dungeon completed!",
            MechanicResult.DungeonFled => "Fled the dungeon!",
            _ => r.ToString() ?? "",
        },
    }).ToList();

GameResponse BuildExploringResponse(GameSession session, List<DeliveryInfo>? deliveries = null) => new()
{
    Mode = "exploring",
    Status = BuildStatus(session.Player),
    Node = BuildNodeInfo(session.CurrentNode, session.Player),
    Exits = BuildExits(session),
    Inventory = BuildInventory(session.Player),
    Mechanics = BuildMechanics(session.Player),
    Deliveries = deliveries,
};

GameResponse BuildEncounterResponse(GameSession session, Encounter encounter, List<Choice> choices) => new()
{
    Mode = "encounter",
    Status = BuildStatus(session.Player),
    Node = BuildNodeInfo(session.CurrentNode, session.Player),
    Encounter = BuildEncounterInfo(encounter, choices),
    Inventory = BuildInventory(session.Player),
    Mechanics = BuildMechanics(session.Player),
};

OutcomeInfo BuildOutcomeInfo(EncounterStep.ShowOutcome outcome, string nextAction = "end_encounter") => new()
{
    Preamble = outcome.Resolved.Preamble,
    Text = outcome.Resolved.Text,
    SkillCheck = outcome.Resolved.CheckResult is { } ck ? new SkillCheckInfo
    {
        Kind = ck.IsMeetsCheck ? "meets" : "check",
        Skill = ck.Skill.ScriptName(),
        Passed = ck.Passed,
        Rolled = ck.Rolled,
        Target = ck.Target,
        Modifier = ck.Modifier,
    } : null,
    Mechanics = BuildMechanicResults(outcome.Results),
    NextAction = nextAction,
};

GameResponse BuildOutcomeResponse(GameSession session, EncounterStep.ShowOutcome outcome, string nextAction = "end_encounter") => new()
{
    Mode = "outcome",
    Status = BuildStatus(session.Player),
    Outcome = BuildOutcomeInfo(outcome, nextAction),
    Inventory = BuildInventory(session.Player),
    Mechanics = BuildMechanics(session.Player),
};

GameResponse BuildCampResponse(GameSession session, CampInfo camp, List<DeliveryInfo>? deliveries = null) => new()
{
    Mode = "camp",
    Status = BuildStatus(session.Player),
    Node = BuildNodeInfo(session.CurrentNode, session.Player),
    Camp = camp,
    Deliveries = deliveries,
    Inventory = BuildInventory(session.Player),
    Mechanics = BuildMechanics(session.Player),
};

CampInfo BuildCampThreats(GameSession session)
{
    var node = session.CurrentNode;
    var biome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
    var tier = node.Region?.Tier ?? 1;
    var threats = EndOfDay.GetThreats(biome, tier, balance);

    return new CampInfo
    {
        Threats = threats.Select(t => new CampThreatInfo
        {
            ConditionId = t.Id,
            Name = t.Name,
            Warning = t.SpecialCure ?? t.SpecialEffect ?? "",
        }).ToList(),
    };
}

List<CampEventInfo> FormatCampEvents(List<EndOfDayEvent> events) =>
    events.Select(e => new CampEventInfo
    {
        Type = e.GetType().Name,
        Description = e switch
        {
            EndOfDayEvent.FoodConsumed f => f.Balanced
                ? $"Balanced meal: {string.Join(", ", f.FoodEaten)}"
                : $"Ate: {string.Join(", ", f.FoodEaten)}",
            EndOfDayEvent.Starving => "No food! Going hungry.",
            EndOfDayEvent.HungerChanged h => $"Hungry ({h.NewStacks} stacks)",
            EndOfDayEvent.HungerCured => "No longer hungry!",
            EndOfDayEvent.ResistPassed r => $"Resisted {r.ConditionId} (rolled {r.Check.Rolled} vs DC {r.Check.Target})",
            EndOfDayEvent.ResistFailed r => $"Failed to resist {r.ConditionId} (rolled {r.Check.Rolled} vs DC {r.Check.Target})",
            EndOfDayEvent.CureApplied c => c.Remaining > 0
                ? $"{c.ItemDefId} reduced {c.ConditionId} by {c.StacksRemoved} ({c.Remaining} remaining)"
                : $"{c.ItemDefId} cured {c.ConditionId}!",
            EndOfDayEvent.CureNegated c => $"{c.ItemDefId} negated — {c.ConditionId} contracted tonight",
            EndOfDayEvent.ConditionAcquired a => a.Stacks > 1
                ? $"Contracted {a.ConditionId} ({a.Stacks} stacks)"
                : $"Contracted {a.ConditionId}",
            EndOfDayEvent.ConditionCured c => $"{c.ConditionId} cured!",
            EndOfDayEvent.ConditionDrain d => $"{d.ConditionId}: -{d.HealthLost} health, -{d.SpiritsLost} spirits",
            EndOfDayEvent.SpecialEffect s => $"{s.ConditionId}: {s.Effect}",
            EndOfDayEvent.Foraged f => f.ItemsFound.Count > 0
                ? $"Foraged: {string.Join(", ", f.ItemsFound)} (rolled {f.Rolled})"
                : $"Found nothing while foraging (rolled {f.Rolled})",
            EndOfDayEvent.RestRecovery r => $"Rest: +{r.HealthGained} health, +{r.SpiritsGained} spirits",
            EndOfDayEvent.PlayerDied d => d.ConditionId != null
                ? $"Perished from {d.ConditionId}."
                : "Perished in the night.",
            _ => e.ToString() ?? "",
        },
    }).ToList();

// ── Endpoints ──

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", apiVersion }));

app.MapPost("/api/game/new", async () =>
{
    var rng = new Random();
    var gameId = Guid.NewGuid().ToString("N")[..12];
    var seed = rng.Next();
    var player = PlayerState.NewGame(gameId, seed, balance);

    if (map.StartingCity != null)
    {
        player.X = map.StartingCity.X;
        player.Y = map.StartingCity.Y;
    }

    var session = BuildSession(player);
    session.MarkVisited();

    // Start with the intro encounter if available
    var introEnc = bundle.GetById("00_Intro");
    if (introEnc != null)
    {
        var step = EncounterRunner.Begin(session, introEnc);
        await store.Save(player);
        return Results.Ok(new { gameId, state = BuildEncounterResponse(session, step.Encounter, step.VisibleChoices) });
    }

    await store.Save(player);
    return Results.Ok(new { gameId, state = BuildExploringResponse(session) });
});

app.MapGet("/api/game/{id}", async (string id) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var session = BuildSession(player);

    // Reconstruct current mode
    if (player.Health <= 0)
        return Results.Ok(new GameResponse
        {
            Mode = "game_over",
            Status = BuildStatus(player),
            Reason = "You have perished in the Dreamlands.",
        });

    if (player.PendingEndOfDay && !noCamp)
        return Results.Ok(BuildCampResponse(session, BuildCampThreats(session)));

    if (player.PendingEndOfDay && noCamp)
        player.PendingEndOfDay = false;

    if (session.CurrentEncounter is { } enc)
    {
        var choices = Choices.GetVisible(enc, player, balance);
        return Results.Ok(BuildEncounterResponse(session, enc, choices));
    }

    return Results.Ok(BuildExploringResponse(session));
});

app.MapPost("/api/game/{id}/action", async (string id, ActionRequest req) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var session = BuildSession(player);

    if (player.Health <= 0)
        return Results.Ok(new GameResponse
        {
            Mode = "game_over",
            Status = BuildStatus(player),
            Reason = "You have perished in the Dreamlands.",
        });

    GameResponse response;

    switch (req.Action)
    {
        case "rest_at_inn":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot rest while not exploring" });

            var innNode = session.CurrentNode;
            if (innNode.Poi?.Kind != PoiKind.Settlement)
                return Results.BadRequest(new { error = "Not at a settlement" });

            var innBiome = innNode.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
            var innTier = innNode.Region?.Tier ?? 1;
            var innEvents = Inn.StayOneNight(player, innBiome, innTier, balance, session.Rng);

            if (player.Health <= 0)
            {
                await store.Save(player);
                return Results.Ok(new GameResponse
                {
                    Mode = "camp_resolved",
                    Status = BuildStatus(player),
                    Reason = "You have perished in the Dreamlands.",
                    Camp = new CampInfo { Threats = [], Events = FormatCampEvents(innEvents) },
                    Node = BuildNodeInfo(innNode, player),
                });
            }

            session.Mode = SessionMode.Exploring;
            await store.Save(player);
            response = new GameResponse
            {
                Mode = "camp_resolved",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(innNode, player),
                Exits = BuildExits(session),
                Camp = new CampInfo { Threats = [], Events = FormatCampEvents(innEvents) },
                Inventory = BuildInventory(player),
            };
            break;
        }

        case "inn_full_recovery":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot rest while not exploring" });

            var innNode = session.CurrentNode;
            if (innNode.Poi?.Kind != PoiKind.Settlement)
                return Results.BadRequest(new { error = "Not at a settlement" });

            var (canRecover, _) = Inn.CanUseInn(player, balance);
            if (!canRecover)
                return Results.BadRequest(new { error = "Cannot fully recover — untreatable conditions" });

            var quote = Inn.GetQuote(player, balance);
            if (player.Gold < quote.GoldCost)
                return Results.BadRequest(new { error = "Not enough gold" });

            var innResult = Inn.StayFullRecovery(player, balance);
            await store.Save(player);

            response = new GameResponse
            {
                Mode = "exploring",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(innNode, player),
                Exits = BuildExits(session),
                InnRecovery = new InnRecoveryInfo
                {
                    NightsStayed = innResult.NightsStayed,
                    GoldSpent = innResult.GoldSpent,
                    HealthRecovered = innResult.HealthRecovered,
                    SpiritsRecovered = innResult.SpiritsRecovered,
                    ConditionsCleared = innResult.ConditionsCleared,
                    MedicinesConsumed = innResult.MedicinesConsumed,
                },
                Inventory = BuildInventory(player),
                Mechanics = BuildMechanics(player),
            };
            break;
        }

        case "chapterhouse_recover":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot rest while not exploring" });

            var innNode = session.CurrentNode;
            if (innNode.Poi?.Kind != PoiKind.Settlement)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (innNode != session.Map.StartingCity)
                return Results.BadRequest(new { error = "Not at the chapterhouse" });

            var chResult = Inn.StayChapterhouse(player, balance);
            await store.Save(player);

            response = new GameResponse
            {
                Mode = "exploring",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(innNode, player),
                Exits = BuildExits(session),
                InnRecovery = new InnRecoveryInfo
                {
                    NightsStayed = chResult.NightsStayed,
                    GoldSpent = 0,
                    HealthRecovered = chResult.HealthRecovered,
                    SpiritsRecovered = chResult.SpiritsRecovered,
                    ConditionsCleared = chResult.ConditionsCleared,
                    MedicinesConsumed = [],
                },
                Inventory = BuildInventory(player),
                Mechanics = BuildMechanics(player),
            };
            break;
        }

        case "move":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot move while not exploring" });

            if (!Enum.TryParse<Direction>(req.Direction, true, out var dir))
                return Results.BadRequest(new { error = $"Invalid direction: {req.Direction}" });

            var target = Movement.TryMove(session, dir);
            if (target == null)
                return Results.BadRequest(new { error = $"No exit {req.Direction}" });

            Movement.Execute(session, dir);

            // Clear conditions on arriving at a settlement and suppress
            // ambient threats overnight — the player has shelter and water.
            List<DeliveryInfo>? deliveries = null;
            if (session.CurrentNode.Poi?.Kind == PoiKind.Settlement)
            {
                player.ActiveConditions.Clear();
                player.PendingNoBiome = true;

                // Auto-deliver hauls destined for this settlement
                if (session.CurrentNode.Poi.SettlementId is { } arrivalId)
                {
                    var hauledItems = player.Pack.Where(i => i.HaulDefId != null).ToList();
                    if (hauledItems.Count > 0)
                    {
                        app.Logger.LogInformation("Arriving at settlement {ArrivalId} ({Name}). Pack hauls:",
                            arrivalId, session.CurrentNode.Poi.Name);
                        foreach (var h in hauledItems)
                            app.Logger.LogInformation("  {Name} destId={DestId} destName={DestName}",
                                h.DisplayName, h.DestinationSettlementId, h.DestinationName);
                    }

                    var delivered = HaulDelivery.Deliver(player, arrivalId, balance.Hauls);
                    if (delivered.Count > 0)
                    {
                        app.Logger.LogInformation("Delivered {Count} hauls:", delivered.Count);
                        foreach (var d in delivered)
                            app.Logger.LogInformation("  {Name} payout={Payout}", d.DisplayName, d.Payout);

                        deliveries = delivered.Select(d => new DeliveryInfo
                        {
                            Name = d.DisplayName,
                            Payout = d.Payout,
                            Flavor = d.DeliveryFlavor,
                        }).ToList();
                    }
                }
            }

            // Advance time by one segment per move
            if (player.Time < TimePeriod.Night)
            {
                player.Time = player.Time + 1;
            }
            else if (!noCamp)
            {
                player.Time = TimePeriod.Morning;
                player.Day++;
                player.PendingEndOfDay = true;
            }

            // Check for encounter trigger at new location
            // Only during mid-day periods (Midday/Afternoon/Evening) to avoid back-to-back with camp
            var node = session.CurrentNode;
            var midDay = player.Time is TimePeriod.Midday or TimePeriod.Afternoon or TimePeriod.Evening;
            if (node.Poi?.Kind == PoiKind.Encounter && midDay && !session.SkipEncounterTrigger && !noEncounters
                && session.Rng.NextDouble() < balance.Character.EncounterChance)
            {
                var enc = EncounterSelection.PickOverworld(session, node);
                if (enc != null)
                {
                    var step = EncounterRunner.Begin(session, enc);
                    await store.Save(player);
                    return Results.Ok(BuildEncounterResponse(session, step.Encounter, step.VisibleChoices));
                }
            }
            session.SkipEncounterTrigger = false;

            await store.Save(player);
            if (player.PendingEndOfDay && !noCamp)
            {
                session.Mode = SessionMode.Camp;
                response = BuildCampResponse(session, BuildCampThreats(session), deliveries);
            }
            else
            {
                if (noCamp) player.PendingEndOfDay = false;
                response = BuildExploringResponse(session, deliveries);
            }
            break;
        }

        case "choose":
        {
            if (session.CurrentEncounter == null)
                return Results.BadRequest(new { error = "No active encounter" });

            var choices = Choices.GetVisible(session.CurrentEncounter, player, balance);
            var idx = req.ChoiceIndex ?? -1;
            if (idx < 0 || idx >= choices.Count)
                return Results.BadRequest(new { error = $"Invalid choice index: {idx}" });

            var chosen = choices[idx];
            var result = EncounterRunner.Choose(session, chosen);

            switch (result)
            {
                case EncounterStep.ShowOutcome outcome:
                    response = BuildOutcomeResponse(session, outcome);
                    break;

                case EncounterStep.Finished finished:
                    switch (finished.Reason)
                    {
                        case FinishReason.NavigatedTo:
                            var next = EncounterSelection.ResolveNavigation(session, finished.NavigateToId!);
                            if (next != null)
                            {
                                var step = EncounterRunner.Begin(session, next);
                                await store.Save(player);
                                return Results.Ok(new GameResponse
                                {
                                    Mode = "encounter",
                                    Status = BuildStatus(player),
                                    Encounter = BuildEncounterInfo(step.Encounter, step.VisibleChoices),
                                    Outcome = finished.Outcome is { } o ? BuildOutcomeInfo(o) : null,
                                    Inventory = BuildInventory(player),
                                });
                            }
                            EncounterRunner.EndEncounter(session);
                            await store.Save(player);
                            return Results.Ok(BuildExploringResponse(session));

                        case FinishReason.DungeonFinished:
                            player.CurrentDungeonId = null;
                            await store.Save(player);
                            return Results.Ok(BuildOutcomeResponse(session, finished.Outcome!, "end_dungeon"));

                        case FinishReason.DungeonFled:
                            player.CurrentDungeonId = null;
                            await store.Save(player);
                            return Results.Ok(BuildOutcomeResponse(session, finished.Outcome!, "end_dungeon"));

                        case FinishReason.PlayerDied:
                            await store.Save(player);
                            return Results.Ok(new GameResponse
                            {
                                Mode = "outcome",
                                Status = BuildStatus(player),
                                Outcome = BuildOutcomeInfo(finished.Outcome!),
                                Inventory = BuildInventory(player),
                                Mechanics = BuildMechanics(player),
                                Reason = "You have perished in the Dreamlands.",
                            });

                        default: // Completed
                            EncounterRunner.EndEncounter(session);
                            await store.Save(player);
                            if (player.PendingEndOfDay && !noCamp)
                            {
                                session.Mode = SessionMode.Camp;
                                return Results.Ok(BuildCampResponse(session, BuildCampThreats(session)));
                            }
                            if (noCamp) player.PendingEndOfDay = false;
                            return Results.Ok(BuildExploringResponse(session));
                    }

                default:
                    response = BuildExploringResponse(session);
                    break;
            }
            break;
        }

        case "enter_dungeon":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot enter dungeon while not exploring" });

            var node = session.CurrentNode;
            if (node.Poi?.Kind != PoiKind.Dungeon || node.Poi.DungeonId == null)
                return Results.BadRequest(new { error = "No dungeon at current location" });

            if (player.CompletedDungeons.Contains(node.Poi.DungeonId))
                return Results.BadRequest(new { error = "Dungeon already completed" });

            player.CurrentDungeonId = node.Poi.DungeonId;
            var start = EncounterSelection.GetDungeonStart(session, node.Poi.DungeonId);
            if (start == null)
            {
                player.CurrentDungeonId = null;
                return Results.BadRequest(new { error = "Dungeon entrance is sealed" });
            }

            var step = EncounterRunner.Begin(session, start);
            await store.Save(player);
            response = BuildEncounterResponse(session, step.Encounter, step.VisibleChoices);
            break;
        }

        case "end_encounter":
        case "end_dungeon":
        {
            EncounterRunner.EndEncounter(session);
            await store.Save(player);

            if (player.PendingEndOfDay && !noCamp)
            {
                session.Mode = SessionMode.Camp;
                response = BuildCampResponse(session, BuildCampThreats(session));
            }
            else
            {
                if (noCamp) player.PendingEndOfDay = false;
                response = BuildExploringResponse(session);
            }
            break;
        }

        case "camp_resolve":
        {
            if (session.Mode != SessionMode.Camp)
                return Results.BadRequest(new { error = "Not in camp mode" });

            var node = session.CurrentNode;
            var campBiome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
            var campTier = node.Region?.Tier ?? 1;

            var campTerrain = node.Region?.Terrain ?? Terrain.Plains;
            var campEvents = EndOfDay.Resolve(
                player, campBiome, campTier,
                balance, session.Rng,
                createFood: (type, rng) =>
                {
                    var (name, desc) = FlavorText.FoodName(type, campTerrain, foraged: true, rng);
                    return new ItemInstance($"food_{type.ToString().ToLowerInvariant()}", name)
                        { FoodType = type, Description = desc };
                });

            var campInfo = BuildCampThreats(session);
            campInfo = new CampInfo
            {
                Threats = campInfo.Threats,
                Events = FormatCampEvents(campEvents),
            };

            if (player.Health <= 0)
            {
                await store.Save(player);
                return Results.Ok(new GameResponse
                {
                    Mode = "camp_resolved",
                    Status = BuildStatus(player),
                    Reason = "You have perished in the Dreamlands.",
                    Camp = campInfo,
                    Node = BuildNodeInfo(node, player),
                });
            }

            session.Mode = SessionMode.Exploring;
            await store.Save(player);
            response = new GameResponse
            {
                Mode = "camp_resolved",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(node, player),
                Exits = BuildExits(session),
                Camp = campInfo,
                Inventory = BuildInventory(player),
            };
            break;
        }

        case "market_order":
        {
            var sNode = session.CurrentNode;
            if (sNode.Poi?.Kind != PoiKind.Settlement || sNode.Poi.SettlementId == null)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (req.Order == null)
                return Results.BadRequest(new { error = "order required" });

            var settlementId = sNode.Poi.SettlementId;
            SettlementRunner.EnsureSettlement(session);
            if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
                return Results.BadRequest(new { error = "Settlement not initialized" });

            var order = new MarketOrder(
                req.Order.Buys.Select(b => new BuyLine(b.ItemId, b.Quantity)).ToList(),
                req.Order.Sells?.Select(s => new SellLine(s.ItemDefId)).ToList() ?? []);

            var mRng = new Random();
            ItemInstance CreateFood(FoodType ft, string biome, Random r)
            {
                var terrain = Enum.Parse<Terrain>(biome, ignoreCase: true);
                var (name, desc) = FlavorText.FoodName(ft, terrain, foraged: false, rng: r);
                return new ItemInstance($"food_{ft.ToString().ToLowerInvariant()}", name)
                {
                    FoodType = ft, Description = desc,
                };
            }
            var result = Market.ApplyOrder(player, order, settlementState, balance, mRng, CreateFood);
            await store.Save(player);

            response = new GameResponse
            {
                Mode = "exploring",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(sNode, player),
                Inventory = BuildInventory(player),
                Mechanics = BuildMechanics(player),
                MarketResult = new MarketOrderResultInfo
                {
                    Success = result.Success,
                    Results = result.Results.Select(r => new MarketLineResultInfo
                    {
                        Action = r.Action,
                        ItemId = r.ItemId,
                        Success = r.Success,
                        Message = r.Message,
                    }).ToList(),
                },
            };
            break;
        }

        case "claim_haul":
        {
            var hNode = session.CurrentNode;
            if (hNode.Poi?.Kind != PoiKind.Settlement || hNode.Poi.SettlementId == null)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (string.IsNullOrEmpty(req.OfferId))
                return Results.BadRequest(new { error = "offerId required" });

            SettlementRunner.EnsureSettlement(session);
            if (!player.Settlements.TryGetValue(hNode.Poi.SettlementId, out var haulState))
                return Results.BadRequest(new { error = "Settlement not initialized" });

            var claimResult = Market.ClaimHaul(player, req.OfferId, haulState);
            if (!claimResult.Success)
                return Results.BadRequest(new { error = claimResult.Message });

            await store.Save(player);
            response = new GameResponse
            {
                Mode = "exploring",
                Status = BuildStatus(player),
                Node = BuildNodeInfo(hNode, player),
                Inventory = BuildInventory(player),
                Mechanics = BuildMechanics(player),
                MarketResult = new MarketOrderResultInfo
                {
                    Success = true,
                    Results = [new MarketLineResultInfo
                    {
                        Action = "claim_haul",
                        ItemId = "haul",
                        Success = true,
                        Message = claimResult.Message,
                    }],
                },
            };
            break;
        }

        case "bank_deposit":
        {
            var bNode = session.CurrentNode;
            if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (string.IsNullOrEmpty(req.ItemId) || string.IsNullOrEmpty(req.Source))
                return Results.BadRequest(new { error = "itemId and source required" });

            SettlementRunner.EnsureSettlement(session);
            if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bDepositState))
                return Results.BadRequest(new { error = "Settlement not initialized" });

            var depositError = Bank.Deposit(player, req.ItemId, req.Source, bDepositState, balance);
            if (depositError != null)
                return Results.BadRequest(new { error = depositError });

            await store.Save(player);
            response = BuildInventoryResponse(session, player);
            break;
        }

        case "bank_withdraw":
        {
            var bNode = session.CurrentNode;
            if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (req.BankIndex == null)
                return Results.BadRequest(new { error = "bankIndex required" });

            SettlementRunner.EnsureSettlement(session);
            if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bWithdrawState))
                return Results.BadRequest(new { error = "Settlement not initialized" });

            var withdrawError = Bank.Withdraw(player, req.BankIndex.Value, bWithdrawState, balance);
            if (withdrawError != null)
                return Results.BadRequest(new { error = withdrawError });

            await store.Save(player);
            response = BuildInventoryResponse(session, player);
            break;
        }

        case "equip":
        {
            if (string.IsNullOrEmpty(req.ItemId))
                return Results.BadRequest(new { error = "ItemId is required" });

            var results = Mechanics.Apply([$"equip {req.ItemId}"], player, balance, session.Rng);
            if (results.Count == 0)
                return Results.BadRequest(new { error = $"Cannot equip '{req.ItemId}' — not in pack or not equippable" });

            response = BuildInventoryResponse(session, player);
            break;
        }

        case "unequip":
        {
            var slot = req.Slot;
            if (string.IsNullOrEmpty(slot))
                return Results.BadRequest(new { error = "Slot is required (weapon, armor, boots)" });

            var results = Mechanics.Apply([$"unequip {slot}"], player, balance, session.Rng);
            if (results.Count == 0)
                return Results.BadRequest(new { error = $"Nothing equipped in slot '{slot}'" });

            response = BuildInventoryResponse(session, player);
            break;
        }

        case "discard":
        {
            if (string.IsNullOrEmpty(req.ItemId))
                return Results.BadRequest(new { error = "ItemId is required" });

            var results = Mechanics.Apply([$"discard {req.ItemId}"], player, balance, session.Rng);
            if (results.Count == 0)
                return Results.BadRequest(new { error = $"Item '{req.ItemId}' not found in inventory" });

            response = BuildInventoryResponse(session, player);
            break;
        }

        default:
            return Results.BadRequest(new { error = $"Unknown action: {req.Action}" });
    }

    await store.Save(player);
    return Results.Ok(response);
});

app.MapGet("/api/game/{id}/market", async (string id) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var session = BuildSession(player);
    var node = session.CurrentNode;
    var tier = node.Region?.Tier ?? 1;

    if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
        return Results.BadRequest(new { error = "Not at a settlement" });

    var settlementId = node.Poi.SettlementId;
    SettlementRunner.EnsureSettlement(session);
    await store.Save(player);
    if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
        return Results.BadRequest(new { error = "Settlement not initialized" });

    int mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);

    var stock = Market.GetStock(settlementState, balance).Select(entry => new
    {
        id = entry.Item.Id,
        name = entry.Item.Name,
        type = entry.Item.Type.ToString().ToLowerInvariant(),
        buyPrice = Market.GetBuyFromSettlementPrice(entry.Item.Id, settlementState, balance, mercantile),
        quantity = entry.Quantity,
        skillModifiers = entry.Item.SkillModifiers.ToDictionary(
            kv => kv.Key.ScriptName(), kv => kv.Value),
        resistModifiers = entry.Item.ResistModifiers,
        description = FormatItemDescription(entry.Item),
    }).ToList();

    var hauls = settlementState.HaulOffers.Select(h => new
    {
        id = h.HaulOfferId,
        name = h.DisplayName,
        destinationName = h.DestinationName,
        destinationHint = h.DestinationX != null && h.DestinationY != null
            ? HaulGeneration.BuildRelativeHint(player.X, player.Y, h.DestinationX.Value, h.DestinationY.Value)
            : h.DestinationHint,
        payout = h.Payout,
        originFlavor = h.Description,
    }).ToList();

    // Sell prices for all player inventory items (excluding hauls)
    var sellPrices = new Dictionary<string, int>();
    void AddSellPrice(ItemInstance item)
    {
        if (sellPrices.ContainsKey(item.DefId)) return;
        if (!balance.Items.TryGetValue(item.DefId, out var def)) return;
        if (def.Type == ItemType.Haul) return;
        var price = Market.GetSellPrice(def, balance);
        if (price > 0) sellPrices[item.DefId] = price;
    }
    foreach (var item in player.Pack) AddSellPrice(item);
    foreach (var item in player.Haversack) AddSellPrice(item);
    if (player.Equipment.Weapon != null) AddSellPrice(player.Equipment.Weapon);
    if (player.Equipment.Armor != null) AddSellPrice(player.Equipment.Armor);
    if (player.Equipment.Boots != null) AddSellPrice(player.Equipment.Boots);

    return Results.Ok(new
    {
        tier,
        stock,
        hauls,
        sellPrices,
    });
});

app.MapGet("/api/game/{id}/inn", async (string id) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var session = BuildSession(player);
    var node = session.CurrentNode;

    if (node.Poi?.Kind != PoiKind.Settlement)
        return Results.BadRequest(new { error = "Not at a settlement" });

    var isChapterhouse = node == session.Map.StartingCity;
    var (canFullRecover, disqualifying) = Inn.CanUseInn(player, balance);
    var quote = Inn.GetQuote(player, balance);
    var needsRecovery = player.Health < player.MaxHealth
                     || player.Spirits < player.MaxSpirits
                     || player.ActiveConditions.Count > 0;

    return Results.Ok(new
    {
        isChapterhouse,
        canFullRecover = isChapterhouse || canFullRecover,
        disqualifyingConditions = isChapterhouse ? Array.Empty<string>() : disqualifying.ToArray(),
        quote = new
        {
            nights = quote.Nights,
            goldCost = isChapterhouse ? 0 : quote.GoldCost,
            healthRecovered = quote.HealthRecovered,
            spiritsRecovered = quote.SpiritsRecovered,
        },
        needsRecovery,
        canAfford = player.Gold >= (isChapterhouse ? 0 : quote.GoldCost),
    });
});

app.MapGet("/api/game/{id}/bank", async (string id) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var session = BuildSession(player);
    var node = session.CurrentNode;

    if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
        return Results.BadRequest(new { error = "Not at a settlement" });

    SettlementRunner.EnsureSettlement(session);
    if (!player.Settlements.TryGetValue(node.Poi.SettlementId, out var settlementState))
        return Results.BadRequest(new { error = "Settlement not initialized" });

    return Results.Ok(new
    {
        settlementName = node.Poi.Name,
        items = settlementState.Bank.Select(i => BuildItemInfo(i)).ToList(),
        capacity = balance.Settlements.BankCapacity,
        packFull = player.Pack.Count >= player.PackCapacity,
        haversackFull = player.Haversack.Count >= player.HaversackCapacity,
    });
});

app.MapGet("/api/game/{id}/discoveries", async (string id) =>
{
    var player = await store.Load(id);
    if (player == null) return Results.NotFound(new { error = "Game not found" });

    var discoveries = new List<DiscoveryInfo>();
    foreach (var encoded in player.VisitedNodes)
    {
        var (x, y) = PlayerState.DecodePosition(encoded);
        if (!map.InBounds(x, y)) continue;
        var node = map[x, y];
        if (node.Poi?.Kind is PoiKind.Settlement or PoiKind.Dungeon)
        {
            discoveries.Add(new DiscoveryInfo
            {
                X = x,
                Y = y,
                Kind = node.Poi.Kind.ToString().ToLowerInvariant(),
                Name = node.Poi.Name ?? node.Poi.Kind.ToString(),
            });
        }
    }

    return Results.Ok(discoveries);
});

string FormatSkillLevel(int level) => level >= 0 ? $"+{level}" : $"{level}";

string FormatItemDescription(ItemDef item)
{
    var parts = new List<string>();
    foreach (var (skill, mod) in item.SkillModifiers)
        parts.Add($"{skill.GetInfo().DisplayName} {(mod >= 0 ? "+" : "")}{mod}");
    foreach (var (resist, mod) in item.ResistModifiers)
        parts.Add($"{resist} resist {(mod >= 0 ? "+" : "")}{mod}");
    if (item.Cures.Count > 0)
        parts.Add($"Cures: {string.Join(", ", item.Cures)}");
    return string.Join(", ", parts);
}

app.Run();
return 0;
