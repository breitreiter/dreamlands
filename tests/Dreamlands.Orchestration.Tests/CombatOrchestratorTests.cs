using Dreamlands.Combat;
using Dreamlands.Encounter;
using Dreamlands.Game;
using Dreamlands.Orchestration;

namespace Dreamlands.Orchestration.Tests;

public class CombatOrchestratorTests
{
    /// <summary>1-HP recover-only monster: any landed attack wins on turn 1.</summary>
    static CombatEncounter MakePushover(
        bool persistent = false, string winMech = "", string fleeBlock = "")
    {
        var fight = CmbParser.ParseString($"""
            [title Pushover]
            [stats hp=1]

            * move Recover
              narration: It catches its breath.

            * win
            It falls.
            {winMech}

            * lose
            You fall.

            {fleeBlock}
            """);
        fight.Id = "plains/tier1/pushover";
        fight.Category = "plains/tier1";
        fight.Persistent = persistent;
        return fight;
    }

    static GameSession MakeCombatSession(CombatEncounter fight, EncounterBundle? bundle = null)
    {
        var session = Helpers.MakeSession(bundle: bundle, combatBundle: Helpers.MakeCombatBundle(fight));
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

    [Fact]
    public void Flee_AppliesFleeMechanics()
    {
        var fight = MakePushover(fleeBlock: """
            * flee
            You scramble away.
            +add_tag fled_pushover
            """);
        var session = MakeCombatSession(fight);

        CombatOrchestrator.Begin(session, fight.Id);
        var turn = CombatOrchestrator.Step(session, new PlayerCombatAction.Flee());

        Assert.True(turn.Resolved);
        Assert.Contains(session.Player.Tags, t => t == "fled_pushover");
        var coda = turn.Events.OfType<CombatEvent.Outcome>().Single();
        Assert.Contains("scramble", coda.Text);
    }

    [Fact]
    public void Win_ChainQueuesEncounter()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("Aftermath", "plains/tier1", Trigger: "none"));
        var fight = MakePushover(winMech: "+chain Aftermath");
        var session = MakeCombatSession(fight, bundle);

        PlayToWin(session, fight);

        Assert.Equal("plains/tier1/Aftermath", session.Player.PendingEncounterChain);
    }

    [Fact]
    public void EncounterCombatVerb_FinishesWithCombatStarted()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("Lair", "plains/tier1", Mechanics: ["combat pushover"]));
        var fight = MakePushover();
        var session = MakeCombatSession(fight, bundle);

        var enc = session.Bundle.GetById("plains/tier1/Lair")!;
        var begin = EncounterRunner.Begin(session, enc);
        var result = EncounterRunner.Choose(session, begin.GatedChoices[0].Choice);

        var finished = Assert.IsType<EncounterStep.Finished>(result);
        Assert.Equal(FinishReason.CombatStarted, finished.Reason);
        Assert.Equal("plains/tier1/pushover", finished.NavigateToId);
    }

    [Fact]
    public void Win_ChainResolvesQualifiedId()
    {
        var bundle = Helpers.MakeBundle(
            new Helpers.BundleEntry("Lair", "arcs/forest/the_lodge", Trigger: "none"));
        var fight = MakePushover(winMech: "+chain arcs/forest/the_lodge/Lair");
        var session = MakeCombatSession(fight, bundle);

        PlayToWin(session, fight);

        Assert.Equal("arcs/forest/the_lodge/Lair", session.Player.PendingEncounterChain);
    }
}
