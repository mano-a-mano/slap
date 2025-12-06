using System;
using UnityEngine;

[CreateAssetMenu()]
public class HudSnapshotChannel : ScriptableObject
{
    public event Action<PlayerRuntimeState[]> OnSnapshot;

    public void Raise(PlayerRuntimeState[] entries)
    {
        OnSnapshot?.Invoke(entries);
    }
}
