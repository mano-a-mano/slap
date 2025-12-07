using System;
using System.Collections.Generic;

public static class RoundEngine
{
    // Resolves a single "exchange" (one attack from A, defense by B).
    // Returns a RoundEvent and updates the provided player state dictionary.
    public static RoundEvent ResolveExchange_1v1(
        ulong attackerId,
        ulong defenderId,
        SlapDirection attackDir,
        SlapDirection defenseDir,
        int powerCommitted,
        Dictionary<ulong, PlayerRuntimeState> players)
    {
        var psA = players[attackerId];
        var psD = players[defenderId];

        // Spend attacker power & slap (attacker pays power even if blocked)
        int spend = Math.Max(0, powerCommitted);
        spend = Math.Min(spend, psA.PowerLeft);
        psA.PowerLeft -= spend;
        psA.SlapsLeft = Math.Max(0, psA.SlapsLeft - 1);

        // Compute outcome & advantage deltas
        bool blocked = (attackDir == defenseDir);
        float attackerDelta = blocked ? 0f : spend;
        float defenderDelta = blocked ? (spend * 0.5f) : 0f;

        psA.Advantage += attackerDelta;
        psD.Advantage += defenderDelta;

        players[attackerId] = psA;
        players[defenderId] = psD;

        return new RoundEvent
        {
            Attacker = attackerId,
            Defender = defenderId,
            AttackDir = attackDir,
            DefenseDir = defenseDir,
            PowerCommitted = spend,
            Outcome = blocked ? SlapOutcome.Blocked : SlapOutcome.Hit,
            AttackerAdvDelta = attackerDelta,
            DefenderAdvDelta = defenderDelta
        };
    }
}
