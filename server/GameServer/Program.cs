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
var mapPath = ParseArg(args, "--map")
    ?? Environment.GetEnvironmentVariable("DREAMLANDS_MAP")
    ?? Path.Combine(repoRoot, "worlds/production/map.json");
var bundlePath = ParseArg(args, "--bundle")
    ?? Environment.GetEnvironmentVariable("DREAMLANDS_BUNDLE")
    ?? Path.Combine(repoRoot, "worlds/production/encounters.bundle.json");
var noEncounters = args.Contains("--no-encounters");

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
    else if (player.PendingEndOfDay)
    {
        session.Mode = SessionMode.Camp;
    }
    else if (player.CurrentSettlementId != null)
    {
        session.Mode = SessionMode.AtSettlement;
    }

    return session;
}

StatusInfo BuildStatus(PlayerState p) => new()
{
    Health = p.Health,
    MaxHealth = p.MaxHealth,
    Spirits = p.Spirits,
    MaxSpirits = p.MaxSpirits,
    Gold = p.Gold,
    Time = p.Time.ToString(),
    Day = p.Day,
    Conditions = new Dictionary<string, int>(p.ActiveConditions),
    Skills = p.Skills.ToDictionary(
        kv => kv.Key.ScriptName(),
        kv => FormatSkillLevel(kv.Value)),
};

NodeInfo BuildNodeInfo(Node node, PlayerState p) => new()
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
        Name = node.Poi.Name,
        DungeonId = node.Poi.DungeonId,
        DungeonCompleted = node.Poi.DungeonId != null
            ? p.CompletedDungeons.Contains(node.Poi.DungeonId) : null,
    } : null,
};

List<ExitInfo> BuildExits(GameSession session) =>
    Movement.GetExits(session).Select(e => new ExitInfo
    {
        Direction = e.Dir.ToString().ToLowerInvariant(),
        Terrain = e.Target.Terrain.ToString().ToLowerInvariant(),
        Poi = e.Target.Poi?.Name ?? e.Target.Poi?.Kind.ToString(),
    }).ToList();

InventoryInfo BuildInventory(PlayerState p) => new()
{
    Pack = p.Pack.Select(i => new ItemInfo { DefId = i.DefId, Name = i.DisplayName, Description = i.Description }).ToList(),
    PackCapacity = p.PackCapacity,
    Haversack = p.Haversack.Select(i => new ItemInfo { DefId = i.DefId, Name = i.DisplayName, Description = i.Description }).ToList(),
    HaversackCapacity = p.HaversackCapacity,
    Equipment = new EquipmentInfo
    {
        Weapon = p.Equipment.Weapon != null ? new ItemInfo { DefId = p.Equipment.Weapon.DefId, Name = p.Equipment.Weapon.DisplayName } : null,
        Armor = p.Equipment.Armor != null ? new ItemInfo { DefId = p.Equipment.Armor.DefId, Name = p.Equipment.Armor.DisplayName } : null,
        Boots = p.Equipment.Boots != null ? new ItemInfo { DefId = p.Equipment.Boots.DefId, Name = p.Equipment.Boots.DisplayName } : null,
    },
};

string ModeString(SessionMode m) => m switch
{
    SessionMode.Exploring => "exploring",
    SessionMode.InEncounter => "encounter",
    SessionMode.AtSettlement => "at_settlement",
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
};

