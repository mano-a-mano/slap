using UnityEngine;

public class AttackSubmitRelay_FFA : MonoBehaviour
{
    public AttackSubmitChannel_FFA attackSubmitChannel;

    private CommitBuffer _buffer;

    void Awake()
    {
        _buffer = FindFirstObjectByType<CommitBuffer>();
    }

    void OnEnable()
    {
        if (attackSubmitChannel != null)
            attackSubmitChannel.OnSubmit += OnSubmit;
    }

    void OnDisable()
    {
        if (attackSubmitChannel != null)
            attackSubmitChannel.OnSubmit -= OnSubmit;
    }

    void OnSubmit(SlapDirection dir, int power, ulong targetClientId)
    {
        if (_buffer != null)
            _buffer.SubmitAttackFromClient_FFA(dir, power, targetClientId);
    }
}
