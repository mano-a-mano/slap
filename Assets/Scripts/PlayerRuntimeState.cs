using System;

[Serializable]
public struct PlayerRuntimeState
{
    public ulong ClientId;     // network id
    public int PowerLeft;      // starts at 100 per round
    public int SlapsLeft;      // starts at 3 per round
    public float Advantage;    // can be 0.5 increments
    public int SetWins;        // best-of-3
    public string Name;
}
