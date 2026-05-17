using Dreamlands.Rules;

namespace Dreamlands.Game;

// RollMode and SkillCheckResult retained for server/GameFunctions.cs compat — Phase 5 will remove them.

public enum RollMode { Normal, Advantage, Disadvantage }

/// <summary>Result of a skill check roll (retained for server compat; d20 roll path removed).</summary>
public record SkillCheckResult(
    bool Passed, int Rolled, int Target, int Modifier,
    int SkillLevel, Skill Skill,
    RollMode RollMode = RollMode.Normal,
    int NaturalRoll = 0,
    bool WasLuckyReroll = false,
    bool IsMeetsCheck = false);

// SkillChecks: stub class retained only for SkillCheckResult/RollMode types used by the server
// and orchestration layer. The actual gear-bonus and d20 roll methods are removed.
