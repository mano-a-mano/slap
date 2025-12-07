using UnityEngine;
using TMPro;

public class SetBannerUI : MonoBehaviour
{
    [Header("Channels")]
    public SetSummaryChannel setSummaryChannel;
    public PhaseChangedChannel phaseChangedChannel;

    [Header("UI")]
    public GameObject root; // panel container
    public TextMeshProUGUI textLine;

    private void OnEnable()
    {
        if (setSummaryChannel != null) setSummaryChannel.OnSetResolved += OnSetResolved;
        if (phaseChangedChannel != null) phaseChangedChannel.OnPhaseChanged += OnPhaseChanged;
        SetVisible(false);
    }

    private void OnDisable()
    {
        if (setSummaryChannel != null) setSummaryChannel.OnSetResolved -= OnSetResolved;
        if (phaseChangedChannel != null) phaseChangedChannel.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnSetResolved(SetSummary s)
    {
        if (textLine == null) return;

        if (s.MatchOver)
        {
            textLine.text = $"Set {s.SetIndex} complete • MATCH OVER\nWinner: {s.MatchWinnerClientId}";
        }
        else if (s.IsTie)
        {
            textLine.text = $"Set {s.SetIndex} is a TIE — Flurry incoming!";
        }
        else
        {
            textLine.text = $"Set {s.SetIndex} won by {s.WinnerClientId}";
        }
        SetVisible(true);
    }

    private void OnPhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        // Hide banner when we leave Transition or enter a new Attack
        if (curr == MatchPhase.Attack)
            SetVisible(false);
    }

    private void SetVisible(bool v)
    {
        if (root != null) root.SetActive(v);
    }
}
