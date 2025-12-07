using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using WebSocketSharp;

[RequireComponent(typeof(NetworkObject))]
public class MatchFlowCoordinator : NetworkBehaviour
{
    public NetworkVariable<MatchPhase> CurrentPhase =
        new NetworkVariable<MatchPhase>(MatchPhase.Waiting,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    [Header("Config (optional)")]
    public MatchConfigSO matchConfig;

    [Header("Event Channels")]
    public PhaseChangedChannel phaseChangedChannel;
    public HudSnapshotChannel hudSnapshotChannel;
    public RoundSummaryChannel roundSummaryChannel;
    public AttackAllSubmittedChannel attackAllSubmittedChannel;
    public DefenseAllSubmittedChannel defenseAllSubmittedChannel;

    [Header("FFA assignments (assign)")]
    public AttackAssignmentsChannel attackAssignmentsChannel;

    [Header("Set summary")]
    public SetSummaryChannel setSummaryChannel;

    [Header("Phase durations (seconds)")]
    public float attackSeconds = 3f;
    public float defendSeconds = 3f;
    public float resolveSeconds = 30f;
    public float transitionSeconds = 2f;

    float _timer;
    bool _started;

    // server state
    private readonly Dictionary<ulong, PlayerRuntimeState> _players = new();
    private CommitBuffer _buffer;

    // set/match tracking
    private int _setsToWin = 2;
    private int _powerPerSet = 100;
    private int _slapsPerSet = 3;
    private int _currentSetIndex = 1;

    private bool _endSetPending;
    private bool _matchOver;
    private ulong _lastSetWinner; // undefined on tie

    // scratch for resolve
    private readonly List<RoundEvent> _roundEvents = new();

    public override void OnNetworkSpawn()
    {
        CurrentPhase.OnValueChanged += HandlePhaseChanged;
        PublishCurrentPhaseToChannel();

        if (IsServer)
        {
            if (matchConfig != null)
            {
                int t = Mathf.Max(1, matchConfig.roundPhaseTimerSeconds);
                attackSeconds = t; defendSeconds = t;
                resolveSeconds = 2f; transitionSeconds = 2f;

                _setsToWin = Mathf.Max(1, (matchConfig.bestOfSets / 2) + 1);
                _powerPerSet = Mathf.Max(1, matchConfig.powerPerRound);
                _slapsPerSet = Mathf.Max(1, matchConfig.slapsPerRound);
                _currentSetIndex = 1;
            }

            _buffer = FindFirstObjectByType<CommitBuffer>();

            CurrentPhase.Value = MatchPhase.Waiting;
            _started = false; _endSetPending = false; _matchOver = false;

            if (KitchenGameManager.Instance != null)
            {
                KitchenGameManager.Instance.OnStateChanged += Kgm_OnStateChanged;
                if (KitchenGameManager.Instance.IsGamePlaying())
                    BeginAttack();
            }
            else
            {
                Debug.LogWarning("[MatchFlowCoordinator] KitchenGameManager not found; remaining in Waiting.");
            }

            if (attackAllSubmittedChannel != null)
                attackAllSubmittedChannel.OnAllSubmitted += OnAllAttacksSubmitted;
            if (defenseAllSubmittedChannel != null)
                defenseAllSubmittedChannel.OnAllSubmitted += OnAllDefensesSubmitted;
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentPhase.OnValueChanged -= HandlePhaseChanged;

        if (IsServer)
        {
            if (KitchenGameManager.Instance != null)
                KitchenGameManager.Instance.OnStateChanged -= Kgm_OnStateChanged;

            if (attackAllSubmittedChannel != null)
                attackAllSubmittedChannel.OnAllSubmitted -= OnAllAttacksSubmitted;
            if (defenseAllSubmittedChannel != null)
                defenseAllSubmittedChannel.OnAllSubmitted -= OnAllDefensesSubmitted;
        }
    }

    void Kgm_OnStateChanged(object sender, System.EventArgs e)
    {
        if (!_started && KitchenGameManager.Instance.IsGamePlaying())
        {
            InitializePlayerStates();
            PublishHudSnapshot();
            BeginAttack();
        }
    }

    void HandlePhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        phaseChangedChannel?.Raise(prev, curr);

        if (IsServer && curr == MatchPhase.Resolve)
        {
            DoResolve_1v1();
            EvaluateSetEndOrEarlyEnd();  // <- NEW
            PublishHudSnapshot();
            PublishRoundSummary();
        }
    }

    void PublishCurrentPhaseToChannel()
    {
        phaseChangedChannel?.Raise(CurrentPhase.Value, CurrentPhase.Value);
    }

    void Update()
    {
        if (!IsServer) return;
        if (_timer <= 0f) return;

        _timer -= Time.deltaTime;
        if (_timer <= 0f) AdvancePhase();
    }

    // ---------- server helpers ----------
    void BeginAttack()
    {
        if (_started && CurrentPhase.Value == MatchPhase.Waiting) _started = false;
        if (_started) return;
        _started = true;

        CurrentPhase.Value = MatchPhase.Attack;
        _timer = Mathf.Max(0.1f, attackSeconds);
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
                if (_matchOver)
                {
                    CurrentPhase.Value = MatchPhase.MatchOver;
                    _timer = 0f;
                }
                else
                {
                    if (_endSetPending)
                    {
                        ResetForNextSet();
                        _endSetPending = false;
                    }
                    CurrentPhase.Value = MatchPhase.Attack;
                    _timer = Mathf.Max(0.1f, attackSeconds);
                }
                break;
        }
    }

