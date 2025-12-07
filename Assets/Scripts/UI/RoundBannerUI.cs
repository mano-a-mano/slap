using UnityEngine;
using TMPro;

public class RoundBannerUI : MonoBehaviour
{
    [Header("Channels (assign)")]
    public RoundSummaryChannel roundSummaryChannel;
    public PhaseChangedChannel phaseChangedChannel;

    [Header("UI (assign)")]
    public TextMeshProUGUI textBanner;
    public GameObject root; // container to show/hide during Resolve

    private void OnEnable()
    {
        if (roundSummaryChannel != null)
            roundSummaryChannel.OnRoundResolved += HandleRoundResolved;

        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += HandlePhaseChanged;

        SetVisible(false);
    }

    private void OnDisable()
    {
        if (roundSummaryChannel != null)
            roundSummaryChannel.OnRoundResolved -= HandleRoundResolved;

        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged -= HandlePhaseChanged;
    }

    private void HandlePhaseChanged(MatchPhase previous, MatchPhase current)
    {
        // Only show during Resolve so it feels like a “replay”
        SetVisible(current == MatchPhase.Resolve);
        if (current != MatchPhase.Resolve && textBanner != null)
            textBanner.text = "";
    }

    private void HandleRoundResolved(RoundEvent[] arr)
    {
        if (textBanner == null) return;

        // Simple text description; you can replace with icons later
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (var e in arr)
        {
            var outcome = (e.Outcome == SlapOutcome.Hit) ? "HIT" : "BLOCK";
            sb.AppendLine(
                $"Atk:{e.Attacker} -> Def:{e.Defender}  Dir:{e.AttackDir} vs {e.DefenseDir}  {outcome}  P:{e.PowerCommitted}"
            );
        }
        textBanner.text = sb.ToString();
    }

    private void SetVisible(bool v)
    {
        if (root != null) root.SetActive(v);
    }
}
