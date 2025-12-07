using UnityEngine;

public class RoundSummaryDebugListener : MonoBehaviour
{
    public RoundSummaryChannel channel;

    void OnEnable()
    {
        if (channel != null) channel.OnRoundResolved += Handle;
    }
    void OnDisable()
    {
        if (channel != null) channel.OnRoundResolved -= Handle;
    }

    void Handle(RoundEvent[] arr)
    {
        var msg = "[RoundSummary]\n";
        foreach (var e in arr)
        {
            msg += $"  {e.Attacker} - {e.Defender}  dir:{e.AttackDir} vs {e.DefenseDir}  " +
                   $"{e.Outcome}  P:{e.PowerCommitted}  Adv A:{e.AttackerAdvDelta} D:{e.DefenderAdvDelta}\n";
        }
        Debug.Log(msg);
    }
}
