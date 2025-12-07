using System;
using UnityEngine;

[CreateAssetMenu()]
public class RoundSummaryChannel : ScriptableObject
{
    public event Action<RoundEvent[]> OnRoundResolved;

    public void Raise(RoundEvent[] eventsArray)
    {
        OnRoundResolved?.Invoke(eventsArray);
    }
}
