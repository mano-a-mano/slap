using System;
using UnityEngine;

[CreateAssetMenu()]
public class AttackSubmitChannel_FFA : ScriptableObject
{
    // (direction, power, targetClientId)
    public event Action<SlapDirection, int, ulong> OnSubmit;

    public void Raise(SlapDirection dir, int power, ulong targetClientId)
    {
        OnSubmit?.Invoke(dir, power, targetClientId);
    }
}
