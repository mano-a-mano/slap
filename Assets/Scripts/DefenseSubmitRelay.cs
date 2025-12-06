using UnityEngine;

public class DefenseSubmitRelay : MonoBehaviour
{
    [Header("Assign")]
    public DefenseSubmitChannel defenseSubmitChannel;

    private CommitBuffer _buffer;

    private void Awake()
    {
        _buffer = FindFirstObjectByType<CommitBuffer>();
    }

    private void OnEnable()
    {
        if (defenseSubmitChannel != null)
            defenseSubmitChannel.OnSubmit += OnSubmit;
    }

    private void OnDisable()
    {
        if (defenseSubmitChannel != null)
            defenseSubmitChannel.OnSubmit -= OnSubmit;
    }

    private void OnSubmit(SlapDirection dir)
    {
        if (_buffer != null)
        {
            _buffer.SubmitDefenseFromClient(dir);
        }
    }
}
