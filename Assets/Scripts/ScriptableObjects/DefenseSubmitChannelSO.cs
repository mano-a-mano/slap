using System;
using UnityEngine;

[CreateAssetMenu()]
public class DefenseSubmitChannel : ScriptableObject
{
    // 1v1 payload: just the block direction.
    public event Action<SlapDirection> OnSubmit;

    public void Raise(SlapDirection dir)
    {
        OnSubmit?.Invoke(dir);
    }
}
