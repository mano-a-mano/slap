using UnityEngine;
using TMPro;

public class PhaseUI : MonoBehaviour
{

    public PhaseChangedChannel phaseChangedChannel;
    public TextMeshProUGUI textPhase;

    private void OnEnable()
    {
        if (phaseChangedChannel != null)
        {
            phaseChangedChannel.OnPhaseChanged += HandlePhaseChanged;

            if (phaseChangedChannel.HasLastValue)
            {
                HandlePhaseChanged(phaseChangedChannel.LastPrevious, phaseChangedChannel.LastCurrent);
                return;
            }
        }

        if (textPhase != null) textPhase.text = "Phase: ...";
    }


    private void OnDisable()
    {
        if (phaseChangedChannel != null)
        {
            phaseChangedChannel.OnPhaseChanged -= HandlePhaseChanged;
        }
    }

    private void HandlePhaseChanged(MatchPhase previous, MatchPhase current)
    {
        if (textPhase != null)
        {
            textPhase.text = "Phase: " + current;
        }
        // Example of show/hide by enum:
        // attackPanel.SetActive(current == MatchPhase.Attack);
        // defendPanel.SetActive(current == MatchPhase.Defend);
        // etc.
    }
}
