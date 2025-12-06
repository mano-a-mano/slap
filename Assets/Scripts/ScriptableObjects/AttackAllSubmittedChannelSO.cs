using System;
using UnityEngine;

[CreateAssetMenu()]
public class AttackAllSubmittedChannel : ScriptableObject
{
    public event Action OnAllSubmitted;

    public void Raise()
    {
        OnAllSubmitted?.Invoke();
    }
}
