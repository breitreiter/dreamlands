using Dreamlands.Game;

namespace Dreamlands.Orchestration;

public abstract record EncounterStep
{
    public record ShowEncounter(Encounter.Encounter Encounter, List<Encounter.Choice> VisibleChoices) : EncounterStep;
    public record ShowOutcome(ResolvedChoice Resolved, List<MechanicResult> Results) : EncounterStep;
    public record Finished(FinishReason Reason, string? NavigateToId = null, ShowOutcome? Outcome = null) : EncounterStep;
}

public enum FinishReason { Completed, NavigatedTo, DungeonFinished, DungeonFled, PlayerDied }

public static class EncounterRunner
{
    public static EncounterStep.ShowEncounter Begin(GameSession session, Encounter.Encounter encounter)
    {
        session.Mode = SessionMode.InEncounter;
        session.CurrentEncounter = encounter;
        session.Player.CurrentEncounterId = encounter.Id;
        session.Player.UsedEncounterIds.Add(encounter.Id);
        var visible = Choices.GetVisible(encounter, session.Player, session.Balance);
        return new EncounterStep.ShowEncounter(encounter, visible);
    }

    public static EncounterStep Choose(GameSession session, Encounter.Choice choice)
    {
        var resolved = Choices.Resolve(choice, session.Player, session.Balance, session.Rng);
        var results = Mechanics.Apply(resolved.Mechanics, session.Player, session.Balance, session.Rng);

        var outcome = new EncounterStep.ShowOutcome(resolved, results);

        // Check for terminal results
        foreach (var r in results)
        {
            if (r is MechanicResult.Navigation nav)
                return new EncounterStep.Finished(FinishReason.NavigatedTo, nav.EncounterId, Outcome: outcome);
            if (r is MechanicResult.DungeonFinished)
            {
                session.Mode = SessionMode.Exploring;
                session.CurrentEncounter = null;
                session.Player.CurrentEncounterId = null;
                return new EncounterStep.Finished(FinishReason.DungeonFinished, Outcome: outcome);
            }
            if (r is MechanicResult.DungeonFled)
            {
                session.Mode = SessionMode.Exploring;
                session.CurrentEncounter = null;
                session.Player.CurrentEncounterId = null;
                return new EncounterStep.Finished(FinishReason.DungeonFled, Outcome: outcome);
            }
        }

        if (session.Player.Health <= 0)
        {
            session.Mode = SessionMode.GameOver;
            session.CurrentEncounter = null;
            session.Player.CurrentEncounterId = null;
            return new EncounterStep.Finished(FinishReason.PlayerDied, Outcome: outcome);
        }

        return outcome;
    }

    public static void EndEncounter(GameSession session)
    {
        session.Mode = SessionMode.Exploring;
        session.CurrentEncounter = null;
        session.Player.CurrentEncounterId = null;
        session.SkipEncounterTrigger = true;
    }
}
