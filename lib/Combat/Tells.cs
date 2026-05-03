using Dreamlands.Encounter;

namespace Dreamlands.Combat;

/// <summary>
/// One-line read of the AI's three-slot commitment. First match wins — see
/// super_rps.md § Tell Logic for the canonical table.
/// </summary>
public static class Tells
{
    public static string For(IReadOnlyList<Move> aiCommit, string enemyName)
    {
        if (aiCommit.Any(m => m.Base == "attack" && m.Has("telegraphed")))
            return $"{enemyName} is preparing a heavy attack!";
        if (aiCommit.Count(m => m.Base == "attack") >= 2)
            return $"{enemyName} is pressing the attack";
        if (aiCommit.Count(m => m.Base == "defend") >= 2)
            return $"{enemyName} is on their back foot";
        if (aiCommit.Count(m => m.Base == "recover") >= 2)
            return $"{enemyName} is winded";
        return $"{enemyName} is wary and awaits your move";
    }
}
