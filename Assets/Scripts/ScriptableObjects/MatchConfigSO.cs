using UnityEngine;

[CreateAssetMenu()]
public class MatchConfigSO : ScriptableObject
{
    public enum Mode
    {
        Duel_1v1,
        FFA_4
    }

    [Header("Mode")]
    public Mode mode = Mode.Duel_1v1;

    [Tooltip("Minimum players allowed in this mode (e.g., 2 for 1v1)")]
    public int minPlayers = 2;

    [Tooltip("Maximum players allowed in this mode (e.g., 2 for 1v1, 4 for FFA)")]
    public int maxPlayers = 2;

    [Header("Round Rules")]
    [Tooltip("Power available to each player at the start of a round")]
    public int powerPerRound = 100;

    [Tooltip("Number of slaps each player gets per round")]
    public int slapsPerRound = 3;

    [Tooltip("Seconds the phase UI will allow for input before auto-submit (we will wire this later)")]
    public int roundPhaseTimerSeconds = 20;

    [Header("Match Rules")]
    [Tooltip("Number of sets needed to win the match (best of N sets)")]
    public int bestOfSets = 3;

    [Header("Tie Breaker")]
    [Tooltip("Number of attacks per player during tie-break flurry")]
    public int flurryAttacks = 5;

    [Tooltip("Points for a successful attack during tie-break")]
    public float flurryAttackPoints = 1f;

    [Tooltip("Points for a successful block during tie-break")]
    public float flurryBlockPoints = 0.5f;
}