    void InitializePlayerStates()
    {
        _players.Clear();

        foreach (var id in Unity.Netcode.NetworkManager.Singleton.ConnectedClientsIds)
        {
            string displayName = $"Player {id}";

            var kgm = KitchenGameMultiplayer.Instance;
            if (kgm != null)
            {
                var pd = kgm.GetPlayerDataFromClientId(id);
                // pd.playerName is FixedString64Bytes -> use Length/ToString()
                if (pd.playerName.Length > 0)              // or: !pd.playerName.IsEmpty
                    displayName = pd.playerName.ToString(); // convert to C# string
            }

            _players[id] = new PlayerRuntimeState
            {
                ClientId = id,
                PowerLeft = _powerPerSet,
                SlapsLeft = _slapsPerSet,
                Advantage = 0f,
                SetWins = 0,
                Name = displayName
            };
        }
    }

    void ResetForNextSet()
    {
        // power/slaps/advantage reset; set wins persist
        var keys = new List<ulong>(_players.Keys);
        foreach (var id in keys)
        {
            var ps = _players[id];
            ps.PowerLeft = _powerPerSet;
            ps.SlapsLeft = _slapsPerSet;
            ps.Advantage = 0f;
            _players[id] = ps;
        }
        PublishHudSnapshot();
        _currentSetIndex += 1;
    }

    // ----- Attack/Defend phase completion -----
    void OnAllAttacksSubmitted()
    {
        if (!IsServer) return;
        if (CurrentPhase.Value != MatchPhase.Attack) return;

        // If CommitBuffer has FFA attacks, build defender->attackers list
        var entries = new List<(ulong defender, ulong[] attackers)>();
        if (_buffer != null && _buffer.TryGetAllAttackCommits_FFA(out var ffaAttacks))
        {
            // group by target
            var map = new Dictionary<ulong, List<ulong>>();
            foreach (var kv in ffaAttacks)
            {
                ulong attacker = kv.Key;
                ulong target = kv.Value.target;
                if (!map.TryGetValue(target, out var list))
                {
                    list = new List<ulong>();
                    map[target] = list;
                }
                list.Add(attacker);
            }

            foreach (var kv in map)
                entries.Add((kv.Key, kv.Value.ToArray()));
        }

        // Broadcast assignments (even if empty—harmless)
        attackAssignmentsChannel?.Raise(entries.ToArray());

        // Now move to Defend
        CurrentPhase.Value = MatchPhase.Defend;
        _timer = Mathf.Max(0.1f, defendSeconds);
    }


    void OnAllDefensesSubmitted()
    {
        if (!IsServer || CurrentPhase.Value != MatchPhase.Defend) return;
        CurrentPhase.Value = MatchPhase.Resolve;
        _timer = Mathf.Max(0.1f, resolveSeconds);
    }

