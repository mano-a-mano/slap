using UnityEngine;

public class HudDebugListener : MonoBehaviour
{
    public HudSnapshotChannel channel;

    void OnEnable()
    {
        if (channel != null) channel.OnSnapshot += Handle;
    }
    void OnDisable()
    {
        if (channel != null) channel.OnSnapshot -= Handle;
    }

    void Handle(PlayerRuntimeState[] entries)
    {
        // Simple proof in the Console; replace later with a real HUD
        var msg = "[HUD] " + entries.Length + " players:";
        foreach (var e in entries)
            msg += $"  ({e.ClientId}) Power:{e.PowerLeft} Slaps:{e.SlapsLeft} Adv:{e.Advantage} Sets:{e.SetWins}";
        Debug.Log(msg);
    }
}
