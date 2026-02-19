using Dreamlands.Orchestration;

namespace DreamlandsCli;

static class EncounterMode
{
    public static void StartEncounter(GameSession session, Dreamlands.Encounter.Encounter encounter)
    {
        var step = EncounterRunner.Begin(session, encounter);
        ShowEncounter(session, step);
    }

    public static void Run(GameSession session)
    {
        // Re-entering encounter mode after navigation
        if (session.CurrentEncounter == null)
        {
            EncounterRunner.EndEncounter(session);
            return;
        }
        var visible = Dreamlands.Game.Choices.GetVisible(session.CurrentEncounter, session.Player, session.Balance);
        ShowEncounter(session, new EncounterStep.ShowEncounter(session.CurrentEncounter, visible));
    }

    static void ShowEncounter(GameSession session, EncounterStep.ShowEncounter step)
    {
        Console.WriteLine();
        Display.WriteLn($"  {step.Encounter.Title}", ConsoleColor.White);
        Console.WriteLine();

        if (!string.IsNullOrWhiteSpace(step.Encounter.Body))
        {
            foreach (var line in step.Encounter.Body.Split('\n'))
                Console.WriteLine($"  {line}");
            Console.WriteLine();
        }

        if (step.VisibleChoices.Count == 0)
        {
            Display.WriteLn("  (No choices available — encounter ends)", ConsoleColor.DarkGray);
            EncounterRunner.EndEncounter(session);
            return;
        }

        for (int i = 0; i < step.VisibleChoices.Count; i++)
        {
            var choice = step.VisibleChoices[i];
            var label = choice.OptionLink ?? choice.OptionText;
            Display.WriteLn($"  [{i + 1}] {label}", ConsoleColor.Cyan);
            if (choice.OptionPreview != null && choice.OptionLink != null)
                Display.WriteLn($"      {choice.OptionPreview}", ConsoleColor.DarkGray);
        }

        while (true)
        {
            Console.Write("\n  Choice> ");
            var key = Console.ReadKey(intercept: true);
            Console.WriteLine();

            if (!int.TryParse(key.KeyChar.ToString(), out var choiceNum) || choiceNum < 1 || choiceNum > step.VisibleChoices.Count)
            {
                Display.WriteLn($"  Enter 1-{step.VisibleChoices.Count}", ConsoleColor.DarkGray);
                continue;
            }

            var chosen = step.VisibleChoices[choiceNum - 1];
            var result = EncounterRunner.Choose(session, chosen);
            HandleResult(session, result);
            return;
        }
    }

    static void HandleResult(GameSession session, EncounterStep result)
    {
        switch (result)
        {
            case EncounterStep.ShowOutcome outcome:
                Console.WriteLine();
                if (outcome.Resolved.Preamble != null)
                {
                    foreach (var line in outcome.Resolved.Preamble.Split('\n'))
                        Console.WriteLine($"  {line}");
                    Console.WriteLine();
                }
                if (outcome.Resolved.CheckResult is { } check)
                    Display.WriteSkillCheck(check);

                foreach (var line in outcome.Resolved.Text.Split('\n'))
                    Console.WriteLine($"  {line}");

                if (outcome.Results.Count > 0)
                {
                    Console.WriteLine();
                    Display.WriteMechanicResults(outcome.Results);
                }

                Display.WriteStatusBar(session);

                // After showing outcome, offer to continue or end
                Console.Write("\n  [Press any key to continue] ");
                Console.ReadKey(intercept: true);
                Console.WriteLine();
                EncounterRunner.EndEncounter(session);
                break;

            case EncounterStep.Finished finished:
                switch (finished.Reason)
                {
                    case FinishReason.NavigatedTo:
                        var next = EncounterSelection.ResolveNavigation(session, finished.NavigateToId!);
                        if (next != null)
                        {
                            StartEncounter(session, next);
                            return;
                        }
                        Display.WriteLn("  (Navigation target not found — encounter ends)", ConsoleColor.DarkGray);
                        EncounterRunner.EndEncounter(session);
                        break;
                    case FinishReason.DungeonFinished:
                        Display.WriteLn("\n  You emerge victorious from the dungeon!", ConsoleColor.Green);
                        Display.WriteStatusBar(session);
                        break;
                    case FinishReason.DungeonFled:
                        Display.WriteLn("\n  You flee the dungeon, battered but alive.", ConsoleColor.Yellow);
                        Display.WriteStatusBar(session);
                        break;
                    case FinishReason.PlayerDied:
                        Display.WriteLn("\n  Darkness claims you...", ConsoleColor.Red);
                        break;
                    case FinishReason.Completed:
                        EncounterRunner.EndEncounter(session);
                        break;
                }
                break;
        }
    }
}
