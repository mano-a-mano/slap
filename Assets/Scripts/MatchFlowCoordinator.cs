using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class MatchFlowCoordinator : NetworkBehaviour
{
    public NetworkVariable<MatchPhase> CurrentPhase =
        new NetworkVariable<MatchPhase>(MatchPhase.Waiting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    [Header("Config (optional)")]
    public MatchConfigSO matchConfig;
    
    [Header("Event Channel (HUD)")]
    public HudSnapshotChannel hudSnapshotChannel;
    
    // server-only player state
    private readonly Dictionary<ulong, PlayerRuntimeState> _players = new Dictionary<ulong, PlayerRuntimeState>();
    
    [Header("Event Channel (assign asset)")]
    public PhaseChangedChannel phaseChangedChannel;

    [Header("Phase durations (seconds)")]
    public float attackSeconds = 3f;
    public float defendSeconds = 3f;
    public float resolveSeconds = 2f;
    public float transitionSeconds = 2f;

    float _timer;
    bool _started;

    [Header("Attack phase completion (assign)")]
    public AttackAllSubmittedChannel attackAllSubmittedChannel;

    [Header("Defense phase completion (assign)")]
    public DefenseAllSubmittedChannel defenseAllSubmittedChannel;

    public override void OnNetworkSpawn()
    {
        CurrentPhase.OnValueChanged += HandlePhaseChanged;

        PublishCurrentPhaseToChannel();

        if (IsServer)
        {
            // Seed timers from config (optional)
            if (matchConfig != null)
            {
                int t = Mathf.Max(1, matchConfig.roundPhaseTimerSeconds);
                attackSeconds = t;
                defendSeconds = t;
                resolveSeconds = 2f;
                transitionSeconds = 2f;
            }

            // Make sure we start in Waiting and DO NOT start phases yet
            CurrentPhase.Value = MatchPhase.Waiting;
            _started = false;

            // Subscribe to KitchenGameManager's state changes
            if (KitchenGameManager.Instance != null)
            {
                KitchenGameManager.Instance.OnStateChanged += Kgm_OnStateChanged;

                // If countdown already finished (e.g., late-joined host tools), start immediately
                if (KitchenGameManager.Instance.IsGamePlaying())
                    BeginAttack();
            }
            else
            {
                Debug.LogWarning("[MatchFlowCoordinator] KitchenGameManager not found at spawn; will remain in Waiting.");
            }

            if (attackAllSubmittedChannel != null)
            {
                attackAllSubmittedChannel.OnAllSubmitted += OnAllAttacksSubmitted;
            }

            if (defenseAllSubmittedChannel != null)
            {
                defenseAllSubmittedChannel.OnAllSubmitted += OnAllDefensesSubmitted;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentPhase.OnValueChanged -= HandlePhaseChanged;

        if (IsServer && KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnStateChanged -= Kgm_OnStateChanged;
        }

        if (IsServer && attackAllSubmittedChannel != null)
        {
            attackAllSubmittedChannel.OnAllSubmitted -= OnAllAttacksSubmitted;
        }

        if (IsServer && defenseAllSubmittedChannel != null)
        {
            defenseAllSubmittedChannel.OnAllSubmitted -= OnAllDefensesSubmitted;
        }
    }

    void Kgm_OnStateChanged(object sender, System.EventArgs e)
    {
        if (!_started && KitchenGameManager.Instance.IsGamePlaying())
        {
            // Initialize server-side player states once at match start
            InitializePlayerStates();

            // Push an initial HUD snapshot so clients can render immediately
            PublishHudSnapshot();

            BeginAttack(); // first real phase starts only after states exist
        }
    }

    void InitializePlayerStates()
    {
        _players.Clear();

        // Round defaults come from MatchConfig (or hard-coded)
        int power = matchConfig != null ? matchConfig.powerPerRound : 100;
        int slaps = matchConfig != null ? matchConfig.slapsPerRound : 3;

        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            _players[clientId] = new PlayerRuntimeState
            {
                ClientId = clientId,
                PowerLeft = power,
                SlapsLeft = slaps,
                Advantage = 0f,
                SetWins = 0
            };
        }
    }

    void PublishHudSnapshot()
    {
        if (hudSnapshotChannel == null) return;
        var arr = new PlayerRuntimeState[_players.Count];
        int i = 0;
        foreach (var kv in _players) arr[i++] = kv.Value;
        hudSnapshotChannel.Raise(arr);
    }

    void HandlePhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.Raise(prev, curr);
    }

    void PublishCurrentPhaseToChannel()
    {
        if (phaseChangedChannel != null)
        {
            var p = CurrentPhase.Value;
            phaseChangedChannel.Raise(p, p); // seed cache + notify current
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (_timer <= 0f) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f) AdvancePhase();
    }

    // ----- server-only -----
    void BeginAttack()
    {
        if (_started) return;
        _started = true;

        CurrentPhase.Value = MatchPhase.Attack;
        _timer = Mathf.Max(0.1f, attackSeconds);

        // optional: log to verify timers
        // Debug.Log($"[MatchFlowCoordinator] BeginAttack: attack={attackSeconds}, defend={defendSeconds}, resolve={resolveSeconds}, transition={transitionSeconds}");
    }

    private void OnAllAttacksSubmitted()
    {
        if (!IsServer) return;
        if (CurrentPhase.Value != MatchPhase.Attack) return;

        // Advance immediately to Defend (don’t wait out the timer)
        CurrentPhase.Value = MatchPhase.Defend;
        Debug.Log("Move to Defend");

        // Reset the timer for Defend duration
        _timer = Mathf.Max(0.1f, defendSeconds);
    }

    private void OnAllDefensesSubmitted()
    {
        if (!IsServer) return;
        if (CurrentPhase.Value != MatchPhase.Defend) return;

        // Advance immediately to Resolve
        CurrentPhase.Value = MatchPhase.Resolve;
        _timer = Mathf.Max(0.1f, resolveSeconds);
    }


    void AdvancePhase()
    {
        switch (CurrentPhase.Value)
        {
            case MatchPhase.Attack:
                CurrentPhase.Value = MatchPhase.Defend;
                _timer = Mathf.Max(0.1f, defendSeconds);
                break;

            case MatchPhase.Defend:
                CurrentPhase.Value = MatchPhase.Resolve;
                _timer = Mathf.Max(0.1f, resolveSeconds);
                break;

            case MatchPhase.Resolve:
                CurrentPhase.Value = MatchPhase.Transition;
                _timer = Mathf.Max(0.1f, transitionSeconds);
                break;

            case MatchPhase.Transition:
                // loop for now
                CurrentPhase.Value = MatchPhase.Attack;
                _timer = Mathf.Max(0.1f, attackSeconds);
                break;
        }
    }
}
