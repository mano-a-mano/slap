using UnityEngine;
using UnityEngine.UI;

public class AttackPanelUI : MonoBehaviour
{
    [Header("Channels (assign)")]
    public PhaseChangedChannel phaseChangedChannel;
    public AttackSubmitChannel attackSubmitChannel;

    [Header("UI (assign)")]
    public GameObject panelRoot;   // <-- assign the child panel container
    public Button btnLeft;
    public Button btnUp;
    public Button btnRight;
    public Slider powerSlider;
    public Button btnConfirm;

    private SlapDirection _chosenDir = SlapDirection.Left;

    private void OnEnable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += OnPhaseChanged;

        if (btnLeft != null) btnLeft.onClick.AddListener(() => _chosenDir = SlapDirection.Left);
        if (btnUp != null) btnUp.onClick.AddListener(() => _chosenDir = SlapDirection.Up);
        if (btnRight != null) btnRight.onClick.AddListener(() => _chosenDir = SlapDirection.Right);

        if (btnConfirm != null) btnConfirm.onClick.AddListener(OnConfirmClicked);

        // Initialize from cached phase if available; otherwise start hidden
        if (phaseChangedChannel != null && phaseChangedChannel.HasLastValue)
        {
            OnPhaseChanged(phaseChangedChannel.LastPrevious, phaseChangedChannel.LastCurrent);
        }
        else
        {
            SetPanelVisible(false);
        }
    }

    private void OnDisable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged -= OnPhaseChanged;

        if (btnConfirm != null) btnConfirm.onClick.RemoveAllListeners();
        if (btnLeft != null) btnLeft.onClick.RemoveAllListeners();
        if (btnUp != null) btnUp.onClick.RemoveAllListeners();
        if (btnRight != null) btnRight.onClick.RemoveAllListeners();
    }

    private void OnPhaseChanged(MatchPhase previous, MatchPhase current)
    {
        SetPanelVisible(current == MatchPhase.Attack);
    }

    private void OnConfirmClicked()
    {
        if (attackSubmitChannel == null) return;

        int power = powerSlider != null ? Mathf.RoundToInt(powerSlider.value) : 0;
        if (power < 0) power = 0;

        // Raise to the bus; the relay will turn this into the RPC
        attackSubmitChannel.Raise(_chosenDir, power);

        // Lock the panel locally after submit (but keep the component subscribed)
        SetPanelVisible(false);
    }

    private void SetPanelVisible(bool visible)
    {
        if (panelRoot != null) panelRoot.SetActive(visible);
    }
}