    // ----- Resolve & Set logic -----
    void DoResolve_1v1()
    {
        _roundEvents.Clear();
        if (_buffer == null || !_buffer.IsServer) return;
        if (!_buffer.TryGetAllAttackCommits(out var attacks)) return;
        if (!_buffer.TryGetAllDefenseCommits(out var defenses)) return;

        var ids = new List<ulong>(_players.Keys);
        if (ids.Count != 2) return;

        ulong A = ids[0], B = ids[1];

        var aAtkDir = attacks.TryGetValue(A, out var aAtk) ? aAtk.dir : SlapDirection.Left;
        var aPow = attacks.TryGetValue(A, out aAtk) ? aAtk.power : 0;
        var aDefDir = defenses.TryGetValue(A, out var aDef) ? aDef : SlapDirection.Left;

        var bAtkDir = attacks.TryGetValue(B, out var bAtk) ? bAtk.dir : SlapDirection.Left;
        var bPow = attacks.TryGetValue(B, out bAtk) ? bAtk.power : 0;
        var bDefDir = defenses.TryGetValue(B, out var bDef) ? bDef : SlapDirection.Left;

        var evt1 = RoundEngine.ResolveExchange_1v1(A, B, aAtkDir, bDefDir, aPow, _players);
        var evt2 = RoundEngine.ResolveExchange_1v1(B, A, bAtkDir, aDefDir, bPow, _players);
        _roundEvents.Add(evt1);
        _roundEvents.Add(evt2);
    }

    void EvaluateSetEndOrEarlyEnd()
    {
        var ids = new List<ulong>(_players.Keys);
        if (ids.Count != 2) return;
        ulong A = ids[0], B = ids[1];

        var psA = _players[A]; var psB = _players[B];

        // End of set if both used all slaps, or early-end if comeback impossible.
        bool bothOutOfSlaps = (psA.SlapsLeft <= 0 && psB.SlapsLeft <= 0);
        bool early = IsEarlyEnd(psA, psB);

        if (!bothOutOfSlaps && !early) return;

        // Decide set winner by Advantage
        _lastSetWinner = 0;
        if (psA.Advantage > psB.Advantage) _lastSetWinner = A;
        else if (psB.Advantage > psA.Advantage) _lastSetWinner = B;
        else _lastSetWinner = 0; // tie -> flurry later

        // Update SetWins / MatchOver
        if (_lastSetWinner != 0)
        {
            var psW = _players[_lastSetWinner];
            psW.SetWins += 1;
            _players[_lastSetWinner] = psW;

            if (psW.SetWins >= _setsToWin)
                _matchOver = true;
        }
        else
        {
            // TODO: flurry tiebreaker here (next step)
        }

        setSummaryChannel?.Raise(new SetSummary
        {
            SetIndex = _currentSetIndex,
            IsTie = (_lastSetWinner == 0),
            WinnerClientId = _lastSetWinner,
            MatchOver = _matchOver,
            MatchWinnerClientId = _matchOver ? _lastSetWinner : 0
        });

        // Transition will reset/start next; mark pending
        _endSetPending = true;
    }

    // Conservative upper bound: maximum extra advantage a player could still earn
    // = remaining own power (as future hits) + 0.5 * opponent remaining power (as future blocks)
    static float MaxPotentialGain(PlayerRuntimeState me, PlayerRuntimeState opp)
    {
        return me.PowerLeft + 0.5f * opp.PowerLeft;
    }

    static bool IsEarlyEnd(PlayerRuntimeState A, PlayerRuntimeState B)
    {
        float lead = Mathf.Abs(A.Advantage - B.Advantage);
        if (A.Advantage >= B.Advantage)
        {
            // Can B catch up?
            float bMax = MaxPotentialGain(B, A);
            return bMax < lead;  // if B's absolute best cannot close the gap, end early
        }
        else
        {
            float aMax = MaxPotentialGain(A, B);
            return aMax < lead;
        }
    }

    void PublishHudSnapshot()
    {
        if (hudSnapshotChannel == null) return;
        var arr = new PlayerRuntimeState[_players.Count];
        int i = 0; foreach (var kv in _players) arr[i++] = kv.Value;
        hudSnapshotChannel.Raise(arr);
    }

    void PublishRoundSummary()
    {
        if (roundSummaryChannel == null) return;
        roundSummaryChannel.Raise(_roundEvents.ToArray());
    }
}
