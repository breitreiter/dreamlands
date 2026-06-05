using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;

namespace Dreamlands.Orchestration.Tests;

public class CombatOrchestratorTests
{
    /// <summary>1-HP recover-only monster: any landed attack wins on turn 1.</summary>
    static CombatEncounter MakePushover(bool persistent = false)
    {
        var fight = CmbParser.ParseString("""
            [title Pushover]
            [stats hp=1]

            * move Recover
              narration: It catches its breath.

            * win
            It falls.

            * lose
            You fall.
            """);
        fight.Id = "plains/tier1/pushover";
        fight.Category = "plains/tier1";
        fight.Persistent = persistent;
        return fight;
    }

    static GameSession MakeCombatSession(CombatEncounter fight)
    {
        var session = Helpers.MakeSession(combatBundle: Helpers.MakeCombatBundle(fight));
        // Falchion gives the move pool a basic Attack (bare hands have none).
        session.Player.Pack.Add(new ItemInstance("falchion", "Falchion") { IsEquipped = true });
        return session;
    }

    static void PlayToWin(GameSession session, CombatEncounter fight)
    {
        CombatOrchestrator.Begin(session, fight.Id);
        var attack = Move.Parse("attack");
        for (int i = 0; i < 10 && session.Player.ActiveCombat is { Resolved: false }; i++)
            CombatOrchestrator.Step(session, new PlayerCombatAction.Commit(attack, attack, attack));
        Assert.Equal(SessionMode.Exploring, session.Mode);
    }

    [Fact]
    public void Win_MarksFightUsed()
    {
        var fight = MakePushover();
        var session = MakeCombatSession(fight);

        PlayToWin(session, fight);

        Assert.Contains("fight:plains/tier1/pushover", session.Player.UsedEncounterIds);
    }

    [Fact]
    public void Win_PersistentFightNotMarked()
    {
        var fight = MakePushover(persistent: true);
        var session = MakeCombatSession(fight);

        PlayToWin(session, fight);

        Assert.DoesNotContain("fight:plains/tier1/pushover", session.Player.UsedEncounterIds);
    }

    [Fact]
    public void Flee_DoesNotMarkFightUsed()
    {
        var fight = MakePushover();
        var session = MakeCombatSession(fight);

        CombatOrchestrator.Begin(session, fight.Id);
        CombatOrchestrator.Step(session, new PlayerCombatAction.Flee());

        Assert.Equal(SessionMode.Exploring, session.Mode);
        Assert.DoesNotContain("fight:plains/tier1/pushover", session.Player.UsedEncounterIds);
    }
}
