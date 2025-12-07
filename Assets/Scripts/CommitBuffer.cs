using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side collector for per-phase commits; now stores target for FFA.
public class CommitBuffer : NetworkBehaviour
{
    [Header("Phase Event Channel (assign)")]
    public PhaseChangedChannel phaseChangedChannel;

    [Header("Phase completion (assign)")]
    public AttackAllSubmittedChannel attackAllSubmittedChannel;
    public DefenseAllSubmittedChannel defenseAllSubmittedChannel;

    // --- Storage ---
    private readonly Dictionary<ulong, AttackCommit> _attackCommits = new();
    private readonly Dictionary<ulong, DefenseCommit> _defenseCommits = new();

    private bool _acceptingAttacks;
    private bool _acceptingDefenses;

    private struct AttackCommit
    {
        public ulong Attacker;
        public ulong Target;           // NEW for FFA
        public SlapDirection Dir;
        public int Power;
    }

    private struct DefenseCommit
    {
        public ulong Defender;
        public SlapDirection Dir;
        // NOTE: for FFA (multiple incoming attacks), we’ll extend with per-attacker defense later.
    }

    void OnEnable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += OnPhaseChanged;
    }

    void OnDisable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged -= OnPhaseChanged;
    }

    private void OnPhaseChanged(MatchPhase prev, MatchPhase curr)
    {
        if (!IsServer) return;

        if (curr == MatchPhase.Attack)
        {
            _attackCommits.Clear();
            _defenseCommits.Clear();
            _acceptingAttacks = true;
            _acceptingDefenses = false;
        }
        else if (curr == MatchPhase.Defend)
        {
            _acceptingAttacks = false;
            _acceptingDefenses = true;
            _defenseCommits.Clear();
        }
        else
        {
            _acceptingAttacks = false;
            _acceptingDefenses = false;
        }
    }

    // -------- ATTACK (1v1 legacy) --------
    public void SubmitAttackFromClient(SlapDirection dir, int power)
    {
        if (IsClient) SubmitAttackServerRpc(dir, power);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitAttackServerRpc(SlapDirection dir, int power, RpcParams rpcParams = default)
    {
        if (!_acceptingAttacks) return;
        ulong sender = rpcParams.Receive.SenderClientId;
        if (_attackCommits.ContainsKey(sender)) return;

        power = Mathf.Clamp(power, 0, 999);
        _attackCommits[sender] = new AttackCommit { Attacker = sender, Target = 0, Dir = dir, Power = power };

        if (AllExpectedAttackSubmissionsReceived())
            attackAllSubmittedChannel?.Raise();
    }

    // -------- ATTACK (FFA with target) --------
    public void SubmitAttackFromClient_FFA(SlapDirection dir, int power, ulong targetClientId)
    {
        if (IsClient) SubmitAttackFFAServerRpc(dir, power, targetClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitAttackFFAServerRpc(SlapDirection dir, int power, ulong targetClientId, RpcParams rpcParams = default)
    {
        if (!_acceptingAttacks) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (_attackCommits.ContainsKey(sender)) return; // one attack per attacker per window

        power = Mathf.Clamp(power, 0, 999);
        _attackCommits[sender] = new AttackCommit
        {
            Attacker = sender,
            Target = targetClientId,
            Dir = dir,
            Power = power
        };

        if (AllExpectedAttackSubmissionsReceived())
            attackAllSubmittedChannel?.Raise();
    }

    private bool AllExpectedAttackSubmissionsReceived()
    {
        int need = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        return _attackCommits.Count >= need && need > 0;
    }

    // 1v1 accessor kept for backward-compat with current Resolve_1v1 path.
    public bool TryGetAllAttackCommits(out Dictionary<ulong, (SlapDirection dir, int power)> data)
    {
        data = null;
        if (!IsServer) return false;

        var dict = new Dictionary<ulong, (SlapDirection, int)>(_attackCommits.Count);
        foreach (var kv in _attackCommits)
            dict[kv.Key] = (kv.Value.Dir, kv.Value.Power);

        data = dict;
        return true;
    }


    public bool TryGetAllAttackCommits_FFA(out Dictionary<ulong, (ulong target, SlapDirection dir, int power)> data)
    {
        data = null; if (!IsServer) return false;
        var dict = new Dictionary<ulong, (ulong, SlapDirection, int)>(_attackCommits.Count);
        foreach (var kv in _attackCommits)
            dict[kv.Key] = (kv.Value.Target, kv.Value.Dir, kv.Value.Power);
        data = dict; return true;
    }

    // -------- DEFENSE (unchanged for now; FFA per-attacker defense in next step) --------
    public void SubmitDefenseFromClient(SlapDirection dir)
    {
        if (IsClient) SubmitDefenseServerRpc(dir);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitDefenseServerRpc(SlapDirection dir, RpcParams rpcParams = default)
    {
        if (!_acceptingDefenses) return;

        ulong sender = rpcParams.Receive.SenderClientId;
        if (_defenseCommits.ContainsKey(sender)) return;

        _defenseCommits[sender] = new DefenseCommit { Defender = sender, Dir = dir };

        if (AllExpectedDefenseSubmissionsReceived())
            defenseAllSubmittedChannel?.Raise();
    }

    private bool AllExpectedDefenseSubmissionsReceived()
    {
        int need = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        return _defenseCommits.Count >= need && need > 0;
    }

    public bool TryGetAllDefenseCommits(out Dictionary<ulong, SlapDirection> data)
    {
        data = null; if (!IsServer) return false;
        var dict = new Dictionary<ulong, SlapDirection>(_defenseCommits.Count);
        foreach (var kv in _defenseCommits)
            dict[kv.Key] = kv.Value.Dir;
        data = dict; return true;
    }
}
