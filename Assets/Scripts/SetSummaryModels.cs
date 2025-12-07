public struct SetSummary
{
    public int SetIndex;          // 1-based
    public bool IsTie;            // true => flurry tiebreaker later
    public ulong WinnerClientId;  // 0 if tie
    public bool MatchOver;        // true if this set clinched the match
    public ulong MatchWinnerClientId; // 0 if not over yet
}
