using System;
using UnityEngine;

[CreateAssetMenu()]
public class DefenseAllSubmittedChannel : ScriptableObject
{
    public event Action OnAllSubmitted;
    public void Raise() => OnAllSubmitted?.Invoke();
}