EncounterInfo BuildEncounterInfo(Encounter encounter, List<Choice> choices) => new()
{
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
    results.Select(r => new MechanicResultInfo
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

GameResponse BuildExploringResponse(GameSession session) => new()
{
    Mode = "exploring",
    Status = BuildStatus(session.Player),
    Node = BuildNodeInfo(session.CurrentNode, session.Player),
    Exits = BuildExits(session),
    Inventory = BuildInventory(session.Player),
};

GameResponse BuildEncounterResponse(GameSession session, Encounter encounter, List<Choice> choices) => new()
{
    Mode = "encounter",
    Status = BuildStatus(session.Player),
    Encounter = BuildEncounterInfo(encounter, choices),
    Inventory = BuildInventory(session.Player),
};

GameResponse BuildOutcomeResponse(GameSession session, EncounterStep.ShowOutcome outcome, string nextAction = "end_encounter") => new()
{
    Mode = "outcome",
    Status = BuildStatus(session.Player),
    Outcome = new OutcomeInfo
    {
        Preamble = outcome.Resolved.Preamble,
        Text = outcome.Resolved.Text,
        SkillCheck = outcome.Resolved.CheckResult is { } ck ? new SkillCheckInfo
        {
            Skill = ck.Skill.ScriptName(),
            Passed = ck.Passed,
            Rolled = ck.Rolled,
            Target = ck.Target,
            Modifier = ck.Modifier,
        } : null,
        Mechanics = BuildMechanicResults(outcome.Results),
        NextAction = nextAction,
    },
    Inventory = BuildInventory(session.Player),
};

GameResponse BuildCampResponse(GameSession session, CampInfo camp) => new()
{
    Mode = "camp",
    Status = BuildStatus(session.Player),
    Node = BuildNodeInfo(session.CurrentNode, session.Player),
    Camp = camp,
    Inventory = BuildInventory(session.Player),
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
            EndOfDayEvent.ConditionCured c => $"{c.ConditionId} cured!",
            EndOfDayEvent.ConditionDrain d => $"{d.ConditionId}: -{d.HealthLost} health, -{d.SpiritsLost} spirits",
            EndOfDayEvent.SpecialEffect s => $"{s.ConditionId}: {s.Effect}",
            EndOfDayEvent.RestRecovery r => $"Rest: +{r.HealthGained} health, +{r.SpiritsGained} spirits",
            EndOfDayEvent.PlayerDied d => d.ConditionId != null
                ? $"Perished from {d.ConditionId}."
                : "Perished in the night.",
            _ => e.ToString() ?? "",
        },
    }).ToList();

