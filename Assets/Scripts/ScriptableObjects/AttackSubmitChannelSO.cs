using System;
using UnityEngine;

[CreateAssetMenu()]
public class AttackSubmitChannel : ScriptableObject
{
    // (direction, power). In FFA we'll add target later.
    public event Action<SlapDirection, int> OnSubmit;

    public void Raise(SlapDirection dir, int power)
    {
        OnSubmit?.Invoke(dir, power);
    }
}
