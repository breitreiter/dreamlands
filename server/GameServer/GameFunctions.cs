using System.Diagnostics;
using System.Text.Json;
using Dreamlands.Encounter;
using Dreamlands.Flavor;
using Dreamlands.Game;
using Dreamlands.Map;
using Dreamlands.Orchestration;
using Dreamlands.Rules;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
        Telemetry.RecordGameCreated();

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

        // Initialise the starting city as a settlement so the new character gets
        // their initial ration refill and any other settlement-entry side effects.
        SettlementRunner.EnsureSettlement(session);

        player.NextEncounterMove = session.Rng.Next(
            data.Balance.Character.EncounterCadenceMin,
            data.Balance.Character.EncounterCadenceMax + 1);

        var introEnc = data.Bundle.GetById("intro/00_Intro");
        if (introEnc != null)
        {
            var step = BeginEncounter(session, introEnc);
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
            return new OkObjectResult(BuildCampResponse(session, new CampInfo()));

        if (player.PendingEndOfDay && data.NoCamp)
            player.PendingEndOfDay = false;

        if (session.Mode == SessionMode.InCombat && player.ActiveCombat is { } combat
            && data.CombatBundle?.GetById(combat.EncounterId) is { } combatEnc)
        {
            if (combat.Resolved)
            {
                // Reload during the post-defeat coda. Reconstruct the Outcome event so the
                // OutcomeCard still has its coda text. Mechanics already ran in the original
                // Step (in CombatOrchestrator.Finalize), so don't re-apply them.
                var codaText = combat.PlayerWon ? combatEnc.WinText
                            : combat.PlayerLost ? combatEnc.LoseText
                            : "";
                var coda = new Dreamlands.Combat.CombatEvent.Outcome(
                    combat.PlayerWon, combat.PlayerLost, combat.PlayerFled, combat.MonsterFled,
                    combat.Turn, codaText, Array.Empty<string>());
                var resolvedTurn = new Dreamlands.Orchestration.CombatOrchestrator.CombatTurn(
                    combat.EncounterId,
                    new Dreamlands.Combat.CombatEvent[] { coda },
                    Array.Empty<MechanicResult>(),
                    Resolved: true,
                    PlayerDied: combat.PlayerLost);
                return new OkObjectResult(BuildCombatResponse(session, resolvedTurn));
            }

            // Resume mid-fight: render the current state with empty events (no new turn happened).
            var resumeTurn = new Dreamlands.Orchestration.CombatOrchestrator.CombatTurn(
                combat.EncounterId,
                Array.Empty<Dreamlands.Combat.CombatEvent>(),
                Array.Empty<MechanicResult>(),
                Resolved: false,
                PlayerDied: false);
            return new OkObjectResult(BuildCombatResponse(session, resumeTurn));
        }

        // A fight outro queued a +chain: launch the .enc now that the coda is dismissed
        // (the win/flee coda's Continue triggers a state refetch, which lands here).
        if (player.PendingEncounterChain is { } chainId && player.ActiveCombat == null)
        {
            player.PendingEncounterChain = null;
            if (session.Bundle.GetById(chainId) is { } chained)
            {
                var chainStep = BeginEncounter(session, chained);
                await store.Save(player);
                return new OkObjectResult(BuildEncounterResponse(session, chainStep.Encounter, chainStep.GatedChoices));
            }
            await store.Save(player); // target gone from the bundle — drop the chain
        }

        // Closed-tab resume for tableau: PendingLevels may be set without an active encounter
        // (e.g., if the encounter session was already closed when +add_level fired).
        if (player.PendingLevels > 0)
        {
            var available = EncounterRunner.GetAvailableSlots(player);
            var resumeTableau = new EncounterStep.AwaitTableauPick(available, player.PendingLevels, null);
            return new OkObjectResult(BuildTableauPromptResponse(session, resumeTableau));
        }

        if (session.CurrentEncounter is { } enc)
        {
            // If there's an active picker check, resume the approach prompt screen
            if (player.ActivePickerCheck is { } resumePicker
                && Dreamlands.Rules.ApproachRoster.GetApproaches(resumePicker.Skill) is { } resumeApproaches)
            {
                var resumeStep = new EncounterStep.AwaitApproach(
                    enc,
                    resumePicker.ChoiceIndex,
                    resumePicker.Skill,
                    resumePicker.CorrectId,
                    resumePicker.WrongId,
                    resumePicker.Preamble,
                    resumeApproaches);
                return new OkObjectResult(BuildApproachPromptResponse(session, resumeStep));
            }

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

        // The generic HTTP layer only sees POST /api/game/{id}/action, which is every
        // verb in the game funnelled through one route. This span is what makes "which
        // action is slow" answerable — and in the isolated worker it is the only
        // server-side span we get at all (see plans/otel_appsignal.md §2a).
        var verb = actionReq.Action ?? "none";
        // The verb goes in the span NAME so a trace list is readable at a glance, but
        // only if it is one we recognise: the name is a grouping key, and anyone can
        // POST an arbitrary action string. Unknown verbs stay under the bare name and
        // are counted as unknown_action anyway.
        using var activity = Telemetry.Source.StartActivity(
            KnownActions.Contains(verb) ? $"game.action {verb}" : "game.action");
        activity?.SetTag("dreamlands.action", verb);
        activity?.SetTag("dreamlands.game_id", id);

        var started = Stopwatch.GetTimestamp();
        var result = await RunGameAction(actionReq, id);
        Telemetry.RecordAction(verb, OutcomeOf(result),
            Stopwatch.GetElapsedTime(started).TotalSeconds);

        if (result is IStatusCodeActionResult { StatusCode: >= 400 } failed)
            activity?.SetStatus(ActivityStatusCode.Error, $"HTTP {failed.StatusCode}");

        return result;
    }

    /// <summary>
    /// The verbs RunGameAction's switch handles. Only used to bound span names — if
    /// this drifts from the switch the span just falls back to the bare name, which is
    /// a harmless degradation rather than a bug.
    /// </summary>
    private static readonly HashSet<string> KnownActions =
    [
        "inn_book", "move", "travel", "choose", "pick_approach", "pick_reward",
        "enter_dungeon", "start_encounter", "end_encounter", "end_dungeon",
        "camp_resolve", "market_order", "restock_rations", "claim_haul",
        "abandon_haul", "bank_deposit", "bank_withdraw", "equip", "unequip", "discard",
    ];

    /// <summary>
    /// Maps the handler's result to a bounded outcome vocabulary. Reading it off the
    /// status code keeps every existing return site untouched — no sweep required.
    /// </summary>
    private static string OutcomeOf(IActionResult result) => result switch
    {
        OkObjectResult => "ok",
        NotFoundObjectResult => "not_found",
        BadRequestObjectResult => "rejected",
        IStatusCodeActionResult { StatusCode: >= 500 } => "error",
        _ => "other",
    };

    /// <summary>
    /// A 400 with a stable reason code beside the human message. The message is
    /// unchanged on the wire — several interpolate user input, so only the code is
    /// safe to use as a metric dimension.
    /// </summary>
    private static IActionResult Reject(string reasonCode, string message)
    {
        Telemetry.RecordRejection(reasonCode);
        return new BadRequestObjectResult(new { error = message });
    }

    private async Task<IActionResult> RunGameAction(ActionRequest actionReq, string id)
    {
        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);

        // Activity.Current is the game.action span started by the caller; null when
        // nothing is listening, which is the no-telemetry case.
        Activity.Current?.SetTag("dreamlands.mode", session.Mode.ToString());
        Activity.Current?.SetTag("dreamlands.in_dungeon", player.CurrentDungeonId != null);

        GameResponse response;

        switch (actionReq.Action)
        {
            case "inn_book":
            {
                if (session.Mode != SessionMode.Exploring)
                    return Reject("not_exploring_rest", "Cannot rest while not exploring");

                var innNode = session.CurrentNode;
                if (innNode.Poi?.Kind != PoiKind.Settlement)
                    return Reject("not_at_settlement", "Not at a settlement");

                var serviceId = actionReq.InnService ?? Inn.BedServiceId;
                var isChapterhouse = innNode == session.Map.StartingCity;
                var bookResult = Inn.BookService(player, data.Balance, serviceId, chapterhouse: isChapterhouse);
                if (!bookResult.Success)
                    return Reject("inn_booking_refused", bookResult.Reason);

                await store.Save(player);

                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(innNode, player, session),
                    Exits = BuildExits(session),
                    InnRecovery = new InnRecoveryInfo
                    {
                        NightsStayed = 1,
                        GoldSpent = bookResult.GoldSpent,
                        HealthRecovered = 0,
                        SpiritsRecovered = bookResult.SpiritsRestored,
                        ConditionsCleared = bookResult.ConditionsCleared
                            .Select(id => data.Balance.Conditions.TryGetValue(id, out var def) ? def.Name : id)
                            .ToList(),
                        MedicinesApplied = bookResult.MedicinesApplied,
                    },
                    Inventory = BuildInventory(player),
                    Mechanics = BuildMechanics(player),
                };
                break;
            }

            case "move":
            {
                if (session.Mode != SessionMode.Exploring)
                    return Reject("not_exploring_move", "Cannot move while not exploring");

                if (player.CurrentDungeonId != null)
                    return Reject("move_in_dungeon", "Cannot move while in a dungeon — leave the dungeon first");

                if (!Enum.TryParse<Direction>(actionReq.Direction, true, out var dir))
                    return Reject("invalid_direction", $"Invalid direction: {actionReq.Direction}");

                var target = Movement.TryMove(session, dir);
                if (target == null)
                    return Reject("no_exit", $"No exit {actionReq.Direction}");

                Movement.Execute(session, dir);

                // Travails: every non-settlement step accrues hazard exposure
                if (session.CurrentNode.Poi?.Kind != PoiKind.Settlement)
                    Travails.AccrueStep(player, BiomeOf(session.CurrentNode), data.Balance);

                List<DeliveryInfo>? deliveries = null;
                ArrivalInfo? moveArrival = null;
                if (session.CurrentNode.Poi?.Kind == PoiKind.Settlement)
                {
                    SettlementRunner.EnsureSettlement(session);

                    // Manual-move journeys flush their travails ledger on settlement arrival
                    if (BuildTravailLines(player) is { } travailLines)
                        moveArrival = new ArrivalInfo
                        {
                            SettlementName = session.CurrentNode.Poi.Name
                                ?? session.CurrentNode.Poi.SettlementId ?? "Settlement",
                            Travails = travailLines,
                        };

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
                        var pick = EncounterSelection.PickOverworld(session, node);
                        if (pick?.Enc is { } enc)
                        {
                            var step = BeginEncounter(session, enc);
                            await store.Save(player);
                            return new OkObjectResult(BuildEncounterResponse(session, step.Encounter, step.GatedChoices));
                        }
                        if (pick?.Fight is { } fight)
                        {
                            var turn = BeginCombat(session, fight.Id);
                            await store.Save(player);
                            return new OkObjectResult(BuildCombatResponse(session, turn));
                        }
                    }
                }

                await store.Save(player);
                if (player.PendingEndOfDay && !data.NoCamp)
                {
                    session.Mode = SessionMode.Camp;
                    response = BuildCampResponse(session, new CampInfo(), deliveries);
                }
                else
                {
                    if (data.NoCamp) player.PendingEndOfDay = false;
                    response = BuildExploringResponse(session, deliveries, moveArrival);
                }
                break;
            }

            case "travel":
            {
                if (session.Mode != SessionMode.Exploring)
                    return Reject("not_exploring_travel", "Cannot travel while not exploring");
                if (player.CurrentDungeonId != null)
                    return Reject("travel_in_dungeon", "Cannot travel while in a dungeon");

                if (actionReq.Path is not { Count: >= 2 } proposedPath)
                    return Reject("path_too_short", "Path is required (at least 2 points)");

                // Validate start matches current position
                if (proposedPath[0].X != player.X || proposedPath[0].Y != player.Y)
                    return Reject("path_bad_origin", "Path must start at current position");

                var stepsCompleted = 0;
                string stopReason = "arrived";
                List<DeliveryInfo> allDeliveries = [];
                Dictionary<string, (int Health, int Spirits)> journeyLosses = new();
                var journeyDayBefore = player.Day;

                for (var i = 1; i < proposedPath.Count; i++)
                {
                    var from = proposedPath[i - 1];
                    var to = proposedPath[i];
                    var dx = to.X - from.X;
                    var dy = to.Y - from.Y;

                    // Validate each step is a single cardinal move
                    if (Math.Abs(dx) + Math.Abs(dy) != 1)
                        return Reject("path_step_not_adjacent", $"Invalid step at index {i}: not adjacent");

                    var stepDir = dx == 1 ? Direction.East
                        : dx == -1 ? Direction.West
                        : dy == 1 ? Direction.South
                        : Direction.North;

                    // Validate the move is legal (not into water, not out of bounds)
                    if (Movement.TryMove(session, stepDir) == null)
                        return Reject("path_step_blocked", $"Blocked step at index {i}");

                    Movement.Execute(session, stepDir);
                    stepsCompleted = i;

                    // Travails: every non-settlement step accrues hazard exposure
                    if (session.CurrentNode.Poi?.Kind != PoiKind.Settlement)
                        Travails.AccrueStep(player, BiomeOf(session.CurrentNode), data.Balance);

                    // Settlement arrival logic (same as move)
                    if (session.CurrentNode.Poi?.Kind == PoiKind.Settlement)
                    {
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
                        // If the player is carrying a serious condition into the night,
                        // interrupt the journey and surface the camp/crisis screen instead
                        // of silently auto-resolving. The player needs the chance to see
                        // their situation (and possibly alter plans), and any death/rescue
                        // must go through the regular camp_resolve → rescued flow.
                        var hasSevereBefore = player.ActiveConditions
                            .Any(id => data.Balance.Conditions.TryGetValue(id, out var def)
                                       && def.Severity == ConditionSeverity.Severe);

                        if (hasSevereBefore)
                        {
                            session.Mode = SessionMode.Camp;
                            var campTravails = BuildTravailLines(player); // flush before save
                            await store.Save(player);
                            return new OkObjectResult(new GameResponse
                            {
                                Mode = "camp",
                                Status = BuildStatus(player),
                                Node = BuildNodeInfo(session.CurrentNode, player, session),
                                Camp = new CampInfo(),
                                Inventory = BuildInventory(player),
                                Mechanics = BuildMechanics(player),
                                Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                                Travel = new TravelInfo
                                {
                                    Path = proposedPath,
                                    StepsCompleted = stepsCompleted,
                                    StopReason = "camp",
                                    Travails = campTravails,
                                },
                            });
                        }

                        // Travails: a night camped on the road accrues fatigue (settlement nights don't)
                        if (session.CurrentNode.Poi?.Kind != PoiKind.Settlement)
                            Travails.AccrueNight(player, data.Balance);

                        var startCity = data.Map.StartingCity;
                        var campEvents = EndOfDay.Resolve(
                            player, data.Balance,
                            startX: startCity?.X ?? 0, startY: startCity?.Y ?? 0);

                        foreach (var drain in campEvents.OfType<EndOfDayEvent.ConditionDrain>())
                        {
                            var (h, s) = journeyLosses.GetValueOrDefault(drain.ConditionId);
                            journeyLosses[drain.ConditionId] = (h + drain.HealthLost, s + drain.SpiritsLost);
                        }

                        player.PendingEndOfDay = false;

                        // Safety net: the severe-condition HP drain can take the last HP of a
                        // low-HP player and trigger a rescue. Surface the proper rescue screen
                        // with camp details rather than silently teleporting.
                        var rescued = campEvents.OfType<EndOfDayEvent.PlayerRescued>().FirstOrDefault();
                        if (rescued != null)
                        {
                            session.Mode = SessionMode.Exploring;
                            var rescueTravails = BuildTravailLines(player); // flush before save
                            await store.Save(player);
                            return new OkObjectResult(new GameResponse
                            {
                                Mode = "rescued",
                                Status = BuildStatus(player),
                                Camp = new CampInfo
                                {
                                    HasSevereCondition = false,
                                    HealthBefore = 0,
                                    HealthAfter = player.Health,
                                    ConditionRows = [],
                                    Events = FormatCampEvents(campEvents),
                                },
                                Rescue = new RescueInfo
                                {
                                    LostItems = rescued.LostItems,
                                    GoldLost = rescued.GoldLost,
                                },
                                Node = BuildNodeInfo(session.CurrentNode, player, session),
                                Exits = BuildExits(session),
                                Inventory = BuildInventory(player),
                                Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                                Travel = new TravelInfo
                                {
                                    Path = proposedPath,
                                    StepsCompleted = stepsCompleted,
                                    StopReason = "rescued",
                                    Travails = rescueTravails,
                                },
                            });
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
                            var pick = EncounterSelection.PickOverworld(session, encNode);
                            if (pick?.Enc is { } enc)
                            {
                                var step = BeginEncounter(session, enc);
                                var encTravails = BuildTravailLines(player); // flush before save
                                await store.Save(player);
                                return new OkObjectResult(new GameResponse
                                {
                                    Mode = "encounter",
                                    Status = BuildStatus(player),
                                    Node = BuildNodeInfo(session.CurrentNode, player, session),
                                    Encounter = BuildEncounterInfo(step.Encounter, step.GatedChoices),
                                    Inventory = BuildInventory(player),
                                    Mechanics = BuildMechanics(player),
                                    Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                                    Travel = new TravelInfo
                                    {
                                        Path = proposedPath,
                                        StepsCompleted = stepsCompleted,
                                        StopReason = "encounter",
                                        Travails = encTravails,
                                    },
                                });
                            }
                            if (pick?.Fight is { } fight)
                            {
                                var turn = BeginCombat(session, fight.Id);
                                var fightTravails = BuildTravailLines(player); // flush before save
                                await store.Save(player);
                                var info = BuildCombatInfo(session, turn);
                                return new OkObjectResult(new GameResponse
                                {
                                    Mode = info.Resolved ? "combat_resolved" : "combat",
                                    Status = BuildStatus(player),
                                    Node = BuildNodeInfo(session.CurrentNode, player, session),
                                    Combat = info,
                                    Inventory = BuildInventory(player),
                                    Mechanics = BuildMechanics(player),
                                    Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                                    Travel = new TravelInfo
                                    {
                                        Path = proposedPath,
                                        StepsCompleted = stepsCompleted,
                                        StopReason = "combat",
                                        Travails = fightTravails,
                                    },
                                });
                            }
                        }
                    }
                }

                // Every journey flushes its travails at the tail end — settlement
                // arrivals fold them into ArrivalInfo, wilderness ends carry them
                // on TravelInfo. Flush before save so the cleared ledger persists.
                var finalTravails = BuildTravailLines(player);
                await store.Save(player);

                ArrivalInfo? arrival = null;
                var finalNode = session.CurrentNode;
                if (finalNode.Poi?.Kind == PoiKind.Settlement)
                {
                    var losses = journeyLosses
                        .Where(kv => kv.Value.Health > 0 || kv.Value.Spirits > 0)
                        .Select(kv => new ArrivalLossInfo
                        {
                            Cause = ResolveCauseName(kv.Key, data.Balance),
                            Health = kv.Value.Health,
                            Spirits = kv.Value.Spirits,
                        })
                        .OrderByDescending(l => l.Health + l.Spirits)
                        .ToList();

                    if (losses.Count > 0 || finalTravails != null)
                    {
                        arrival = new ArrivalInfo
                        {
                            SettlementName = finalNode.Poi.Name ?? finalNode.Poi.SettlementId ?? "Settlement",
                            DaysElapsed = player.Day - journeyDayBefore,
                            Losses = losses,
                            Travails = finalTravails ?? [],
                        };
                        finalTravails = null; // shown on arrival, not duplicated on TravelInfo
                    }
                }

                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(session.CurrentNode, player, session),
                    Exits = BuildExits(session),
                    Inventory = BuildInventory(player),
                    Mechanics = BuildMechanics(player),
                    Deliveries = allDeliveries.Count > 0 ? allDeliveries : null,
                    Arrival = arrival,
                    Travel = new TravelInfo
                    {
                        Path = proposedPath,
                        StepsCompleted = stepsCompleted,
                        StopReason = stopReason,
                        Travails = finalTravails,
                    },
                };
                break;
            }

            case "choose":
            {
                if (session.CurrentEncounter == null)
                    return Reject("no_active_encounter", "No active encounter");

                var allChoices = session.CurrentEncounter.Choices;
                var idx = actionReq.ChoiceIndex ?? -1;
                if (idx < 0 || idx >= allChoices.Count)
                    return Reject("invalid_choice_index", $"Invalid choice index: {idx}");

                var chosen = allChoices[idx];
                if (chosen.Requires != null && !Conditions.Evaluate(chosen.Requires, player, data.Balance, Random.Shared))
                    return Reject("choice_locked", "Choice is locked");
                var result = EncounterRunner.Choose(session, chosen);

                switch (result)
                {
                    case EncounterStep.ShowOutcome outcome:
                        response = BuildOutcomeResponse(session, outcome);
                        break;

                    case EncounterStep.AwaitApproach awaitApproach:
                        await store.Save(player);
                        return new OkObjectResult(BuildApproachPromptResponse(session, awaitApproach));

                    case EncounterStep.AwaitTableauPick awaitTableau:
                        await store.Save(player);
                        return new OkObjectResult(BuildTableauPromptResponse(session, awaitTableau));

                    case EncounterStep.Finished finished:
                        switch (finished.Reason)
                        {
                            case FinishReason.NavigatedTo:
                                var next = EncounterSelection.ResolveNavigation(session, finished.NavigateToId!, session.CurrentNode);
                                if (next != null)
                                {
                                    var step = BeginEncounter(session, next);
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

                            case FinishReason.CombatStarted:
                            {
                                EncounterRunner.EndEncounter(session);
                                var combatTurn = BeginCombat(session, finished.NavigateToId!);
                                await store.Save(player);
                                return new OkObjectResult(BuildCombatResponse(session, combatTurn));
                            }

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
                                    Node = BuildNodeInfo(session.CurrentNode, player, session),
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
                                    return new OkObjectResult(BuildCampResponse(session, new CampInfo()));
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

            case "pick_approach":
            {
                if (session.CurrentEncounter == null || player.ActivePickerCheck == null)
                    return Reject("no_active_picker", "No active picker check");

                var approachId = actionReq.Approach;
                if (string.IsNullOrEmpty(approachId))
                    return Reject("missing_approach_id", "approach id required");

                // Validate the approach id is in the skill's roster
                var pickerSkill = player.ActivePickerCheck.Skill;
                if (Dreamlands.Rules.ApproachRoster.GetApproach(pickerSkill, approachId) == null)
                    return Reject("invalid_approach", $"Invalid approach '{approachId}' for skill '{pickerSkill}'");

                var pickResult = EncounterRunner.Pick(session, approachId);
                switch (pickResult)
                {
                    case EncounterStep.ShowOutcome pickOutcome:
                        response = BuildOutcomeResponse(session, pickOutcome);
                        break;

                    case EncounterStep.AwaitTableauPick pickTableau:
                        await store.Save(player);
                        return new OkObjectResult(BuildTableauPromptResponse(session, pickTableau));

                    case EncounterStep.Finished pickFinished:
                        switch (pickFinished.Reason)
                        {
                            case FinishReason.NavigatedTo:
                                var pickNav = EncounterSelection.ResolveNavigation(session, pickFinished.NavigateToId!, session.CurrentNode);
                                if (pickNav != null)
                                {
                                    var step = BeginEncounter(session, pickNav);
                                    await store.Save(player);
                                    return new OkObjectResult(new GameResponse
                                    {
                                        Mode = "encounter",
                                        Status = BuildStatus(player),
                                        Encounter = BuildEncounterInfo(step.Encounter, step.GatedChoices),
                                        Outcome = pickFinished.Outcome is { } o ? BuildOutcomeInfo(o) : null,
                                        Inventory = BuildInventory(player),
                                    });
                                }
                                EncounterRunner.EndEncounter(session);
                                await store.Save(player);
                                return new OkObjectResult(BuildExploringResponse(session));

                            case FinishReason.CombatStarted:
                            {
                                EncounterRunner.EndEncounter(session);
                                var combatTurn = BeginCombat(session, pickFinished.NavigateToId!);
                                await store.Save(player);
                                return new OkObjectResult(BuildCombatResponse(session, combatTurn));
                            }

                            case FinishReason.DungeonFinished:
                                player.CurrentDungeonId = null;
                                await store.Save(player);
                                return new OkObjectResult(BuildOutcomeResponse(session, pickFinished.Outcome!, "end_dungeon"));

                            case FinishReason.DungeonFled:
                                player.CurrentDungeonId = null;
                                await store.Save(player);
                                return new OkObjectResult(BuildOutcomeResponse(session, pickFinished.Outcome!, "end_dungeon"));

                            case FinishReason.PlayerDied:
                            {
                                var sc = data.Map.StartingCity;
                                var encRescue = Rescue.Apply(player, sc?.X ?? 0, sc?.Y ?? 0, data.Balance);
                                await store.Save(player);
                                return new OkObjectResult(new GameResponse
                                {
                                    Mode = "rescued",
                                    Status = BuildStatus(player),
                                    Outcome = BuildOutcomeInfo(pickFinished.Outcome!),
                                    Rescue = new RescueInfo
                                    {
                                        LostItems = encRescue.LostItems,
                                        GoldLost = encRescue.GoldLost,
                                    },
                                    Node = BuildNodeInfo(session.CurrentNode, player, session),
                                    Exits = BuildExits(session),
                                    Inventory = BuildInventory(player),
                                });
                            }

                            default:
                                EncounterRunner.EndEncounter(session);
                                await store.Save(player);
                                if (player.PendingEndOfDay && !data.NoCamp)
                                {
                                    session.Mode = SessionMode.Camp;
                                    return new OkObjectResult(BuildCampResponse(session, new CampInfo()));
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

            case "pick_reward":
            {
                var slotId = actionReq.RewardSlotId;
                if (string.IsNullOrEmpty(slotId))
                    return Reject("missing_reward_slot", "rewardSlotId required");

                if (player.PendingLevels <= 0)
                    return Reject("no_pending_level_picks", "No pending level picks");

                var slot = System.Array.Find(Dreamlands.Rules.ArcRewards.All, s => s.Id == slotId);
                if (slot == null)
                    return Reject("unknown_reward_slot", $"Unknown reward slot '{slotId}'");

                var takenCount = player.ArcRewardsTaken.GetValueOrDefault(slotId);
                if (takenCount >= slot.Cap)
                    return Reject("reward_slot_at_cap", $"Slot '{slotId}' is at cap");

                // Reconstruct any pending outcome from CurrentEncounterId for closed-tab resilience
                var pickStep = EncounterRunner.PickReward(session, slotId, null);

                await store.Save(player);

                if (pickStep is EncounterStep.AwaitTableauPick stillPending)
                    return new OkObjectResult(BuildTableauPromptResponse(session, stillPending));

                // All picks consumed — return exploring
                return new OkObjectResult(BuildExploringResponse(session));
            }

            case "enter_dungeon":
            {
                if (session.Mode != SessionMode.Exploring)
                    return Reject("not_exploring_enter_dungeon", "Cannot enter dungeon while not exploring");

                var node = session.CurrentNode;
                if (node.Poi?.Kind != PoiKind.Dungeon || node.Poi.DungeonId == null)
                    return Reject("no_dungeon_here", "No dungeon at current location");

                if (player.CompletedDungeons.Contains(node.Poi.DungeonId))
                    return Reject("dungeon_completed", "Dungeon already completed");

                player.CurrentDungeonId = node.Poi.DungeonId;
                var start = EncounterSelection.GetDungeonStart(session, node);
                if (start == null)
                {
                    player.CurrentDungeonId = null;
                    return Reject("dungeon_sealed", "Dungeon entrance is sealed");
                }

                var step = BeginEncounter(session, start);
                await store.Save(player);
                response = BuildEncounterResponse(session, step.Encounter, step.GatedChoices);
                break;
            }

            case "start_encounter":
            {
                if (session.Mode != SessionMode.Exploring)
                    return Reject("not_exploring_start_encounter", "Cannot start encounter while not exploring");

                if (string.IsNullOrEmpty(actionReq.EncounterId))
                    return Reject("missing_encounter_id", "encounterId required");

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
                    return Reject("encounter_not_available", $"Encounter '{actionReq.EncounterId}' not available at this location");

                if (curNode.Poi?.Kind == PoiKind.Settlement && curNode.Poi.SettlementId != null
                    && session.Player.Settlements.TryGetValue(curNode.Poi.SettlementId, out var sState2))
                {
                    sState2.StoryletOffers.Remove(actionReq.EncounterId);
                }

                var step = BeginEncounter(session, encTarget);
                await store.Save(player);
                response = BuildEncounterResponse(session, step.Encounter, step.GatedChoices);
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
                    response = BuildCampResponse(session, new CampInfo());
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
                    return Reject("not_in_camp", "Not in camp mode");

                var node = session.CurrentNode;
                var startCity = data.Map.StartingCity;

                var healthBefore = player.Health;
                var conditionsBefore = player.ActiveConditions
                    .Where(id => data.Balance.Conditions.TryGetValue(id, out var def)
                                 && def.Severity == ConditionSeverity.Severe)
                    .ToHashSet();

                // Travails: a night camped on the road accrues fatigue (settlement nights don't).
                // Track what this night charged so the camp report can show it.
                var fatigueBefore = player.TravailLedger.GetValueOrDefault("fatigue")?.SpiritsCharged ?? 0;
                if (node.Poi?.Kind != PoiKind.Settlement)
                    Travails.AccrueNight(player, data.Balance);
                var fatigueCharged = (player.TravailLedger.GetValueOrDefault("fatigue")?.SpiritsCharged ?? 0) - fatigueBefore;

                var campEvents = EndOfDay.Resolve(
                    player, data.Balance,
                    startX: startCity?.X ?? 0, startY: startCity?.Y ?? 0);

                var rescued = campEvents.OfType<EndOfDayEvent.PlayerRescued>().FirstOrDefault();

                var hasSevere = player.ActiveConditions
                    .Any(id => data.Balance.Conditions.TryGetValue(id, out var def)
                               && def.Severity == ConditionSeverity.Severe);

                var conditionRows = BuildConditionRows(conditionsBefore, campEvents);
                var eventInfos = FormatCampEvents(campEvents);
                if (fatigueCharged > 0)
                    eventInfos.Add(new CampEventInfo
                    {
                        Type = "Travail",
                        Description = $"The road wears on you: -{fatigueCharged} spirit{(fatigueCharged == 1 ? "" : "s")} to fatigue",
                    });
                var campInfo = new CampInfo
                {
                    HasSevereCondition = hasSevere,
                    HealthBefore = healthBefore,
                    HealthAfter = player.Health,
                    ConditionRows = conditionRows,
                    Events = eventInfos,
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
                        Node = BuildNodeInfo(session.CurrentNode, player, session),
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
                    Node = BuildNodeInfo(node, player, session),
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
                    return Reject("not_at_settlement", "Not at a settlement");

                if (actionReq.Order == null)
                    return Reject("missing_order", "order required");

                var settlementId = sNode.Poi.SettlementId;
                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(settlementId, out var settlementState))
                    return Reject("settlement_not_initialized", "Settlement not initialized");

                var order = new MarketOrder(
                    actionReq.Order.Buys.Select(b => new BuyLine(b.ItemId, b.Quantity)).ToList(),
                    actionReq.Order.Sells?.Select(s => new SellLine(s.ItemDefId)).ToList() ?? []);

                var mRng = new Random();
                var marketResult = Market.ApplyOrder(player, order, settlementState, data.Balance, mRng);
                await store.Save(player);

                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(sNode, player, session),
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

            case "restock_rations":
            {
                var rNode = session.CurrentNode;
                if (rNode.Poi?.Kind != PoiKind.Settlement)
                    return Reject("not_at_settlement", "Not at a settlement");

                var biome = rNode.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";
                var rationRng = new Random();
                Rations.Refill(player, data.Balance,
                    () => $"Rations ({FlavorText.RationName(biome, rationRng)})");
                await store.Save(player);

                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(rNode, player, session),
                    Inventory = BuildInventory(player),
                    Mechanics = BuildMechanics(player),
                };
                break;
            }

            case "claim_haul":
            {
                var hNode = session.CurrentNode;
                if (hNode.Poi?.Kind != PoiKind.Settlement || hNode.Poi.SettlementId == null)
                    return Reject("not_at_settlement", "Not at a settlement");

                if (string.IsNullOrEmpty(actionReq.OfferId))
                    return Reject("missing_offer_id", "offerId required");

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(hNode.Poi.SettlementId, out var haulState))
                    return Reject("settlement_not_initialized", "Settlement not initialized");

                var claimResult = Market.ClaimHaul(player, actionReq.OfferId, haulState);
                if (!claimResult.Success)
                    return Reject("haul_claim_refused", claimResult.Message);

                await store.Save(player);
                response = new GameResponse
                {
                    Mode = "exploring",
                    Status = BuildStatus(player),
                    Node = BuildNodeInfo(hNode, player, session),
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
                    return Reject("missing_offer_id", "offerId required");

                var haulIdx = player.Pack.FindIndex(i => i.HaulOfferId == actionReq.OfferId);
                if (haulIdx < 0)
                    return Reject("haul_not_in_pack", "Haul not found in pack");

                player.Pack.RemoveAt(haulIdx);
                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "bank_deposit":
            {
                var bNode = session.CurrentNode;
                if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                    return Reject("not_at_settlement", "Not at a settlement");

                if (string.IsNullOrEmpty(actionReq.ItemId) || string.IsNullOrEmpty(actionReq.Source))
                    return Reject("missing_item_or_source", "itemId and source required");

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bDepositState))
                    return Reject("settlement_not_initialized", "Settlement not initialized");

                var depositError = Bank.Deposit(player, actionReq.ItemId, actionReq.Source, bDepositState, data.Balance);
                if (depositError != null)
                    return Reject("bank_deposit_refused", depositError);

                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "bank_withdraw":
            {
                var bNode = session.CurrentNode;
                if (bNode.Poi?.Kind != PoiKind.Settlement || bNode.Poi.SettlementId == null)
                    return Reject("not_at_settlement", "Not at a settlement");

                if (actionReq.BankIndex == null)
                    return Reject("missing_bank_index", "bankIndex required");

                SettlementRunner.EnsureSettlement(session);
                if (!player.Settlements.TryGetValue(bNode.Poi.SettlementId, out var bWithdrawState))
                    return Reject("settlement_not_initialized", "Settlement not initialized");

                var withdrawError = Bank.Withdraw(player, actionReq.BankIndex.Value, bWithdrawState, data.Balance);
                if (withdrawError != null)
                    return Reject("bank_withdraw_refused", withdrawError);

                await store.Save(player);
                response = BuildInventoryResponse(session, player);
                break;
            }

            case "equip":
            {
                if (string.IsNullOrEmpty(actionReq.ItemId))
                    return Reject("missing_item_id", "ItemId is required");

                var results = Mechanics.Apply([$"equip {actionReq.ItemId}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return Reject("cannot_equip", $"Cannot equip '{actionReq.ItemId}' — not in pack or not equippable");

                response = BuildInventoryResponse(session, player);
                break;
            }

            case "unequip":
            {
                var slot = actionReq.Slot;
                if (string.IsNullOrEmpty(slot))
                    return Reject("missing_slot", "Slot is required (weapon, armor)");

                var results = Mechanics.Apply([$"unequip {slot}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return Reject("slot_empty", $"Nothing equipped in slot '{slot}'");

                response = BuildInventoryResponse(session, player);
                break;
            }

            case "discard":
            {
                if (string.IsNullOrEmpty(actionReq.ItemId))
                    return Reject("missing_item_id", "ItemId is required");

                var results = Mechanics.Apply([$"discard {actionReq.ItemId}"], player, data.Balance, session.Rng);
                if (results.Count == 0)
                    return Reject("item_not_in_inventory", $"Item '{actionReq.ItemId}' not found in inventory");

                response = BuildInventoryResponse(session, player);
                break;
            }

            default:
                return Reject("unknown_action", $"Unknown action: {actionReq.Action}");
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
            skillModifiers = new Dictionary<string, int>(),
            requiredCombat = entry.Item.RequiredCombat,
            description = entry.Item.Description ?? "",
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

        var rationCost = data.Balance.Items[Rations.RationDefId].Cost ?? 0;

        return new OkObjectResult(new { tier, stock, hauls, sellPrices, rationCost });
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
        var services = Inn.GetServiceOptions(data.Balance);
        var needsRecovery = player.Spirits < player.MaxSpirits
                         || player.ActiveConditions.Any(id =>
                                data.Balance.Conditions.TryGetValue(id, out var def)
                                && def.Severity == ConditionSeverity.Severe);

        return new OkObjectResult(new
        {
            isChapterhouse,
            needsRecovery,
            services = services.Select(s =>
            {
                var cost = isChapterhouse ? 0 : s.Cost;
                return new
                {
                    id = s.Id,
                    name = s.Name,
                    cost,
                    spirits = s.Spirits,
                    restoresFull = s.RestoresFull,
                    canAfford = player.Gold >= cost,
                };
            }).ToList(),
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
        Telemetry.RecordDebugEndpointHit("add-condition");

        var debugReq = await req.ReadFromJsonAsync<DebugConditionRequest>();
        if (debugReq == null) return new BadRequestObjectResult(new { error = "Invalid request body" });

        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var conditionId = debugReq.Condition;
        if (string.IsNullOrWhiteSpace(conditionId))
            return new BadRequestObjectResult(new { error = "Missing condition" });

        if (!player.ActiveConditions.Add(conditionId))
            return new OkObjectResult(new { message = $"Already has {conditionId}" });

        await store.Save(player);

        return new OkObjectResult(new { message = $"Added {conditionId}" });
    }

    // ── Builder helpers ──

    GameSession BuildSession(PlayerState player)
    {
        // Mix in Day, Time, MoveCount and condition count alongside Seed + visited count so the
        // RNG advances between API calls even when the player is stationary. Without this, a
        // stuck player keeps re-rolling the exact same outcome and can lock into a bad state.
        var rngSeed = player.Seed
                    + player.VisitedNodes.Count * 31
                    + player.Day * 1009
                    + (int)player.Time * 17
                    + player.MoveCount * 7
                    + player.ActiveConditions.Count
                    // Mix in vitals + active-combat tick so each combat step gets a fresh
                    // RNG. Without these, every attack request rolls identical dice.
                    + player.Spirits * 11
                    + player.Health * 13
                    + (player.ActiveCombat?.Turn * 23 ?? 0)
                    + (player.ActiveCombat?.MonsterHp * 29 ?? 0);
        var rng = new Random(rngSeed);
        var session = new GameSession(player, data.Map, data.Bundle, data.Balance, rng, data.CombatBundle);

        if (player.ActiveCombat != null)
        {
            session.Mode = SessionMode.InCombat;
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
        Conditions = p.ActiveConditions.Select(id =>
        {
            var def = data.Balance.Conditions.GetValueOrDefault(id);
            var flavor = data.Balance.ConditionFlavors.GetValueOrDefault(id);
            return new ConditionInfo
            {
                Id = id,
                Name = def?.Name ?? id,
                Stacks = 1,
                Description = flavor?.Ongoing ?? "",
                Effect = BuildConditionEffect(def),
            };
        }).ToList(),
        Skills = Skills.All.Select(si =>
        {
            var tier = p.Skills.GetValueOrDefault(si.Skill);
            var level = (int)tier;
            var tierName = tier switch
            {
                SkillTier.Expert => "expert",
                SkillTier.Trained => "trained",
                _ => "untrained",
            };
            var tierFormatted = tier switch
            {
                SkillTier.Expert => "Expert",
                SkillTier.Trained => "Trained",
                _ => "Untrained",
            };
            return new SkillInfoDto
            {
                Id = si.ScriptName,
                Name = si.DisplayName,
                Level = level,
                Tier = tierName,
                Formatted = tierFormatted,
                Flavor = SkillFlavor.Get(si.Skill, level),
            };
        }).ToList(),
    };

    NodeInfo BuildNodeInfo(Node node, PlayerState p, GameSession session)
    {
        List<string>? services = null;
        if (node.Poi?.Kind == PoiKind.Settlement)
        {
            var isChapterhouse = node == data.Map.StartingCity;
            services = ["market", "bank", isChapterhouse ? "chapterhouse" : "inn"];

            if (node.Poi.SettlementId != null
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
            Description = i.Description ?? def?.Description,
            Type = def?.Type.ToString().ToLowerInvariant() ?? "",
            Cost = def?.Cost,
            SkillModifiers = [],
            Cures = def?.Cures.ToList() ?? [],
            Immunities = data.Balance.Hazards.Values
                .Where(h => h.MitigatingItemId == i.DefId)
                .Select(h => h.Name)
                .ToList(),
            Moves = def?.Type is ItemType.Weapon or ItemType.Armor
                ? def.RpsMoves.Select(m => m.DisplayName).ToList()
                : [],
            IsEquippable = def?.Type is ItemType.Weapon or ItemType.Armor,
            IsEquipped = i.IsEquipped,
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
    };

    MechanicsInfo BuildMechanics(PlayerState p)
    {
        var resistances = new List<MechanicLine>();
        var encounterChecks = new List<MechanicLine>();
        var other = new List<MechanicLine>();

        foreach (var (condId, condDef) in data.Balance.Conditions)
        {
            // Severe conditions resist via Cunning (Mechanics.ApplyAddCondition)
            var resistSkill = condDef.Severity == ConditionSeverity.Severe
                ? (Skill?)Skill.Cunning
                : null;

            var skillBonus = resistSkill != null ? (int)p.Skills.GetValueOrDefault(resistSkill.Value) : 0;

            var source = resistSkill switch
            {
                { } s => s.GetInfo().DisplayName,
                null => "None",
            };

            resistances.Add(new MechanicLine
            {
                Label = condDef.Name,
                Value = $"+{skillBonus}",
                Source = source,
            });
        }

        foreach (var si in Skills.All)
        {
            var skillLevel = (int)p.Skills.GetValueOrDefault(si.Skill);

            encounterChecks.Add(new MechanicLine
            {
                Label = si.DisplayName,
                Value = FormatSkillLevel(skillLevel),
                Source = si.DisplayName,
            });
        }

        var negotiationTier = (int)p.Skills.GetValueOrDefault(Skill.Negotiation);
        var haulBonus = (int)(negotiationTier * 0.2 * 100);
        other.Add(new MechanicLine
        {
            Label = "Contract bonus",
            Value = $"+{haulBonus}%",
            Source = "Negotiation",
        });

        var bushcraftTier = p.Skills.GetValueOrDefault(Skill.Bushcraft);
        other.Add(new MechanicLine
        {
            Label = "Travel hazard costs",
            Value = bushcraftTier switch
            {
                SkillTier.Trained => "half",
                SkillTier.Expert => "quarter",
                _ => "full",
            },
            Source = "Bushcraft",
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
        Node = BuildNodeInfo(s.CurrentNode, p, s),
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
                MechanicResult.ConditionAdded c => $"Condition: {c.ConditionId}",
                MechanicResult.ConditionResisted cr => cr.Check != null ? $"Resisted: {cr.ConditionId} (rolled {cr.Check.Rolled} vs DC {cr.Check.Target})" : $"Resisted: {cr.ConditionId}",
                MechanicResult.ConditionRemoved c => $"Condition removed: {c.ConditionId}",
                MechanicResult.TimeAdvanced ta => $"Time: {ta.NewPeriod}, Day {ta.NewDay}",
                MechanicResult.Navigation n => $"Navigate to: {n.EncounterId}",
                MechanicResult.DungeonFinished => "Dungeon completed!",
                MechanicResult.DungeonFled => "Fled the dungeon!",
                _ => r.ToString() ?? "",
            },
            ResistCheck = r switch
            {
                MechanicResult.ConditionResisted { Check: { } ck2 } cr2 => new ResistCheckInfo
                {
                    ConditionId = cr2.ConditionId,
                    ConditionName = data.Balance.Conditions.GetValueOrDefault(cr2.ConditionId)?.Name ?? cr2.ConditionId,
                    Passed = ck2.Passed,
                    Rolled = ck2.Rolled,
                    Target = ck2.Target,
                    Modifier = ck2.Modifier,
                    RollMode = ck2.RollMode != Dreamlands.Game.RollMode.Normal ? ck2.RollMode.ToString().ToLowerInvariant() : null,
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

    GameResponse BuildExploringResponse(GameSession session, List<DeliveryInfo>? deliveries = null,
        ArrivalInfo? arrival = null) => new()
    {
        Mode = "exploring",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
        Exits = BuildExits(session),
        Inventory = BuildInventory(session.Player),
        Mechanics = BuildMechanics(session.Player),
        Deliveries = deliveries,
        Arrival = arrival,
    };

    GameResponse BuildEncounterResponse(GameSession session, Encounter encounter, List<GatedChoice> gated) => new()
    {
        Mode = "encounter",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
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

    GameResponse BuildApproachPromptResponse(GameSession session, EncounterStep.AwaitApproach awaitApproach) => new()
    {
        Mode = "approach_prompt",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
        Encounter = BuildEncounterInfo(awaitApproach.Encounter, Choices.GetAllWithLockState(awaitApproach.Encounter, session.Player, session.Balance)),
        ApproachPrompt = new ApproachPromptInfo
        {
            Skill = awaitApproach.Skill.ScriptName(),
            Preamble = awaitApproach.Preamble,
            Approaches = awaitApproach.Approaches.Select(a => new ApproachInfo
            {
                Id = a.Id,
                Label = a.DisplayLabel,
                IconHint = a.IconHint,
            }).ToList(),
        },
        Inventory = BuildInventory(session.Player),
        Mechanics = BuildMechanics(session.Player),
    };

    GameResponse BuildTableauPromptResponse(GameSession session, EncounterStep.AwaitTableauPick awaitTableau) => new()
    {
        Mode = "tableau_prompt",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
        Outcome = awaitTableau.Outcome != null ? BuildOutcomeInfo(awaitTableau.Outcome) : null,
        TableauPrompt = new TableauPromptInfo
        {
            PendingLevels = awaitTableau.PendingLevels,
            Slots = Dreamlands.Rules.ArcRewards.All.Select(s => new TableauSlotInfo
            {
                Id = s.Id,
                Label = s.Label,
                Kind = s.Kind.ToString().ToLowerInvariant(),
                CurrentCount = session.Player.ArcRewardsTaken.GetValueOrDefault(s.Id),
                Cap = s.Cap,
                IsPickable = awaitTableau.AvailableSlots.Any(a => a.Id == s.Id),
                Tier1Description = s.Tier1Description,
                Tier2Description = s.Tier2Description,
            }).ToList(),
        },
        Inventory = BuildInventory(session.Player),
        Mechanics = BuildMechanics(session.Player),
    };

    GameResponse BuildCampResponse(GameSession session, CampInfo camp, List<DeliveryInfo>? deliveries = null) => new()
    {
        Mode = "camp",
        Status = BuildStatus(session.Player),
        Node = BuildNodeInfo(session.CurrentNode, session.Player, session),
        Camp = camp,
        Deliveries = deliveries,
        Inventory = BuildInventory(session.Player),
        Mechanics = BuildMechanics(session.Player),
    };

    static string BiomeOf(Dreamlands.Map.Node node)
        => node.Region?.Terrain.ToString().ToLowerInvariant() ?? "plains";

    /// <summary>Flush the travails ledger into display lines. Null when the trip accrued nothing.</summary>
    List<TravailLineInfo>? BuildTravailLines(PlayerState player)
        => Travails.Summarize(player, data.Balance)?.Lines
            .Select(l => new TravailLineInfo
            {
                Name = l.Name,
                SpiritsLost = l.SpiritsLost,
                SparedByGear = l.SparedByGear,
                Text = l.Text,
            })
            .ToList();

    List<ConditionRowInfo> BuildConditionRows(
        HashSet<string> conditionsBefore,
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
        foreach (var conditionId in conditionsBefore)
        {
            if (!data.Balance.Conditions.TryGetValue(conditionId, out var def)) continue;

            var cure = cures.GetValueOrDefault(conditionId);
            var drain = drains.GetValueOrDefault(conditionId);

            string? cureItem = null;
            string? cureMessage = null;
            int stacksAfter = cure != null ? 0 : 1;

            if (cure != null)
            {
                var itemDef = data.Balance.Items.GetValueOrDefault(cure.ItemDefId);
                cureItem = itemDef?.Name ?? cure.ItemDefId;
                cureMessage = $"Used {cureItem}, cured!";
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
                Stacks = 1,
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
                EndOfDayEvent.FoodConsumed f => $"Ate: {string.Join(", ", f.FoodEaten)}",
                EndOfDayEvent.Starving => "No food!",
                EndOfDayEvent.CureApplied c => $"{c.ItemDefId} cured {c.ConditionId}!",
                EndOfDayEvent.ConditionCured c => $"{c.ConditionId} cured!",
                EndOfDayEvent.ConditionDrain d => $"{d.ConditionId}: -{d.HealthLost} health, -{d.SpiritsLost} spirits",
                EndOfDayEvent.SpecialEffect s => $"{s.ConditionId}: {s.Effect}",
                EndOfDayEvent.HealthRegen h => $"Rest: +{h.HealthGained} health",
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
        if (def.SpecialEffect is { } se)
            parts.Add(se);
        return string.Join(". ", parts) + (parts.Count > 0 ? "." : "");
    }

    static string FormatSkillLevel(int level) => level >= 0 ? $"+{level}" : $"{level}";

    static string ResolveCauseName(string id, BalanceData balance)
    {
        if (balance.Conditions.TryGetValue(id, out var def) && !string.IsNullOrEmpty(def.Name))
            return def.Name;
        return id switch
        {
            "starving" => "Starvation",
            _ => char.ToUpperInvariant(id[0]) + id[1..].Replace('_', ' '),
        };
    }

    static string FormatItemDescription(ItemDef item)
    {
        if (item.Cures.Count > 0)
            return $"Cures: {string.Join(", ", item.Cures)}";
        return "";
    }

    // ── Combat ──

    [Function("CombatList")]
    public IActionResult CombatList(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "game/{id}/combat/list")] HttpRequest req,
        string id)
    {
        // Debug-only affordance: lists all loaded .fight encounters so the picker
        // overlay in Explore can let the developer fight any of them. Players
        // never reach this endpoint via the normal play path.
        Telemetry.RecordDebugEndpointHit("combat-list");

        if (data.CombatBundle == null)
            return new OkObjectResult(new { encounters = Array.Empty<object>() });
        var list = data.CombatBundle.Encounters
            .Select(e => new CombatEncounterSummary {
                Id = e.Id,
                Title = e.Title,
                Category = e.Category,
                Tier = e.Tier,
                Hp = e.Stats.Hp,
            })
            .OrderBy(e => e.Tier ?? int.MaxValue)
            .ThenBy(e => e.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(e => e.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return new OkObjectResult(new CombatListResponse { Encounters = list });
    }

    [Function("CombatBegin")]
    public async Task<IActionResult> CombatBegin(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/combat/begin")] HttpRequest req,
        string id)
    {
        Telemetry.RecordDebugEndpointHit("combat-begin");

        var beginReq = await req.ReadFromJsonAsync<CombatBeginRequest>();
        if (beginReq == null || string.IsNullOrWhiteSpace(beginReq.EncounterId))
            return new BadRequestObjectResult(new { error = "Missing encounterId" });

        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        if (data.CombatBundle == null)
            return new BadRequestObjectResult(new { error = "Combat bundle not loaded" });

        if (player.ActiveCombat != null)
            return new BadRequestObjectResult(new { error = "Combat already in progress" });

        Dreamlands.Orchestration.CombatOrchestrator.CombatTurn turn;
        try
        {
            turn = BeginCombat(session, beginReq.EncounterId);
        }
        catch (InvalidOperationException ex)
        {
            return new BadRequestObjectResult(new { error = ex.Message });
        }

        if (turn.PlayerDied)
            return await CombatRescue(player, session);

        await store.Save(player);
        return new OkObjectResult(BuildCombatResponse(session, turn));
    }

    [Function("CombatAction")]
    public async Task<IActionResult> CombatAction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "game/{id}/combat/action")] HttpRequest req,
        string id)
    {
        var actionReq = await req.ReadFromJsonAsync<CombatActionRequest>();
        if (actionReq == null) return new BadRequestObjectResult(new { error = "Invalid request body" });

        var player = await store.Load(id);
        if (player == null) return new NotFoundObjectResult(new { error = "Game not found" });

        var session = BuildSession(player);
        if (player.ActiveCombat == null)
            return new BadRequestObjectResult(new { error = "No active combat" });

        Dreamlands.Combat.PlayerCombatAction action;
        switch (actionReq.Action?.ToLowerInvariant())
        {
            case "commit":
                if (actionReq.Slots is null || actionReq.Slots.Count != 3)
                    return new BadRequestObjectResult(new { error = "commit requires exactly 3 slot strings" });
                try
                {
                    var s1 = Dreamlands.Encounter.Move.Parse(actionReq.Slots[0]);
                    var s2 = Dreamlands.Encounter.Move.Parse(actionReq.Slots[1]);
                    var s3 = Dreamlands.Encounter.Move.Parse(actionReq.Slots[2]);
                    action = new Dreamlands.Combat.PlayerCombatAction.Commit(s1, s2, s3);
                }
                catch (ArgumentException ex)
                {
                    return new BadRequestObjectResult(new { error = ex.Message });
                }
                break;
            case "flee":
                action = new Dreamlands.Combat.PlayerCombatAction.Flee();
                break;
            case "continue":
                // Player dismissed the defeat coda — chain to the rescue flow now.
                if (player.ActiveCombat is not { Resolved: true, PlayerLost: true })
                    return new BadRequestObjectResult(new { error = "continue is only valid from a defeat coda" });
                return await CombatRescue(player, session);
            default:
                return new BadRequestObjectResult(new { error = $"Unknown combat action '{actionReq.Action}'" });
        }

        // Commit and flee only mean anything while the fight is live. A double-submit
        // (two clicks on the final commit, or a client retry) would otherwise reach
        // CombatRunner.Step and surface its internal invariant as a 500. The "continue"
        // branch above already returned, so it isn't caught by this.
        if (player.ActiveCombat.Resolved)
            return new BadRequestObjectResult(new { error = "Combat is already resolved" });

        var turn = Dreamlands.Orchestration.CombatOrchestrator.Step(session, action);
        RecordCombatOutcome(turn);

        // On player death, don't rescue immediately — leave the defeat coda visible
        // and let Continue (action="continue", above) chain into CombatRescue.
        await store.Save(player);
        return new OkObjectResult(BuildCombatResponse(session, turn));
    }

    /// <summary>
    /// Wraps EncounterRunner.Begin so every encounter entry is counted in one place —
    /// encounters start from the intro, road rolls, chains and navigation targets.
    /// </summary>
    private static EncounterStep.ShowEncounter BeginEncounter(GameSession session, Encounter encounter)
    {
        Telemetry.RecordEncounterStarted(encounter.Id);
        return EncounterRunner.Begin(session, encounter);
    }

    /// <summary>
    /// Wraps CombatOrchestrator.Begin so every entry into a fight is counted in one
    /// place — combat can start from an encounter chain, a navigation target, or the
    /// debug picker.
    /// </summary>
    private static Dreamlands.Orchestration.CombatOrchestrator.CombatTurn BeginCombat(
        GameSession session, string encounterId)
    {
        var turn = Dreamlands.Orchestration.CombatOrchestrator.Begin(session, encounterId);
        Telemetry.RecordCombatStarted();
        RecordCombatOutcome(turn);
        return turn;
    }

    /// <summary>
    /// Combat metrics live at this boundary, not in Dreamlands.Combat — the engine
    /// stays a pure (state, args, balance, rng) -> (state, results) library so it can
    /// be driven headless for testing and modelling. The terminal Outcome event is
    /// the engine telling us how the fight ended; we only translate it.
    /// </summary>
    private static void RecordCombatOutcome(Dreamlands.Orchestration.CombatOrchestrator.CombatTurn turn)
    {
        var outcome = turn.Events.OfType<Dreamlands.Combat.CombatEvent.Outcome>().FirstOrDefault();
        if (outcome == null) return;

        Telemetry.RecordCombatEnded(outcome switch
        {
            { PlayerWon: true } => "won",
            { PlayerLost: true } => "lost",
            { PlayerFled: true } => "fled",
            { MonsterFled: true } => "monster_fled",
            _ => "other",
        });
    }

    /// <summary>
    /// Apply the standard rescue (strip purchasables, reset gold, teleport to chapterhouse,
    /// full recovery, day++) and return a "rescued" response. Triggered by the
    /// action="continue" branch of CombatAction once the player has dismissed the
    /// defeat coda — CombatOrchestrator leaves ActiveCombat in place on a loss
    /// specifically so this can run after Continue, not at the moment of death.
    /// The rescue surface IS the death screen per project/combat/landing_plan.md.
    /// </summary>
    async Task<IActionResult> CombatRescue(Dreamlands.Game.PlayerState player, Dreamlands.Orchestration.GameSession session)
    {
        var sc = data.Map.StartingCity;
        var rescue = Dreamlands.Game.Rescue.Apply(player, sc?.X ?? 0, sc?.Y ?? 0, data.Balance);
        // Combat is over; clear lingering combat session state so the next request
        // routes through normal exploring, not stuck in InCombat.
        player.ActiveCombat = null;
        session.Mode = Dreamlands.Orchestration.SessionMode.Exploring;
        await store.Save(player);
        return new OkObjectResult(new GameResponse
        {
            Mode = "rescued",
            Status = BuildStatus(player),
            Rescue = new RescueInfo
            {
                LostItems = rescue.LostItems,
                GoldLost = rescue.GoldLost,
            },
            Node = BuildNodeInfo(session.CurrentNode, player, session),
            Exits = BuildExits(session),
            Inventory = BuildInventory(player),
        });
    }

    GameResponse BuildCombatResponse(GameSession session, Dreamlands.Orchestration.CombatOrchestrator.CombatTurn turn)
    {
        var info = BuildCombatInfo(session, turn);
        return new GameResponse
        {
            Mode = info.Resolved ? "combat_resolved" : "combat",
            Status = BuildStatus(session.Player),
            Combat = info,
            Inventory = BuildInventory(session.Player),
            Mechanics = BuildMechanics(session.Player),
        };
    }

    CombatInfo BuildCombatInfo(GameSession session, Dreamlands.Orchestration.CombatOrchestrator.CombatTurn turn)
    {
        // After Finalize, ActiveCombat is null on resolved combats — the events still
        // carry the terminal state, so reconstruct what we can from them.
        var state = session.Player.ActiveCombat;
        // After Finalize clears ActiveCombat, fall back to turn.EncounterId so the
        // resolved-state response still has the encounter (vignette, title, blood).
        var encounterId = state?.EncounterId ?? turn.EncounterId;
        var encounter = string.IsNullOrEmpty(encounterId)
            ? null
            : data.CombatBundle?.GetById(encounterId);

        var events = BuildCombatEventLog(turn.Events);
        var outcome = turn.Events.OfType<Dreamlands.Combat.CombatEvent.Outcome>().FirstOrDefault();
        var introEvt = turn.Events.OfType<Dreamlands.Combat.CombatEvent.Intro>().FirstOrDefault();
        var lastTurn = turn.Events.OfType<Dreamlands.Combat.CombatEvent.TurnStarted>().LastOrDefault();

        string weaponClass = state?.Profile.Weapon?.ToString() ?? "Unarmed";
        string armorClass = state?.Profile.Armor?.ToString() ?? "Unarmored";
        int monsterHp = state?.MonsterHp ?? 0;
        int monsterMaxHp = state?.MonsterMaxHp ?? 0;

        // Player move pool: surface encoding + authored display name so the CLI/UI
        // can render selectors. Cooldown filtering happens client-side using
        // PlayerLastUsedTurn (also surfaced) plus the current turn number.
        var movePool = state?.Profile.MovePool
            .Select(em => new MoveOption(em.Encoded, em.DisplayName))
            .ToList()
            ?? new List<MoveOption>();

        return new CombatInfo
        {
            EncounterId = encounter?.Id ?? state?.EncounterId ?? "",
            Title = encounter?.Title ?? "",
            Image = string.IsNullOrEmpty(encounter?.Image) ? null : encounter.Image,
            BiomeImage = BuildCombatBiomeImage(encounter),
            BloodColor = encounter?.BloodColor ?? "#7a0a0a",
            IntroText = introEvt?.Text ?? encounter?.Intro ?? "",

            MonsterHp = monsterHp,
            MonsterMaxHp = monsterMaxHp,

            PlayerSpirits = session.Player.Spirits,
            PlayerMaxSpirits = session.Player.MaxSpirits,
            PlayerHealth = session.Player.Health,
            PlayerMaxHealth = session.Player.MaxHealth,
            PlayerWeaponClass = weaponClass,
            PlayerArmorClass = armorClass,
            PlayerMovePool = movePool,
            PlayerCarryStun = state?.PlayerCarryStun?.ToList() ?? new List<bool> { false, false, false },
            PlayerLastUsedTurn = state?.PlayerLastUsedTurn?.ToDictionary(kv => kv.Key, kv => kv.Value)
                                  ?? new Dictionary<string, int>(),

            Turn = state?.Turn ?? outcome?.Turns ?? 0,
            Tell = lastTurn?.Tell ?? "",
            Plan = lastTurn?.Plan?.Select(m => m.Encoded).ToList(),

            Resolved = turn.Resolved,
            PlayerWon = outcome?.PlayerWon ?? false,
            PlayerLost = outcome?.PlayerLost ?? false,
            PlayerFled = outcome?.PlayerFled ?? false,
            MonsterFled = outcome?.MonsterFled ?? false,
            OutcomeText = outcome?.Text,
            OutcomeMechanics = turn.Resolved ? BuildMechanicResults(turn.OutcomeMechanics.ToList()) : null,

            Events = events,
        };
    }

    // Vignette path relative to assets/vignettes, derived from the encounter's
    // category ("forest/tier2") + tier. Matches the convention used by Explore
    // and Encounter screens: "{biome}/{biome}_tier_{n}_1".
    static string? BuildCombatBiomeImage(Dreamlands.Encounter.CombatEncounter? encounter)
    {
        if (encounter is null) return null;
        if (!string.IsNullOrEmpty(encounter.Background)) return encounter.Background;
        if (encounter.Tier is null) return null;

        // Category is combat/<biome>/tier<n> for road fights (or <biome>/tier<n>
        // pre-relocation); arc fights (arcs/<biome>/<arc>) have no tier and rely
        // on [background] above.
        var parts = encounter.Category.Split('/');
        var biome = parts[0] is "combat" or "arcs" && parts.Length > 1 ? parts[1] : parts[0];
        if (string.IsNullOrEmpty(biome)) return null;
        return $"{biome}/{biome}_tier_{encounter.Tier}_1";
    }

    /// <summary>
    /// Walks the event stream and produces the log feed. Phase 1+2 renders bare strings;
    /// Phase 3+4 will give the UI structured per-slot panels with both moves and the
    /// HP delta. The CLI consumes the strings as-is.
    /// </summary>
    static List<CombatLogEntry> BuildCombatEventLog(IReadOnlyList<Dreamlands.Combat.CombatEvent> evts)
    {
        var log = new List<CombatLogEntry>();
        foreach (var evt in evts)
        {
            var entry = BuildCombatLogEntry(evt);
            if (entry != null) log.Add(entry);
        }
        return log;
    }

    static CombatLogEntry? BuildCombatLogEntry(Dreamlands.Combat.CombatEvent evt) => evt switch
    {
        Dreamlands.Combat.CombatEvent.Intro x               => Plain(x.Text),
        Dreamlands.Combat.CombatEvent.TurnStarted x         => RenderTurnStarted(x),
        Dreamlands.Combat.CombatEvent.SlotResolved x        => RenderSlotResolved(x),
        Dreamlands.Combat.CombatEvent.PlayerFleeAttempted _ => Plain("  You break and run."),
        // Outcome is rendered by the client's OutcomePanel — no inline log entry.
        Dreamlands.Combat.CombatEvent.Outcome _              => null,
        _                                                    => null,
    };

    static CombatLogEntry Plain(string text) => new() { Text = text };

    static CombatLogEntry RenderTurnStarted(Dreamlands.Combat.CombatEvent.TurnStarted x)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append($"— Turn {x.Turn} — {x.Tell}");
        if (x.Plan is not null && x.Plan.Count > 0)
        {
            sb.Append("  [read] plan: ");
            sb.Append(string.Join("  /  ", x.Plan.Select(m => m.Encoded)));
        }
        return Plain(sb.ToString());
    }

    static CombatLogEntry RenderSlotResolved(Dreamlands.Combat.CombatEvent.SlotResolved x)
    {
        // Death-aftermath: a side's move is Skipped *and* their HP is 0. The slot
        // where the death itself happened keeps the actual killing move on file
        // (move != skipped), so this only catches the slots that ran against a corpse.
        bool monsterDeadAftermath = x.MonsterMove.Base == "skipped" && x.MonsterHpAfter == 0;
        bool playerDeadAftermath  = x.PlayerMove.Base  == "skipped" && x.PlayerHealthAfter == 0;
        if (monsterDeadAftermath || playerDeadAftermath)
        {
            return new CombatLogEntry
            {
                Text = "",
                Slot = x.Slot,
                PlayerMove = x.PlayerMove.Encoded,
                MonsterMove = x.MonsterMove.Encoded,
            };
        }

        // Prosaic single line per slot: "You picked Read, they picked Defend • You take 2 damage, they take 0"
        var moves = (x.PlayerMove.Base, x.MonsterMove.Base) switch
        {
            ("skipped", "skipped") => "Both stunned this slot",
            ("skipped", _)         => $"You're stunned, they picked {VerbName(x.MonsterMove)}",
            (_, "skipped")         => $"You picked {VerbName(x.PlayerMove)}, they're stunned",
            _                      => $"You picked {VerbName(x.PlayerMove)}, they picked {VerbName(x.MonsterMove)}",
        };
        var effect = $"{DescribeDelta(x.PlayerDelta, "You")}, {DescribeDelta(x.MonsterDelta, "they")}";
        var line = $"{moves} • {effect}";
        if (!string.IsNullOrEmpty(x.MonsterNarration))
            line += $"\n    {x.MonsterNarration}";
        return new CombatLogEntry
        {
            Text = line,
            PlayerAttack = x.PlayerMove.Base == "attack"
                ? new PlayerAttackInfo
                {
                    Outcome = x.MonsterDelta < 0 ? "hit" : "miss",
                    Damage = x.MonsterDelta < 0 ? -x.MonsterDelta : null,
                }
                : null,
            Slot = x.Slot,
            PlayerMove = x.PlayerMove.Encoded,
            MonsterMove = x.MonsterMove.Encoded,
        };
    }

    // Capitalized base verb for the slot log. Mutators are surfaced through icons
    // and tooltips; the prose log just names what kind of action each side took.
    static string VerbName(Dreamlands.Encounter.Move m) =>
        m.Base.Length == 0 ? m.Base : char.ToUpper(m.Base[0]) + m.Base[1..];

    static string DescribeDelta(int delta, string subj) =>
        delta < 0 ? $"{subj} take {-delta} damage"
        : delta > 0 ? $"{subj} recover {delta}"
        : $"{subj} take 0";

}
