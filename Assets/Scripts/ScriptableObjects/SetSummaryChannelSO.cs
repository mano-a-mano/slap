using System;
using UnityEngine;

[CreateAssetMenu()]
public class SetSummaryChannel : ScriptableObject
{
    public event Action<SetSummary> OnSetResolved;
    public void Raise(SetSummary s) => OnSetResolved?.Invoke(s);
}
