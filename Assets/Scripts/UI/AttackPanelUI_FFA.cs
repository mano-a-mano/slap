using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Collections;

public class AttackPanelUI_FFA : MonoBehaviour
{
    [Header("Channels (optional)")]
    public PhaseChangedChannel phaseChangedChannel;              // show/hide
    public HudSnapshotChannel hudSnapshotChannel;               // optional: not required now
    public AttackSubmitChannel_FFA attackSubmitChannel;

    [Header("UI")]
    public GameObject panelRoot;   // container to toggle
    public Button btnLeft, btnUp, btnRight;
    public Slider powerSlider;
    public Dropdown targetDropdown;     // or TMP_Dropdown if you prefer
    public Button btnConfirm;

    private SlapDirection _dir = SlapDirection.Left;
    private readonly List<ulong> _targets = new();
    private ulong _selfId;

    private KitchenGameMultiplayer _kgm;

    private void Awake()
    {
        _kgm = KitchenGameMultiplayer.Instance; // safe to cache; it’s DontDestroyOnLoad
    }

    private void OnEnable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += OnPhaseChanged;

        if (hudSnapshotChannel != null)
            hudSnapshotChannel.OnSnapshot += OnHudSnapshot; // optional refresh

        if (_kgm != null)
            _kgm.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;

        if (btnLeft) btnLeft.onClick.AddListener(() => _dir = SlapDirection.Left);
        if (btnUp) btnUp.onClick.AddListener(() => _dir = SlapDirection.Up);
        if (btnRight) btnRight.onClick.AddListener(() => _dir = SlapDirection.Right);
        if (btnConfirm) btnConfirm.onClick.AddListener(OnConfirm);

        SetVisible(false);

        if (targetDropdown) targetDropdown.ClearOptions();
        RebuildFromPlayerDataList();
    }

    private void OnDisable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged -= OnPhaseChanged;

        if (hudSnapshotChannel != null)
            hudSnapshotChannel.OnSnapshot -= OnHudSnapshot;

        if (_kgm != null)
            _kgm.OnPlayerDataNetworkListChanged -= OnPlayerDataNetworkListChanged;

        if (btnLeft) btnLeft.onClick.RemoveAllListeners();
        if (btnUp) btnUp.onClick.RemoveAllListeners();
        if (btnRight) btnRight.onClick.RemoveAllListeners();
        if (btnConfirm) btnConfirm.onClick.RemoveAllListeners();
    }

    private void OnPhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        bool show = (curr == MatchPhase.Attack);
        SetVisible(show);
        if (show)
            RebuildFromPlayerDataList(); // ensure fresh names & membership
    }

    // Optional: also rebuild when your HUD snapshot arrives
    private void OnHudSnapshot(PlayerRuntimeState[] _)
    {
        RebuildFromPlayerDataList();
    }

    private void OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        RebuildFromPlayerDataList();
    }

    private void SetVisible(bool v)
    {
        if (panelRoot) panelRoot.SetActive(v);
    }

    private void RebuildFromPlayerDataList()
    {
        _selfId = NetworkManager.Singleton ? NetworkManager.Singleton.LocalClientId : 0;

        var options = new List<Dropdown.OptionData>();
        _targets.Clear();

        if (_kgm != null)
        {
            // Iterate known slots; only add connected entries
            for (int i = 0; i < KitchenGameMultiplayer.MAX_PLAYER_AMOUNT; i++)
            {
                if (!_kgm.IsPlayerIndexConnected(i)) continue;

                var pd = _kgm.GetPlayerDataFromPlayerIndex(i); // has clientId and playerName (FixedString64Bytes)
                if (pd.clientId == _selfId) continue;          // cannot target self

                string label;
                FixedString64Bytes fs = pd.playerName;
                label = fs.Length > 0 ? fs.ToString() : $"Player {pd.clientId}";

                _targets.Add(pd.clientId);
                options.Add(new Dropdown.OptionData(label));
            }
        }

        if (targetDropdown != null)
        {
            targetDropdown.options = options;
            targetDropdown.value = Mathf.Clamp(targetDropdown.value, 0, Mathf.Max(0, options.Count - 1));
            targetDropdown.RefreshShownValue();
        }

        if (btnConfirm != null)
            btnConfirm.interactable = (_targets.Count > 0);
    }

    private void OnConfirm()
    {
        if (attackSubmitChannel == null || targetDropdown == null) return;
        if (_targets.Count == 0) return;

        int idx = Mathf.Clamp(targetDropdown.value, 0, Mathf.Max(0, _targets.Count - 1));
        ulong targetId = _targets[idx];

        int power = powerSlider ? Mathf.RoundToInt(powerSlider.value) : 0;
        if (power < 0) power = 0;

        attackSubmitChannel.Raise(_dir, power, targetId);
        SetVisible(false);
    }
}