// ── Endpoints ──

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

    if (player.PendingEndOfDay)
        return Results.Ok(BuildCampResponse(session, BuildCampThreats(session)));

    if (player.CurrentSettlementId != null)
    {
        var node = session.CurrentNode;
        var tier = node.Region?.Tier ?? 1;
        return Results.Ok(new GameResponse
        {
            Mode = "at_settlement",
            Status = BuildStatus(player),
            Settlement = new SettlementInfo
            {
                Name = player.CurrentSettlementId,
                Tier = tier,
                Services = ["market"],
            },
            Node = BuildNodeInfo(node, player),
            Inventory = BuildInventory(player),
        });
    }

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

            // Advance time by one segment per move
            if (player.Time < TimePeriod.Night)
            {
                player.Time = player.Time + 1;
            }
            else
            {
                player.Time = TimePeriod.Morning;
                player.Day++;
                player.PendingEndOfDay = true;
            }

            // Check for encounter trigger at new location
            var node = session.CurrentNode;
            if (node.Poi?.Kind == PoiKind.Encounter && !session.SkipEncounterTrigger && !noEncounters)
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
            if (player.PendingEndOfDay)
            {
                session.Mode = SessionMode.Camp;
                response = BuildCampResponse(session, BuildCampThreats(session));
            }
            else
            {
                response = BuildExploringResponse(session);
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
                                return Results.Ok(BuildEncounterResponse(session, step.Encounter, step.VisibleChoices));
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
                                Mode = "game_over",
                                Status = BuildStatus(player),
                                Reason = "You have perished in the Dreamlands.",
                            });

                        default: // Completed
                            EncounterRunner.EndEncounter(session);
                            await store.Save(player);
                            if (player.PendingEndOfDay)
                            {
                                session.Mode = SessionMode.Camp;
                                return Results.Ok(BuildCampResponse(session, BuildCampThreats(session)));
                            }
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

            if (player.PendingEndOfDay)
            {
                session.Mode = SessionMode.Camp;
                response = BuildCampResponse(session, BuildCampThreats(session));
            }
            else
            {
                response = BuildExploringResponse(session);
            }
            break;
        }

        case "enter_settlement":
        {
            if (session.Mode != SessionMode.Exploring)
                return Results.BadRequest(new { error = "Cannot enter settlement while not exploring" });

            var settlement = SettlementRunner.Enter(session);
            if (settlement == null)
                return Results.BadRequest(new { error = "No settlement at current location" });

            await store.Save(player);
            response = new GameResponse
            {
                Mode = "at_settlement",
                Status = BuildStatus(player),
                Settlement = new SettlementInfo
                {
                    Name = settlement.Name,
                    Tier = settlement.Tier,
                    Services = settlement.Services,
                },
                Node = BuildNodeInfo(session.CurrentNode, player),
                Inventory = BuildInventory(player),
            };
            break;
        }

        case "leave_settlement":
        {
            SettlementRunner.Leave(session);
            await store.Save(player);
            response = BuildExploringResponse(session);
            break;
        }

        case "camp_resolve":
        {
            if (session.Mode != SessionMode.Camp)
                return Results.BadRequest(new { error = "Not in camp mode" });

            var campChoices = req.CampChoices ?? new CampResolveRequest();
            var node = session.CurrentNode;
            var campBiome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
            var campTier = node.Region?.Tier ?? 1;

            var campEvents = EndOfDay.Resolve(
                player, campBiome, campTier,
                campChoices.Food, campChoices.Medicine,
                balance, session.Rng);

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
                    Mode = "game_over",
                    Status = BuildStatus(player),
                    Reason = "You have perished in the Dreamlands.",
                    Camp = campInfo,
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
            if (session.Mode != SessionMode.AtSettlement)
                return Results.BadRequest(new { error = "Not at a settlement" });

            if (req.Order == null)
                return Results.BadRequest(new { error = "order required" });

            var settlementId = player.CurrentSettlementId!;
            if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
                return Results.BadRequest(new { error = "Settlement not initialized" });

            var sNode = session.CurrentNode;
            var sBiome = sNode.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";

            var order = new MarketOrder(
                req.Order.Buys.Select(b => new BuyLine(b.ItemId, b.Quantity)).ToList(),
                req.Order.Sells.Select(s => new SellLine(s.ItemDefId)).ToList());

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
            var result = Market.ApplyOrder(player, order, sBiome, settlementState, balance, mRng, CreateFood);
            await store.Save(player);

            var tier = sNode.Region?.Tier ?? 1;
            response = new GameResponse
            {
                Mode = "at_settlement",
                Status = BuildStatus(player),
                Settlement = new SettlementInfo
                {
                    Name = settlementId,
                    Tier = tier,
                    Services = ["market"],
                },
                Node = BuildNodeInfo(sNode, player),
                Inventory = BuildInventory(player),
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
    var settlementId = player.CurrentSettlementId;

    if (settlementId == null || !player.Settlements.TryGetValue(settlementId, out var settlementState))
        return Results.BadRequest(new { error = "Not at a settlement" });

    var biome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
    int mercantile = player.Skills.GetValueOrDefault(Skill.Mercantile);

    var stock = Market.GetStock(settlementState, balance).Select(entry => new
    {
        id = entry.Item.Id,
        name = entry.Item.Name,
        type = entry.Item.Type.ToString().ToLowerInvariant(),
        buyPrice = Market.GetBuyFromSettlementPrice(entry.Item.Id, settlementState, balance, mercantile),
        sellPrice = Market.GetSellToSettlementPrice(entry.Item, biome, settlementState, balance, mercantile),
        quantity = entry.Quantity,
        isFeaturedSell = entry.IsFeaturedSell,
        skillModifiers = entry.Item.SkillModifiers.ToDictionary(
            kv => kv.Key.ScriptName(), kv => kv.Value),
        resistModifiers = entry.Item.ResistModifiers,
        description = FormatItemDescription(entry.Item),
    }).ToList();

    var sellPrices = player.Pack.Concat(player.Haversack)
        .Select(i => i.DefId)
        .Distinct()
        .Where(id => balance.Items.ContainsKey(id))
        .ToDictionary(id => id, id =>
            Market.GetSellToSettlementPrice(balance.Items[id], biome, settlementState, balance, mercantile));

    return Results.Ok(new
    {
        tier,
        stock,
        sellPrices,
        featuredBuyItem = settlementState.FeaturedBuyItem,
        featuredBuyPremium = balance.Trade.FeaturedBuyPremium,
    });
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
