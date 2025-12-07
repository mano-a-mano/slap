using System;
using Unity.Burst.Intrinsics;
using UnityEngine;

[CreateAssetMenu()]
public class HudSnapshotChannel : ScriptableObject
{
    public event Action<PlayerRuntimeState[]> OnSnapshot;
    public bool HasLast { get; private set; }
    public PlayerRuntimeState[] Last { get; private set; }

    public void Raise(PlayerRuntimeState[] entries)
    {
        Last = entries;
        HasLast = true;
        OnSnapshot?.Invoke(entries);
    }
}
