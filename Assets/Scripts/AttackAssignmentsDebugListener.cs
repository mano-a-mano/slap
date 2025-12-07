using UnityEngine;

public class AttackAssignmentsDebugListener : MonoBehaviour
{
    public AttackAssignmentsChannel channel;

    void OnEnable() { if (channel != null) channel.OnAssignments += Handle; }
    void OnDisable() { if (channel != null) channel.OnAssignments -= Handle; }

    void Handle((ulong defender, ulong[] attackers)[] entries)
    {
        foreach (var e in entries)
            Debug.Log($"[Assignments] Defender {e.defender} attacked by [{string.Join(",", e.attackers)}]");
    }
}
