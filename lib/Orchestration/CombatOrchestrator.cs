using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Rules;

namespace Dreamlands.Orchestration;

/// <summary>
/// Bridges the stateless RPS combat engine to <see cref="GameSession"/>: looks up the
/// encounter from the bundle, builds the player profile from current equipment + skills,
/// owns <see cref="SessionMode"/> transitions, and applies win/lose mechanics through
/// the regular <see cref="Mechanics.Apply"/> pipeline on resolution.
/// </summary>
public static class CombatOrchestrator
{
    public sealed record CombatTurn(
        string EncounterId,
        IReadOnlyList<CombatEvent> Events,
        IReadOnlyList<MechanicResult> OutcomeMechanics,
        bool Resolved,
        bool PlayerDied);

    public static CombatTurn Begin(GameSession session, string encounterId)
    {
        var encounter = ResolveEncounter(session, encounterId);

        var profile = BuildProfile(session.Player);
        var state = new CombatState
        {
            EncounterId = encounter.Id,
            Profile = profile,
        };

        var events = CombatRunner.Begin(encounter, session.Player, state, session.Rng);

        session.Player.ActiveCombat = state;
        session.Mode = SessionMode.InCombat;

        return Finalize(session, encounter, events);
    }

    public static CombatTurn Step(GameSession session, PlayerCombatAction action)
    {
        var state = session.Player.ActiveCombat
            ?? throw new InvalidOperationException("No active combat to step.");
        var encounter = ResolveEncounter(session, state.EncounterId);

        var events = CombatRunner.Step(encounter, session.Player, state, action, session.Rng);
        return Finalize(session, encounter, events);
    }

    static CombatTurn Finalize(GameSession session, CombatEncounter encounter, IReadOnlyList<CombatEvent> events)
    {
        var state = session.Player.ActiveCombat!;
        var outcomeMechanics = new List<MechanicResult>();
        bool playerDied = false;

        if (state.Resolved)
        {
            // Apply win/lose mechanics through the regular pipeline so they show up the
            // same as encounter outcomes (gold, tags, conditions, etc.). Flee + monster-flee
            // emit no mechanics today; revisit if encounter authors need flee-specific hooks.
            var mechanics = state.PlayerWon
                ? encounter.WinMechanics
                : state.PlayerLost
                    ? encounter.LoseMechanics
                    : (IReadOnlyList<string>)Array.Empty<string>();
            if (mechanics.Count > 0)
                outcomeMechanics = Mechanics.Apply(mechanics, session.Player, session.Balance, session.Rng);

            playerDied = state.PlayerLost || session.Player.Health <= 0;

            if (!playerDied)
            {
                session.Player.ActiveCombat = null;
                session.Mode = SessionMode.Exploring;
            }
            // Defeat path: keep ActiveCombat + InCombat mode so the client can render
            // the defeat coda (and re-render on reload). Rescue is deferred to an
            // explicit "continue" action on the OutcomeCard's Continue button — see
            // CombatAction in GameFunctions.cs.
        }

        return new CombatTurn(encounter.Id, events, outcomeMechanics, state.Resolved, playerDied);
    }

    static CombatEncounter ResolveEncounter(GameSession session, string id)
    {
        if (session.CombatBundle == null)
            throw new InvalidOperationException("Session has no combat bundle loaded.");
        return session.CombatBundle.GetById(id)
            ?? throw new InvalidOperationException($"Combat encounter not found in bundle: '{id}'");
    }

    /// <summary>
    /// Snapshot the player's combat-relevant kit from equipped weapon/armor. The
    /// resulting move pool reads RpsMoves directly off the itemdef.
    /// </summary>
    public static CombatPlayerProfile BuildProfile(PlayerState player)
    {
        ItemDef? weapon = null;
        ItemDef? armor = null;

        if (player.EquippedWeapon is { } w && ItemDef.All.TryGetValue(w.DefId, out var wDef))
            weapon = wDef;
        if (player.EquippedArmor is { } a && ItemDef.All.TryGetValue(a.DefId, out var aDef))
            armor = aDef;

        return CombatPlayerProfile.From(weapon, armor);
    }
}
