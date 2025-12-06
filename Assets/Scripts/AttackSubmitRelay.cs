using UnityEngine;

// Listens to AttackSubmitChannel and forwards to CommitBuffer via one-time lookup.
public class AttackSubmitRelay : MonoBehaviour
{
    [Header("Assign")]
    public AttackSubmitChannel attackSubmitChannel;

    private CommitBuffer _buffer; // found once

    private void Awake()
    {
        _buffer = FindFirstObjectByType<CommitBuffer>();
    }

    private void OnEnable()
    {
        if (attackSubmitChannel != null)
            attackSubmitChannel.OnSubmit += OnSubmit;
    }

    private void OnDisable()
    {
        if (attackSubmitChannel != null)
            attackSubmitChannel.OnSubmit -= OnSubmit;
    }

    private void OnSubmit(SlapDirection dir, int power)
    {
        if (_buffer != null)
        {
            _buffer.SubmitAttackFromClient(dir, power);
        }
    }
}
