public enum SlapOutcome : byte { Hit = 0, Blocked = 1 }

public struct RoundEvent
{
    public ulong Attacker;
    public ulong Defender;
    public SlapDirection AttackDir;
    public SlapDirection DefenseDir;
    public int PowerCommitted;
    public SlapOutcome Outcome;    // Hit or Blocked
    public float AttackerAdvDelta; // +Power if hit, +0 if blocked
    public float DefenderAdvDelta; // +0.5*Power if blocked, +0 if hit
}
