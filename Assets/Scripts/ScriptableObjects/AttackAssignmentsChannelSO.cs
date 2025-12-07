using System;
using UnityEngine;

// Broadcast, at the start of Defend, who attacks whom.
// For each defender, a list of attacker ClientIds.
[CreateAssetMenu()]
public class AttackAssignmentsChannel : ScriptableObject
{
    // payload: array of entries (Defender, Attackers[])
    public event Action<(ulong defender, ulong[] attackers)[]> OnAssignments;

    public void Raise((ulong defender, ulong[] attackers)[] entries)
    {
        OnAssignments?.Invoke(entries);
    }
}
