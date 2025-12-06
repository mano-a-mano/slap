using UnityEngine;
using UnityEngine.UI;

public class DefensePanelUI : MonoBehaviour
{
    [Header("Channels (assign)")]
    public PhaseChangedChannel phaseChangedChannel;
    public DefenseSubmitChannel defenseSubmitChannel;

    [Header("UI (assign)")]
    public GameObject panelRoot; // child container to show/hide
    public Button btnLeft;
    public Button btnUp;
    public Button btnRight;
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

        // Start hidden until Defend phase begins
        SetVisible(false);
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
        SetVisible(current == MatchPhase.Defend);
    }

    private void SetVisible(bool v)
    {
        if (panelRoot != null) panelRoot.SetActive(v);
    }

    private void OnConfirmClicked()
    {
        if (defenseSubmitChannel == null) return;
        defenseSubmitChannel.Raise(_chosenDir);
        SetVisible(false);
    }
}
