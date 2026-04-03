using System.Text.Json;
using Dreamlands.Encounter;
using Dreamlands.Flavor;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Dreamlands.Tactical;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace GameServer;

public class GameFunctions(GameData data, IGameStore store, ILogger<GameFunctions> log)
{
    // ── Endpoints ──

    [Function("Health")]
    public IActionResult Health(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest req)
    {
        return new OkObjectResult(new { status = "ok", apiVersion = data.ApiVersion });
    }

    [Function("ReloadBundle")]
    public IActionResult ReloadBundle(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "ops/reload-bundle")] HttpRequest req)
    {
        if (!data.IsDev)
            return new NotFoundResult();

        data.ReloadBundle();
        log.LogInformation("Encounter bundle reloaded");
        return new OkObjectResult(new { status = "reloaded" });
    }

    [Function("NewGame")]
    public async Task<IActionResult> NewGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/new")] HttpRequest req)
    {
        var rng = new Random();
        var gameId = Guid.NewGuid().ToString("N")[..12];
        var seed = rng.Next();
        var player = PlayerState.NewGame(gameId, seed, data.Balance);

        if (data.Map.StartingCity != null)
        {
            player.X = data.Map.StartingCity.X;
            player.Y = data.Map.StartingCity.Y;
        }

        var session = BuildSession(player);
        session.MarkVisited();

        player.NextEncounterMove = session.Rng.Next(
            data.Balance.Character.EncounterCadenceMin,
            data.Balance.Character.EncounterCadenceMax + 1);

        var introEnc = data.Bundle.GetById("intro/00_Intro");
        if (introEnc != null)
        {
            var step = EncounterRunner.Begin(session, introEnc);
            await store.Save(player);
            return new OkObjectResult(new { gameId, state = BuildEncounterResponse(session, step.Encounter, step.GatedChoices) });
        }

        await store.Save(player);
        return new OkObjectResult(new { gameId, state = BuildExploringResponse(session) });
    }

    [Function("GetGame")]
    public async Task<IActionResult> GetGame(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);

        if (player.PendingEndOfDay && !data.NoCamp)
            return new OkObjectResult(BuildCampResponse(session, BuildCampThreats(session)));

        if (player.PendingEndOfDay && data.NoCamp)
            player.PendingEndOfDay = false;

        if (session.Mode == SessionMode.InTactical && player.CurrentTacticalId is { } tacId)
        {
            var tacEnc = data.TacticalBundle?.GetEncounterById(tacId);
            var tacState = tacEnc != null ? DeserializeTacticalState(player) : null;
            if (tacEnc != null && tacState != null)
            {
                var step = TacticalRunner.Resume(session, tacEnc, tacState);
                return new OkObjectResult(BuildTacticalResponse(session, tacEnc, step, tacState));
            }
        }

        if (session.CurrentEncounter is { } enc)
        {
            var gated = Choices.GetAllWithLockState(enc, player, data.Balance);
            return new OkObjectResult(BuildEncounterResponse(session, enc, gated));
        }

        return new OkObjectResult(BuildExploringResponse(session));
    }

    [Function("GameAction")]
    public async Task<IActionResult> GameAction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/action")] HttpRequest req,
        string id)
    {
        var actionReq = await req.ReadFromJsonAsync<ActionRequest>();
        if (actionReq == null) return new BadRequestObjectResult(new { error = "Invalid request body" });

        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);

        GameResponse response;

        switch (actionReq.Action)
        {
            case "inn_full_recovery":
            {
                if (session.Mode != SessionMode.Exploring)
                    return new BadRequestObjectResult(new { error = "Cannot rest while not exploring" });

                var innNode = session.CurrentNode;
                if (innNode.Poi?.Kind != PoiKind.Settlement)
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                var (canRecover, _) = Inn.CanUseInn(player, data.Balance);
                if (!canRecover)
                    return new BadRequestObjectResult(new { error = "Cannot fully recover — untreatable conditions" });

                var quote = Inn.GetQuote(player, data.Balance);
                if (player.Gold < quote.GoldCost)
                    return new BadRequestObjectResult(new { error = "Not enough gold" });

                var innResult = Inn.StayAtInn(player, data.Balance);
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
                    return new BadRequestObjectResult(new { error = "Cannot rest while not exploring" });

                var innNode = session.CurrentNode;
                if (innNode.Poi?.Kind != PoiKind.Settlement)
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                if (innNode != session.Map.StartingCity)
                    return new BadRequestObjectResult(new { error = "Not at the chapterhouse" });

                var chResult = Inn.StayChapterhouse(player, data.Balance);
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
                    return new BadRequestObjectResult(new { error = "Cannot move while not exploring" });

                if (player.CurrentDungeonId != null)
                    return new BadRequestObjectResult(new { error = "Cannot move while in a dungeon — leave the dungeon first" });

                if (!Enum.TryParse<Direction>(actionReq.Direction, true, out var dir))
                    return new BadRequestObjectResult(new { error = $"Invalid direction: {actionReq.Direction}" });

                var target = Movement.TryMove(session, dir);
                if (target == null)
                    return new BadRequestObjectResult(new { error = $"No exit {actionReq.Direction}" });

                Movement.Execute(session, dir);

                List<DeliveryInfo>? deliveries = null;
                if (session.CurrentNode.Poi?.Kind == PoiKind.Settlement)
                {
                    var balance = BalanceData.Default;
                    foreach (var cid in player.ActiveConditions.Keys.ToList())
                        if (balance.Conditions.TryGetValue(cid, out var cdef) && cdef.ClearedOnSettlement)
                            player.ActiveConditions.Remove(cid);
                    player.PendingNoBiome = true;

                    SettlementRunner.EnsureSettlement(session);

                    if (session.CurrentNode.Poi.SettlementId is { } arrivalId)
                    {
                        var hauledItems = player.Pack.Where(i => i.HaulDefId != null).ToList();
                        if (hauledItems.Count > 0)
                        {
                            log.LogInformation("Arriving at settlement {ArrivalId} ({Name}). Pack hauls:",
                                arrivalId, session.CurrentNode.Poi.Name);
                            foreach (var h in hauledItems)
                                log.LogInformation("  {Name} destId={DestId} destName={DestName}",
                                    h.DisplayName, h.DestinationSettlementId, h.DestinationName);
                        }

                        var delivered = HaulDelivery.Deliver(player, arrivalId, data.Balance.Hauls, session.Rng, data.Balance);
                        if (delivered.Count > 0)
                        {
                            log.LogInformation("Delivered {Count} hauls:", delivered.Count);
                            foreach (var d in delivered)
                                log.LogInformation("  {Name} payout={Payout}", d.DisplayName, d.Payout);

                            deliveries = delivered.Select(d => new DeliveryInfo
                            {
                                Name = d.DisplayName,
                                Payout = d.Payout,
                                Flavor = d.DeliveryFlavor,
                            }).ToList();
                        }
                    }
                }

                if (player.Time < TimePeriod.Night)
                {
                    player.Time = player.Time + 1;
                }
                else if (!data.NoCamp)
                {
                    player.Time = TimePeriod.Morning;
                    player.Day++;
                    player.PendingEndOfDay = true;
                }

                var node = session.CurrentNode;
                player.MoveCount++;

                if (player.MoveCount >= player.NextEncounterMove)
                {
                    player.NextEncounterMove = player.MoveCount
                        + session.Rng.Next(data.Balance.Character.EncounterCadenceMin,
                                           data.Balance.Character.EncounterCadenceMax + 1);

                    var eligible = player.Time is not TimePeriod.Morning
                                   && node.Poi is null;

                    if (eligible && !data.NoEncounters)
                    {
                        var enc = EncounterSelection.PickOverworld(session, node);
                        if (enc != null)
                        {
                            var step = EncounterRunner.Begin(session, enc);
                            await store.Save(player);
                            return new OkObjectResult(BuildEncounterResponse(session, step.Encounter, step.GatedChoices));
                        }
                    }
                }

                await store.Save(player);
                if (player.PendingEndOfDay && !data.NoCamp)
                {
                    session.Mode = SessionMode.Camp;
                    response = BuildCampResponse(session, BuildCampThreats(session), deliveries);
                }
                else
                {
                    if (data.NoCamp) player.PendingEndOfDay = false;
                    response = BuildExploringResponse(session, deliveries);
                }
                break;
            }

            case "travel":
            {
                if (session.Mode != SessionMode.Exploring)
                    return new BadRequestObjectResult(new { error = "Cannot travel while not exploring" });
                if (player.CurrentDungeonId != null)
                    return new BadRequestObjectResult(new { error = "Cannot travel while in a dungeon" });
                if (actionReq.Path is not { Count: >= 2 } proposedPath)
                    return new BadRequestObjectResult(new { error = "Path is required (at least 2 points)" });

                // Validate start matches current position
                if (proposedPath[0].X != player.X || proposedPath[0].Y != player.Y)
                    return new BadRequestObjectResult(new { error = "Path must start at current position" });

                var stepsCompleted = 0;
                string stopReason = "arrived";
                List<DeliveryInfo> allDeliveries = [];

                for (var i = 1; i < proposedPath.Count; i++)
                {
                    var from = proposedPath[i - 1];
                    var to = proposedPath[i];
                    var dx = to.X - from.X;
                    var dy = to.Y - from.Y;

                    // Validate each step is a single cardinal move
                    if (Math.Abs(dx) + Math.Abs(dy) != 1)
                        return new BadRequestObjectResult(new { error = $"Invalid step at index {i}: not adjacent" });

                    var stepDir = dx == 1 ? Direction.East
                        : dx == -1 ? Direction.West
                        : dy == 1 ? Direction.South
                        : Direction.North;

                    // Validate the move is legal (not into water, not out of bounds)
                    if (Movement.TryMove(session, stepDir) == null)
                        return new BadRequestObjectResult(new { error = $"Blocked step at index {i}" });

                    Movement.Execute(session, stepDir);
                    stepsCompleted = i;

                    // Settlement arrival logic (same as move)
                    if (session.CurrentNode.Poi?.Kind == PoiKind.Settlement)
                    {
                        foreach (var cid in player.ActiveConditions.Keys.ToList())
                            if (data.Balance.Conditions.TryGetValue(cid, out var cdef) && cdef.ClearedOnSettlement)
                                player.ActiveConditions.Remove(cid);
                        player.PendingNoBiome = true;

                        SettlementRunner.EnsureSettlement(session);

                        if (session.CurrentNode.Poi.SettlementId is { } arrivalId)
                        {
                            var delivered = HaulDelivery.Deliver(player, arrivalId, data.Balance.Hauls, session.Rng, data.Balance);
                            if (delivered.Count > 0)
                                allDeliveries.AddRange(delivered.Select(d => new DeliveryInfo
                                {
                                    Name = d.DisplayName,
                                    Payout = d.Payout,
                                    Flavor = d.DeliveryFlavor,
                                }));
                        }
                    }

                    // Time advancement
                    if (player.Time < TimePeriod.Night)
                    {
                        player.Time = player.Time + 1;
                    }
                    else if (!data.NoCamp)
                    {
                        player.Time = TimePeriod.Morning;
                        player.Day++;
                        player.PendingEndOfDay = true;
                    }

                    // End-of-day: auto-resolve camp during travel
                    if (player.PendingEndOfDay && !data.NoCamp)
                    {
                        var campNode = session.CurrentNode;
                        var campBiome = campNode.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
                        var campTier = campNode.Region?.Tier ?? 1;
                        var campTerrain = campNode.Region?.Terrain ?? Terrain.Plains;
                        var startCity = data.Map.StartingCity;

                        EndOfDay.Resolve(
                            player, campBiome, campTier,
                            data.Balance, session.Rng,
                            startX: startCity?.X ?? 0, startY: startCity?.Y ?? 0,
                            createFood: (type, rng) =>
                            {
                                var (name, desc) = FlavorText.FoodName(type, campTerrain, foraged: true, rng);
                                return new ItemInstance($"food_{type.ToString().ToLowerInvariant()}", name)
                                    { FoodType = type, Description = desc };
                            });

                        player.PendingEndOfDay = false;

                        // If player was rescued (died overnight), stop travel
                        if (player.Health <= 0)
                        {
                            stopReason = "rescued";
                            break;
                        }
                    }

                    // Encounter check
                    player.MoveCount++;
                    if (player.MoveCount >= player.NextEncounterMove)
                    {
                        player.NextEncounterMove = player.MoveCount
                            + session.Rng.Next(data.Balance.Character.EncounterCadenceMin,
                                               data.Balance.Character.EncounterCadenceMax + 1);

                        var encNode = session.CurrentNode;
                        var eligible = player.Time is not TimePeriod.Morning
                                       && encNode.Poi is null;

                        if (eligible && !data.NoEncounters)
                        {
                            var enc = EncounterSelection.PickOverworld(session, encNode);
                            if (enc != null)
                            {
                                var step = EncounterRunner.Begin(session, enc);
                                await store.Save(player);
                                return new OkObjectResult(new GameResponse
                                {
                                    Mode = "encounter",
                                    Status = BuildStatus(player),
                                    Node = BuildNodeInfo(session.CurrentNode, player),
                                    Encounter = BuildEncounterInfo(step.Encounter, step.GatedChoices),
                                    Inventory = BuildInventory(player),
                                    Mechanics = BuildMechanics(player),
                                    Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                                    Travel = new TravelInfo
                                    {
                                        Path = proposedPath,
                                        StepsCompleted = stepsCompleted,
                                        StopReason = "encounter",
                                    },
                                });
                            }
                        }
                    }
                }

                await store.Save(player);
                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(session.CurrentNode, player, session),
                    Exits = BuildExits(session),
                    Inventory = BuildInventory(player),
                    Mechanics = BuildMechanics(player),
                    Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                    DungeonHub = BuildDungeonHubInfo(session, session.CurrentNode),
                    Travel = new TravelInfo
                    {
                        Path = proposedPath,
                        StepsCompleted = stepsCompleted,
                        StopReason = stopReason,
                    },
                };
                break;
            }

            case "choose":
            {
                if (session.CurrentEncounter == null)
                    return new BadRequestObjectResult(new { error = "No active encounter" });

                var allChoices = session.CurrentEncounter.Choices;
                var idx = actionReq.ChoiceIndex ?? -1;
                if (idx < 0 || idx >= allChoices.Count)
                    return new BadRequestObjectResult(new { error = $"Invalid choice index: {idx}" });

                var chosen = allChoices[idx];
                if (chosen.Requires != null && !Conditions.Evaluate(chosen.Requires, player, data.Balance, Random.Shared))
                    return new BadRequestObjectResult(new { error = "Choice is locked" });
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
                                // Check tactical bundle first
                                var tacTarget = data.TacticalBundle?.ResolveNavigation(
                                    finished.NavigateToId!, session.CurrentEncounter?.Category);
                                if (tacTarget != null)
                                {
                                    EncounterRunner.EndEncounter(session);
                                    var tacResponse = BeginTacticalEncounter(session, player, tacTarget);
                                    if (finished.Outcome is { } oo)
                                        tacResponse.Outcome = BuildOutcomeInfo(oo);
                                    await store.Save(player);
                                    return new OkObjectResult(tacResponse);
                                }

                                var next = EncounterSelection.ResolveNavigation(session, finished.NavigateToId!, session.CurrentNode);
                                if (next != null)
                                {
                                    var step = EncounterRunner.Begin(session, next);
                                    await store.Save(player);
                                    return new OkObjectResult(new GameResponse
                                    {
                                        Mode = "encounter",
                                        Status = BuildStatus(player),
                                        Encounter = BuildEncounterInfo(step.Encounter, step.GatedChoices),
                                        Outcome = finished.Outcome is { } o ? BuildOutcomeInfo(o) : null,
                                        Inventory = BuildInventory(player),
                                    });
                                }
                                EncounterRunner.EndEncounter(session);
                                await store.Save(player);
                                return new OkObjectResult(BuildExploringResponse(session));

                            case FinishReason.DungeonFinished:
                                player.CurrentDungeonId = null;
                                await store.Save(player);
                                return new OkObjectResult(BuildOutcomeResponse(session, finished.Outcome!, "end_dungeon"));

                            case FinishReason.DungeonFled:
                                player.CurrentDungeonId = null;
                                await store.Save(player);
                                return new OkObjectResult(BuildOutcomeResponse(session, finished.Outcome!, "end_dungeon"));

                            case FinishReason.PlayerDied:
                            {
                                var sc = data.Map.StartingCity;
                                var encRescue = Rescue.Apply(player, sc?.X ?? 0, sc?.Y ?? 0, data.Balance);
                                await store.Save(player);
                                return new OkObjectResult(new GameResponse
                                {
                                    Mode = "rescued",
                                    Status = BuildStatus(player),
                                    Outcome = BuildOutcomeInfo(finished.Outcome!),
                                    Rescue = new RescueInfo
                                    {
                                        LostItems = encRescue.LostItems,
                                        GoldLost = encRescue.GoldLost,
                                    },
                                    Node = BuildNodeInfo(session.CurrentNode, player),
                                    Exits = BuildExits(session),
                                    Inventory = BuildInventory(player),
                                });
                            }

                            default: // Completed
                                EncounterRunner.EndEncounter(session);
                                await store.Save(player);
                                if (player.PendingEndOfDay && !data.NoCamp)
                                {
                                    session.Mode = SessionMode.Camp;
                                    return new OkObjectResult(BuildCampResponse(session, BuildCampThreats(session)));
                                }
                                if (data.NoCamp) player.PendingEndOfDay = false;
                                return new OkObjectResult(BuildExploringResponse(session));
                        }

                    default:
                        response = BuildExploringResponse(session);
                        break;
                }
                break;
            }

            case "tactical_approach":
            {
                if (session.Mode != SessionMode.InTactical)
                    return new BadRequestObjectResult(new { error = "Not in a tactical encounter" });
                var tacEnc = data.TacticalBundle?.GetEncounterById(player.CurrentTacticalId!);
                if (tacEnc == null)
                    return new BadRequestObjectResult(new { error = "Tactical encounter not found" });
                var tacState = DeserializeTacticalState(player);
                if (tacState == null)
                    return new BadRequestObjectResult(new { error = "No tactical state" });

                if (!Enum.TryParse<ApproachKind>(actionReq.Approach, ignoreCase: true, out var approachKind))
                    return new BadRequestObjectResult(new { error = $"Invalid approach: {actionReq.Approach}" });

                var tacStep = TacticalRunner.ApplyApproach(session, tacEnc, tacState, approachKind);
                SerializeTacticalState(player, tacState);
                await store.Save(player);
                response = BuildTacticalResponse(session, tacEnc, tacStep, tacState);
                break;
            }

            case "tactical_act":
            {
                if (session.Mode != SessionMode.InTactical)
                    return new BadRequestObjectResult(new { error = "Not in a tactical encounter" });
                var tacEnc2 = data.TacticalBundle?.GetEncounterById(player.CurrentTacticalId!);
                if (tacEnc2 == null)
                    return new BadRequestObjectResult(new { error = "Tactical encounter not found" });
                var tacState2 = DeserializeTacticalState(player);
                if (tacState2 == null)
                    return new BadRequestObjectResult(new { error = "No tactical state" });

                if (!Enum.TryParse<TacticalAction>(actionReq.TacticalAction, ignoreCase: true, out var tacAction))
                    return new BadRequestObjectResult(new { error = $"Invalid tactical action: {actionReq.TacticalAction}" });

                var tacStep2 = TacticalRunner.Act(session, tacEnc2, tacState2, tacAction, actionReq.OpeningIndex ?? 0);

                if (tacStep2 is TacticalStep.Finished)
                {
                    // Clear tactical state
                    player.CurrentTacticalId = null;
                    player.TacticalStateJson = null;
                    session.Mode = SessionMode.Exploring;
                }
                else
                {
                    SerializeTacticalState(player, tacState2);
                }
                await store.Save(player);
                response = BuildTacticalResponse(session, tacEnc2, tacStep2, tacState2);
                break;
            }

            case "end_tactical":
            {
                player.CurrentTacticalId = null;
                player.TacticalStateJson = null;
                session.Mode = SessionMode.Exploring;
                await store.Save(player);
                response = BuildExploringResponse(session);
                break;
            }

            case "enter_dungeon":
            {
                if (session.Mode != SessionMode.Exploring)
                    return new BadRequestObjectResult(new { error = "Cannot enter dungeon while not exploring" });

                var node = session.CurrentNode;
                if (node.Poi?.Kind != PoiKind.Dungeon || node.Poi.DungeonId == null)
                    return new BadRequestObjectResult(new { error = "No dungeon at current location" });

                if (player.CompletedDungeons.Contains(node.Poi.DungeonId))
                    return new BadRequestObjectResult(new { error = "Dungeon already completed" });

                player.CurrentDungeonId = node.Poi.DungeonId;
                var start = EncounterSelection.GetDungeonStart(session, node);
                if (start == null)
                {
                    player.CurrentDungeonId = null;
                    return new BadRequestObjectResult(new { error = "Dungeon entrance is sealed" });
                }

                var step = EncounterRunner.Begin(session, start);
                await store.Save(player);
                response = BuildEncounterResponse(session, step.Encounter, step.GatedChoices);
                break;
            }

            case "start_encounter":
            {
                if (session.Mode != SessionMode.Exploring)
                    return new BadRequestObjectResult(new { error = "Cannot start encounter while not exploring" });

                if (string.IsNullOrEmpty(actionReq.EncounterId))
                    return new BadRequestObjectResult(new { error = "encounterId required" });

                var available = EncounterSelection.GetAvailableAtPoi(session, session.CurrentNode);
                var encTarget = available.FirstOrDefault(e => e.Id.Equals(actionReq.EncounterId, StringComparison.OrdinalIgnoreCase));

                var curNode = session.CurrentNode;
                if (encTarget == null && curNode.Poi?.Kind == PoiKind.Settlement && curNode.Poi.SettlementId != null
                    && session.Player.Settlements.TryGetValue(curNode.Poi.SettlementId, out var sState)
                    && sState.StoryletOffers.Contains(actionReq.EncounterId))
                {
                    encTarget = session.Bundle.GetById(actionReq.EncounterId);
                }

                if (encTarget == null)
                    return new BadRequestObjectResult(new { error = $"Encounter '{actionReq.EncounterId}' not available at this location" });

                if (curNode.Poi?.Kind == PoiKind.Settlement && curNode.Poi.SettlementId != null
                    && session.Player.Settlements.TryGetValue(curNode.Poi.SettlementId, out var sState2))
                {
                    sState2.StoryletOffers.Remove(actionReq.EncounterId);
                }

                var step = EncounterRunner.Begin(session, encTarget);
                await store.Save(player);
                response = BuildEncounterResponse(session, step.Encounter, step.GatedChoices);
                break;
            }

            case "leave_dungeon":
            {
                if (player.CurrentDungeonId == null)
                    return new BadRequestObjectResult(new { error = "Not in a dungeon" });

                player.CurrentDungeonId = null;
                await store.Save(player);
                response = BuildExploringResponse(session);
                break;
            }

            case "end_encounter":
            case "end_dungeon":
            {
                EncounterRunner.EndEncounter(session);
                await store.Save(player);

                if (player.PendingEndOfDay && !data.NoCamp)
                {
                    session.Mode = SessionMode.Camp;
                    response = BuildCampResponse(session, BuildCampThreats(session));
                }
                else
                {
                    if (data.NoCamp) player.PendingEndOfDay = false;
                    response = BuildExploringResponse(session);
                }
                break;
            }

            case "camp_resolve":
            {
                if (session.Mode != SessionMode.Camp)
                    return new BadRequestObjectResult(new { error = "Not in camp mode" });

                var node = session.CurrentNode;
                var campBiome = node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
                var campTier = node.Region?.Tier ?? 1;
                var campTerrain = node.Region?.Terrain ?? Terrain.Plains;
                var startCity = data.Map.StartingCity;

                var healthBefore = player.Health;
                var conditionsBefore = player.ActiveConditions
                    .Where(kv => data.Balance.Conditions.TryGetValue(kv.Key, out var def)
                                 && def.Severity == ConditionSeverity.Severe)
                    .ToDictionary(kv => kv.Key, kv => kv.Value);

                var campEvents = EndOfDay.Resolve(
                    player, campBiome, campTier,
                    data.Balance, session.Rng,
                    startX: startCity?.X ?? 0, startY: startCity?.Y ?? 0,
                    createFood: (type, rng) =>
                    {
                        var (name, desc) = FlavorText.FoodName(type, campTerrain, foraged: true, rng);
                        return new ItemInstance($"food_{type.ToString().ToLowerInvariant()}", name)
                            { FoodType = type, Description = desc };
                    });

                var rescued = campEvents.OfType<EndOfDayEvent.PlayerRescued>().FirstOrDefault();

                var hasSevere = player.ActiveConditions.Keys
                    .Any(id => data.Balance.Conditions.TryGetValue(id, out var def)
                               && def.Severity == ConditionSeverity.Severe);

                var conditionRows = BuildConditionRows(conditionsBefore, campEvents);
                var campInfo = BuildCampThreats(session);
                campInfo = new CampInfo
                {
                    HasSevereCondition = hasSevere,
                    HealthBefore = healthBefore,
                    HealthAfter = player.Health,
                    ConditionRows = conditionRows,
                    Threats = campInfo.Threats,
                    Events = FormatCampEvents(campEvents),
                };

                if (rescued != null)
                {
                    session.Mode = SessionMode.Exploring;
                    await store.Save(player);
                    return new OkObjectResult(new GameResponse
                    {
                        Mode = "rescued",
                        Status = BuildStatus(player),
                        Camp = campInfo,
                        Rescue = new RescueInfo
                        {
                            LostItems = rescued.LostItems,
                            GoldLost = rescued.GoldLost,
                        },
                        Node = BuildNodeInfo(session.CurrentNode, player),
                        Exits = BuildExits(session),
                        Inventory = BuildInventory(player),
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
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                if (actionReq.Order == null)
                    return new BadRequestObjectResult(new { error = "order required" });

                var settlementId = sNode.Poi.SettlementId;
                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
                    return new BadRequestObjectResult(new { error = "Settlement not initialized" });

                var order = new MarketOrder(
                    actionReq.Order.Buys.Select(b => new BuyLine(b.ItemId, b.Quantity)).ToList(),
                    actionReq.Order.Sells?.Select(s => new SellLine(s.ItemDefId)).ToList() ?? []);

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
                var marketResult = Market.ApplyOrder(player, order, settlementState, data.Balance, mRng, CreateFood);
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
                        Success = marketResult.Success,
                        Results = marketResult.Results.Select(r => new MarketLineResultInfo
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
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                if (string.IsNullOrEmpty(actionReq.OfferId))
                    return new BadRequestObjectResult(new { error = "offerId required" });

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(hNode.Poi.SettlementId, out var haulState))
                    return new BadRequestObjectResult(new { error = "Settlement not initialized" });

                var claimResult = Market.ClaimHaul(player, actionReq.OfferId, haulState);
                if (!claimResult.Success)
                    return new BadRequestObjectResult(new { error = claimResult.Message });

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

            case "abandon_haul":
            {
                if (string.IsNullOrEmpty(actionReq.OfferId))
                    return new BadRequestObjectResult(new { error = "offerId required" });

                var haulIdx = player.Pack.FindIndex(i => i.HaulOfferId == actionReq.OfferId);
                if (haulIdx < 0)
                    return new BadRequestObjectResult(new { error = "Haul not found in pack" });

                player.Pack.RemoveAt(haulIdx);
                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "bank_deposit":
            {
                var bNode = session.CurrentNode;
                if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                if (string.IsNullOrEmpty(actionReq.ItemId) || string.IsNullOrEmpty(actionReq.Source))
                    return new BadRequestObjectResult(new { error = "itemId and source required" });

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bDepositState))
                    return new BadRequestObjectResult(new { error = "Settlement not initialized" });

                var depositError = Bank.Deposit(player, actionReq.ItemId, actionReq.Source, bDepositState, data.Balance);
                if (depositError != null)
                    return new BadRequestObjectResult(new { error = depositError });

                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "bank_withdraw":
            {
                var bNode = session.CurrentNode;
                if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                    return new BadRequestObjectResult(new { error = "Not at a settlement" });

                if (actionReq.BankIndex == null)
                    return new BadRequestObjectResult(new { error = "bankIndex required" });

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bWithdrawState))
                    return new BadRequestObjectResult(new { error = "Settlement not initialized" });

                var withdrawError = Bank.Withdraw(player, actionReq.BankIndex.Value, bWithdrawState, data.Balance);
                if (withdrawError != null)
                    return new BadRequestObjectResult(new { error = withdrawError });

                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "equip":
            {
                if (string.IsNullOrEmpty(actionReq.ItemId))
                    return new BadRequestObjectResult(new { error = "ItemId is required" });

                var results = Mechanics.Apply([$"equip {actionReq.ItemId}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return new BadRequestObjectResult(new { error = $"Cannot equip '{actionReq.ItemId}' — not in pack or not equippable" });

                response = BuildInventoryResponse(session, player);
                break;
            }

            case "unequip":
            {
                var slot = actionReq.Slot;
                if (string.IsNullOrEmpty(slot))
                    return new BadRequestObjectResult(new { error = "Slot is required (weapon, armor, boots)" });

                var results = Mechanics.Apply([$"unequip {slot}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return new BadRequestObjectResult(new { error = $"Nothing equipped in slot '{slot}'" });

                response = BuildInventoryResponse(session, player);
                break;
            }

            case "discard":
            {
                if (string.IsNullOrEmpty(actionReq.ItemId))
                    return new BadRequestObjectResult(new { error = "ItemId is required" });

                var results = Mechanics.Apply([$"discard {actionReq.ItemId}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return new BadRequestObjectResult(new { error = $"Item '{actionReq.ItemId}' not found in inventory" });

                response = BuildInventoryResponse(session, player);
                break;
            }

            default:
                return new BadRequestObjectResult(new { error = $"Unknown action: {actionReq.Action}" });
        }

        await store.Save(player);
        return new OkObjectResult(response);
    }

    [Function("GetMarket")]
    public async Task<IActionResult> GetMarket(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/market")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        var node = session.CurrentNode;
        var tier = node.Region?.Tier ?? 1;

        if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
            return new BadRequestObjectResult(new { error = "Not at a settlement" });

        var settlementId = node.Poi.SettlementId;
        SettlementRunner.EnsureSettlement(session);
        await store.Save(player);
        if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
            return new BadRequestObjectResult(new { error = "Settlement not initialized" });

        var stock = Market.GetStock(settlementState, data.Balance).Select(entry => new
        {
            id = entry.Item.Id,
            name = entry.Item.Name,
            type = entry.Item.Type.ToString().ToLowerInvariant(),
            buyPrice = Market.GetBuyFromSettlementPrice(entry.Item.Id, settlementState, data.Balance),
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
            isGeneric = h.IsGeneric,
        }).ToList();

        var sellPrices = new Dictionary<string, int>();
        void AddSellPrice(ItemInstance item)
        {
            if (sellPrices.ContainsKey(item.DefId)) return;
            if (!data.Balance.Items.TryGetValue(item.DefId, out var def)) return;
            if (def.Type == ItemType.Haul) return;
            var price = Market.GetSellPrice(def, data.Balance);
            if (price > 0) sellPrices[item.DefId] = price;
        }
        foreach (var item in player.Pack) AddSellPrice(item);
        foreach (var item in player.Haversack) AddSellPrice(item);
        if (player.Equipment.Weapon != null) AddSellPrice(player.Equipment.Weapon);
        if (player.Equipment.Armor != null) AddSellPrice(player.Equipment.Armor);
        if (player.Equipment.Boots != null) AddSellPrice(player.Equipment.Boots);

        return new OkObjectResult(new { tier, stock, hauls, sellPrices });
    }

    [Function("GetInn")]
    public async Task<IActionResult> GetInn(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/inn")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        var node = session.CurrentNode;

        if (node.Poi?.Kind != PoiKind.Settlement)
            return new BadRequestObjectResult(new { error = "Not at a settlement" });

        var isChapterhouse = node == session.Map.StartingCity;
        var (canFullRecover, disqualifying) = Inn.CanUseInn(player, data.Balance);
        var quote = Inn.GetQuote(player, data.Balance);
        var needsRecovery = player.Health < player.MaxHealth
                         || player.Spirits < player.MaxSpirits
                         || player.ActiveConditions.Count > 0;

        return new OkObjectResult(new
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
    }

    [Function("GetBank")]
    public async Task<IActionResult> GetBank(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/bank")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        var node = session.CurrentNode;

        if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
            return new BadRequestObjectResult(new { error = "Not at a settlement" });

        SettlementRunner.EnsureSettlement(session);
        if (!player.Settlements.TryGetValue(node.Poi.SettlementId, out var settlementState))
            return new BadRequestObjectResult(new { error = "Settlement not initialized" });

        return new OkObjectResult(new
        {
            settlementName = node.Poi.Name,
            items = settlementState.Bank.Select(i => BuildItemInfo(i)).ToList(),
            capacity = data.Balance.Settlements.BankCapacity,
            packFull = player.Pack.Count >= player.PackCapacity,
            haversackFull = player.Haversack.Count >= player.HaversackCapacity,
        });
    }

    [Function("GetNotices")]
    public async Task<IActionResult> GetNotices(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/notices")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        var node = session.CurrentNode;

        if (node.Poi?.Kind != PoiKind.Settlement || node.Poi.SettlementId == null)
            return new BadRequestObjectResult(new { error = "Not at a settlement" });

        SettlementRunner.EnsureSettlement(session);
        var settlement = session.Player.Settlements.GetValueOrDefault(node.Poi.SettlementId);
        var offers = settlement?.StoryletOffers ?? [];

        var encounters = offers
            .Select(eid => session.Bundle.GetById(eid))
            .Where(e => e != null)
            .Select(e => new EncounterSummary { Id = e!.Id, Title = e.Title })
            .ToList();

        return new OkObjectResult(new { encounters });
    }

    [Function("GetDiscoveries")]
    public async Task<IActionResult> GetDiscoveries(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/discoveries")] HttpRequest req,
        string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var discoveries = new List<DiscoveryInfo>();
        foreach (var encoded in player.VisitedNodes)
        {
            var (x, y) = PlayerState.DecodePosition(encoded);
            if (!data.Map.InBounds(x, y)) continue;
            var node = data.Map[x, y];
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

        return new OkObjectResult(discoveries);
    }

    [Function("DebugAddCondition")]
    public async Task<IActionResult> DebugAddCondition(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/debug/add-condition")] HttpRequest req,
        string id)
    {
        var debugReq = await req.ReadFromJsonAsync<DebugConditionRequest>();
        if (debugReq == null) return new BadRequestObjectResult(new { error = "Invalid request body" });

        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var conditionId = debugReq.Condition;
        if (string.IsNullOrWhiteSpace(conditionId))
            return new BadRequestObjectResult(new { error = "Missing condition" });

        if (player.ActiveConditions.ContainsKey(conditionId))
            return new OkObjectResult(new { message = $"Already has {conditionId}" });

        var stacks = data.Balance.Conditions.TryGetValue(conditionId, out var def) ? def.Stacks : 1;
        player.ActiveConditions[conditionId] = stacks;
        await store.Save(player);

        return new OkObjectResult(new { message = $"Added {conditionId} ({stacks} stacks)" });
    }

    // ── Builder helpers ──

    GameSession BuildSession(PlayerState player)
    {
        var rng = new Random(player.Seed + player.VisitedNodes.Count);
        var session = new GameSession(player, data.Map, data.Bundle, data.Balance, rng, data.TacticalBundle);

        if (player.CurrentTacticalId is { } tacId)
        {
            session.Mode = SessionMode.InTactical;
        }
        else if (player.CurrentEncounterId is { } encId)
        {
            var enc = data.Bundle.GetById(encId);
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
        else if (player.PendingEndOfDay && !data.NoCamp)
        {
            session.Mode = SessionMode.Camp;
        }
        else if (player.PendingEndOfDay && data.NoCamp)
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
            var def = data.Balance.Conditions.GetValueOrDefault(kv.Key);
            var flavor = data.Balance.ConditionFlavors.GetValueOrDefault(kv.Key);
            return new ConditionInfo
            {
                Id = kv.Key,
                Name = def?.Name ?? kv.Key,
                Stacks = kv.Value,
                Description = flavor?.Ongoing ?? "",
                Effect = BuildConditionEffect(def),
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

    NodeInfo BuildNodeInfo(Node node, PlayerState p, GameSession? session = null)
    {
        List<string>? services = null;
        if (node.Poi?.Kind == PoiKind.Settlement)
        {
            var isChapterhouse = node == data.Map.StartingCity;
            services = ["market", "bank", isChapterhouse ? "chapterhouse" : "inn"];

            if (session != null && node.Poi.SettlementId != null
                && session.Player.Settlements.TryGetValue(node.Poi.SettlementId, out var settlementInfo)
                && settlementInfo.StoryletOffers.Count > 0)
                services.Add("notices");
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

    DungeonHubInfo? BuildDungeonHubInfo(GameSession session, Node node)
    {
        if (session.Player.CurrentDungeonId == null) return null;

        var available = EncounterSelection.GetAvailableAtPoi(session, node);
        return new DungeonHubInfo
        {
            DungeonId = session.Player.CurrentDungeonId,
            Name = node.Poi?.Name ?? session.Player.CurrentDungeonId,
            Encounters = available.Select(e => new EncounterSummary
            {
                Id = e.Id,
                Title = e.Title,
            }).ToList(),
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
        var def = data.Balance.Items.GetValueOrDefault(i.DefId);
        return new ItemInfo
        {
            DefId = i.DefId,
            Name = i.DisplayName,
            Description = i.Description ?? (def != null ? FormatItemDescription(def) : null),
            Type = def?.Type.ToString().ToLowerInvariant() ?? "",
            Cost = def?.Cost,
            SkillModifiers = def?.SkillModifiers.ToDictionary(kv => kv.Key.ScriptName(), kv => kv.Value) ?? [],
            ResistModifiers = def?.ResistModifiers.ToDictionary(kv => kv.Key, kv => kv.Value) ?? [],
            Cures = def?.Cures.ToList() ?? [],
            IsEquippable = def?.Type is ItemType.Weapon or ItemType.Armor or ItemType.Boots,
            DestinationName = i.DestinationName,
            DestinationHint = i.DestinationX != null && i.DestinationY != null
                ? HaulGeneration.BuildRelativeHint(playerX, playerY, i.DestinationX.Value, i.DestinationY.Value)
                : i.DestinationHint,
            Payout = i.Payout,
            HaulOfferId = i.HaulOfferId,
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

        foreach (var (condId, condDef) in data.Balance.Conditions)
        {
            var resistSkill = condId switch
            {
                "freezing" or "thirsty" or "lost" or "poisoned" => (Skill?)Skill.Bushcraft,
                "injured" => Skill.Combat,
                _ => null,
            };

            var skillBonus = resistSkill != null ? p.Skills.GetValueOrDefault(resistSkill.Value) : 0;
            var gearBonus = SkillChecks.GetResistBonus(condId, p, data.Balance);
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

        foreach (var si in Skills.All)
        {
            if (si.Skill is Skill.Luck or Skill.Mercantile) continue;
            var skillLevel = p.Skills.GetValueOrDefault(si.Skill);
            var itemBonus = SkillChecks.GetItemBonus(si.Skill, p, data.Balance);
            var total = skillLevel + itemBonus;

            encounterChecks.Add(new MechanicLine
            {
                Label = si.DisplayName,
                Value = FormatSkillLevel(total),
                Source = $"{si.DisplayName} + gear",
            });
        }

        var mercantile = p.Skills.GetValueOrDefault(Skill.Mercantile);
        var haulBonus = (int)(mercantile * data.Balance.Trade.MercantileHaulBonusPerPoint * 100);
        other.Add(new MechanicLine
        {
            Label = "Contract bonus",
            Value = $"+{haulBonus}%",
            Source = "Mercantile",
        });

        var luckLevel = p.Skills.GetValueOrDefault(Skill.Luck);
        var luckChances = data.Balance.Character.LuckRerollChance;
        var rerollChance = luckChances[Math.Min(Math.Max(luckLevel, 0), luckChances.Count - 1)];
        other.Add(new MechanicLine
        {
            Label = "Reroll any failure",
            Value = $"{rerollChance}%",
            Source = "Luck",
        });

        var totalForaging = p.Skills.GetValueOrDefault(Skill.Bushcraft)
                          + SkillChecks.GetItemBonus(Skill.Bushcraft, p, data.Balance);
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

    static string ModeString(SessionMode m) => m switch
    {
        SessionMode.Exploring => "exploring",
        SessionMode.InEncounter => "encounter",
        SessionMode.Camp => "camp",
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

    EncounterInfo BuildEncounterInfo(Encounter encounter, List<GatedChoice> gated) => new()
    {
        Id = encounter.Id,
        Category = encounter.Category,
        Vignette = encounter.Vignette,
        Title = encounter.Title,
        Body = encounter.Body,
        Choices = gated.Select(g => new ChoiceInfo
        {
            Index = g.OriginalIndex,
            Label = g.Choice.OptionLink ?? g.Choice.OptionText,
            Preview = g.Choice.OptionPreview,
            Locked = g.Locked,
            Requires = g.Locked ? FormatRequires(g.Choice.Requires) : null,
        }).ToList(),
    };

    static string? FormatRequires(string? requires)
    {
        if (requires == null) return null;
        var parts = ActionVerb.Tokenize(requires);
        return parts[0] switch
        {
            "quality" when parts.Count >= 3 && int.TryParse(parts[2], out var t) && t < 0
                => $"{parts[1]} \u2264 {parts[2]}",
            "quality" when parts.Count >= 3
                => $"{parts[1]} \u2265 {parts[2]}",
            "tag" when parts.Count >= 2 => parts[1],
            "has" when parts.Count >= 2 => parts[1],
            "meets" when parts.Count >= 3 => $"{parts[1]} \u2265 {parts[2]}",
            _ => requires,
        };
    }

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
                MechanicResult.ConditionResisted cr => $"Resisted: {cr.ConditionId} (rolled {cr.Check.Rolled} vs DC {cr.Check.Target})",
                MechanicResult.ConditionRemoved c => $"Condition removed: {c.ConditionId}",
                MechanicResult.TimeAdvanced ta => $"Time: {ta.NewPeriod}, Day {ta.NewDay}",
                MechanicResult.Navigation n => $"Navigate to: {n.EncounterId}",
                MechanicResult.DungeonFinished => "Dungeon completed!",
                MechanicResult.DungeonFled => "Fled the dungeon!",
                _ => r.ToString() ?? "",
            },
            ResistCheck = r switch
            {
                MechanicResult.ConditionResisted cr2 => new ResistCheckInfo
                {
                    ConditionId = cr2.ConditionId,
                    ConditionName = data.Balance.Conditions.GetValueOrDefault(cr2.ConditionId)?.Name ?? cr2.ConditionId,
                    Passed = cr2.Check.Passed,
                    Rolled = cr2.Check.Rolled,
                    Target = cr2.Check.Target,
                    Modifier = cr2.Check.Modifier,
                    RollMode = cr2.Check.RollMode != Dreamlands.Game.RollMode.Normal ? cr2.Check.RollMode.ToString().ToLowerInvariant() : null,
                },
                MechanicResult.ConditionAdded { Check: { } ck } ca => new ResistCheckInfo
                {
                    ConditionId = ca.ConditionId,
                    ConditionName = data.Balance.Conditions.GetValueOrDefault(ca.ConditionId)?.Name ?? ca.ConditionId,
                    Passed = ck.Passed,
                    Rolled = ck.Rolled,
                    Target = ck.Target,
                    Modifier = ck.Modifier,
                    RollMode = ck.RollMode != Dreamlands.Game.RollMode.Normal ? ck.RollMode.ToString().ToLowerInvariant() : null,
                },
                _ => null,
            },
        }).ToList();

    GameResponse BuildExploringResponse(GameSession session, List<DeliveryInfo>? deliveries = null) => new()
    {
        Mode = "exploring",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
        Exits = BuildExits(session),
        Inventory = BuildInventory(session.Player),
        Mechanics = BuildMechanics(session.Player),
        Deliveries = deliveries,
        DungeonHub = BuildDungeonHubInfo(session, session.CurrentNode),
    };

    GameResponse BuildEncounterResponse(GameSession session, Encounter encounter, List<GatedChoice> gated) => new()
    {
        Mode = "encounter",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player),
        Encounter = BuildEncounterInfo(encounter, gated),
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
            RollMode = ck.RollMode != Dreamlands.Game.RollMode.Normal ? ck.RollMode.ToString().ToLowerInvariant() : null,
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
        var threats = EndOfDay.GetThreats(biome, tier, data.Balance);

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

    List<ConditionRowInfo> BuildConditionRows(
        Dictionary<string, int> conditionsBefore,
        List<EndOfDayEvent> events)
    {
        var cures = new Dictionary<string, EndOfDayEvent.CureApplied>();
        var drains = new Dictionary<string, (int Health, int Spirits)>();

        foreach (var e in events)
        {
            if (e is EndOfDayEvent.CureApplied c)
                cures[c.ConditionId] = c;
            if (e is EndOfDayEvent.ConditionDrain d)
            {
                var prev = drains.GetValueOrDefault(d.ConditionId);
                drains[d.ConditionId] = (prev.Health + d.HealthLost, prev.Spirits + d.SpiritsLost);
            }
        }

        var rows = new List<ConditionRowInfo>();
        foreach (var (conditionId, stacksBefore) in conditionsBefore)
        {
            if (!data.Balance.Conditions.TryGetValue(conditionId, out var def)) continue;

            var cure = cures.GetValueOrDefault(conditionId);
            var drain = drains.GetValueOrDefault(conditionId);

            string? cureItem = null;
            string? cureMessage = null;
            int stacksAfter = stacksBefore;

            if (cure != null)
            {
                var itemDef = data.Balance.Items.GetValueOrDefault(cure.ItemDefId);
                cureItem = itemDef?.Name ?? cure.ItemDefId;
                stacksAfter = cure.Remaining;
                cureMessage = cure.Remaining > 0
                    ? $"Used {cureItem}, removed {cure.StacksRemoved} stack"
                    : $"Used {cureItem}, cured!";
            }
            else
            {
                var cureItemDef = data.Balance.Items.Values
                    .FirstOrDefault(i => i.Cures.Contains(conditionId));
                cureMessage = cureItemDef != null
                    ? $"You have no {cureItemDef.Name.ToLowerInvariant()}"
                    : "No cure available";
            }

            rows.Add(new ConditionRowInfo
            {
                ConditionId = conditionId,
                Name = def.Name,
                Stacks = stacksBefore,
                CureItem = cureItem,
                CureMessage = cureMessage,
                StacksAfter = stacksAfter,
                HealthLost = drain.Health,
                SpiritsLost = drain.Spirits,
            });
        }

        return rows;
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
                EndOfDayEvent.Starving => "No food!",
                EndOfDayEvent.ResistPassed r => $"Resisted {r.ConditionId} (rolled {r.Check.Rolled} vs DC {r.Check.Target})",
                EndOfDayEvent.ResistFailed r => $"Failed to resist {r.ConditionId} (rolled {r.Check.Rolled} vs DC {r.Check.Target})",
                EndOfDayEvent.CureApplied c => c.Remaining > 0
                    ? $"{c.ItemDefId} reduced {c.ConditionId} by {c.StacksRemoved} ({c.Remaining} remaining)"
                    : $"{c.ItemDefId} cured {c.ConditionId}!",
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

    static string BuildConditionEffect(ConditionDef? def)
    {
        if (def is null) return "";
        var parts = new List<string>();
        if (def.Severity == ConditionSeverity.Severe)
            parts.Add("Drains health each night");
        if (def.SpiritsDrain is { })
            parts.Add("Drains spirits each night");
        if (def.SpecialEffect is { } se)
            parts.Add(se);
        if (def.ClearedOnSettlement)
            parts.Add("Cleared at settlements");
        if (def.SpecialCure is { } sc)
            parts.Add($"Cure: {sc}");
        return string.Join(". ", parts) + (parts.Count > 0 ? "." : "");
    }

    static string FormatSkillLevel(int level) => level >= 0 ? $"+{level}" : $"{level}";

    static string FormatItemDescription(ItemDef item)
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

    // ── Tactical helpers ─────────────────────────────────────

    static readonly JsonSerializerOptions TacJsonOpts = new() { PropertyNameCaseInsensitive = true };

    GameResponse BeginTacticalEncounter(GameSession session, PlayerState player, TacticalEncounter tacEnc)
    {
        var tacState = new TacticalState();
        var step = TacticalRunner.Begin(session, tacEnc, tacState);
        player.CurrentTacticalId = tacEnc.Id;
        player.CurrentEncounterId = null;
        session.Mode = SessionMode.InTactical;
        SerializeTacticalState(player, tacState);
        return BuildTacticalResponse(session, tacEnc, step, tacState);
    }

    GameResponse BuildTacticalResponse(GameSession session, TacticalEncounter tacEnc, TacticalStep step, TacticalState tacState)
    {
        var info = new TacticalInfo
        {
            Title = tacEnc.Title,
            Body = tacEnc.Body,
            Variant = tacEnc.Variant.ToString().ToLowerInvariant(),
        };

        switch (step)
        {
            case TacticalStep.ChooseApproach ca:
                info = info with
                {
                    Phase = "approach",
                    Approaches = ca.Approaches.Select(a => new TacticalApproachInfo
                    {
                        Kind = a.Kind.ToString().ToLowerInvariant(),
                    }).ToList(),
                };
                break;

            case TacticalStep.ShowTurn st:
                info = info with
                {
                    Phase = "turn",
                    Turn = new TacticalTurnInfo
                    {
                        Turn = st.Data.Turn,
                        Resistance = st.Data.Resistance,
                        ResistanceMax = st.Data.ResistanceMax,
                        Momentum = st.Data.Momentum,
                        Spirits = st.Data.PlayerSpirits,
                        Timers = st.Data.Timers.Select(t => new TacticalTimerInfo
                        {
                            Name = t.Name,
                            CounterName = t.CounterName,
                            Effect = t.Effect.ToString().ToLowerInvariant(),
                            Amount = t.Amount,
                            Countdown = t.Countdown,
                            Current = t.Current,
                            Stopped = t.Stopped,
                            ConditionId = t.ConditionId,
                        }).ToList(),
                        Openings = st.Data.Openings.Select(BuildOpeningInfo).ToList(),
                        Queue = st.Data.Queue?.Select(BuildOpeningInfo).ToList(),
                    },
                };
                break;

            case TacticalStep.Finished fin:
                info = info with
                {
                    Phase = "finished",
                    FinishReason = fin.Reason.ToString().ToLowerInvariant(),
                    FailureText = tacEnc.Failure?.Text,
                    SuccessText = tacEnc.Success?.Text,
                    FailureMechanics = fin.FailureResults != null ? BuildMechanicResults(fin.FailureResults) : null,
                    SuccessMechanics = fin.SuccessResults != null ? BuildMechanicResults(fin.SuccessResults) : null,
                    ConditionResults = fin.ConditionResults != null ? BuildMechanicResults(fin.ConditionResults) : null,
                };
                break;
        }

        return new GameResponse
        {
            Mode = "tactical",
            Status = BuildStatus(session.Player),
            Tactical = info,
            Node = BuildNodeInfo(session.CurrentNode, session.Player),
            Inventory = BuildInventory(session.Player),
            Mechanics = BuildMechanics(session.Player),
        };
    }

    static TacticalOpeningInfo BuildOpeningInfo(OpeningSnapshot o) => new()
    {
        Name = o.Name,
        CostKind = ToSnakeCase(o.CostKind.ToString()),
        CostAmount = o.CostAmount,
        EffectKind = ToSnakeCase(o.EffectKind.ToString()),
        EffectAmount = o.EffectAmount,
        StopsTimerIndex = o.StopsTimerIndex,
    };

    static string ToSnakeCase(string pascalCase) =>
        string.Concat(pascalCase.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    static void SerializeTacticalState(PlayerState player, TacticalState state) =>
        player.TacticalStateJson = JsonSerializer.Serialize(state, TacJsonOpts);

    static TacticalState? DeserializeTacticalState(PlayerState player) =>
        player.TacticalStateJson != null
            ? JsonSerializer.Deserialize<TacticalState>(player.TacticalStateJson, TacJsonOpts)
            : null;
}
