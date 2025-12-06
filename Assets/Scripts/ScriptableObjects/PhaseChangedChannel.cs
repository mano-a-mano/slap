using System;
using UnityEngine;

[CreateAssetMenu()]
public class PhaseChangedChannel : ScriptableObject
{
    public event Action<MatchPhase, MatchPhase> OnPhaseChanged;

    // Cache last phases so late listeners can initialize immediately
    public bool HasLastValue { get; private set; }
    public MatchPhase LastPrevious { get; private set; }
    public MatchPhase LastCurrent { get; private set; }

    public void Raise(MatchPhase previous, MatchPhase current)
    {
        HasLastValue = true;
        LastPrevious = previous;
        LastCurrent = current;
        OnPhaseChanged?.Invoke(previous, current);
    }
}
