using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side collector for per-phase commits.
// Listens to phase channel (no direct coordinator ref).
public class CommitBuffer : NetworkBehaviour
{
    [Header("Phase Event Channel (assign)")]
    public PhaseChangedChannel phaseChangedChannel;

    [Header("Attack phase completion (assign)")]
    public AttackAllSubmittedChannel attackAllSubmittedChannel;

    [Header("Defense phase completion (assign)")]
    public DefenseAllSubmittedChannel defenseAllSubmittedChannel;

    // --- Storage ---
    private readonly Dictionary<ulong, AttackCommit> _attackCommits = new();
    private readonly Dictionary<ulong, DefenseCommit> _defenseCommits = new();

    private bool _acceptingAttacks;
    private bool _acceptingDefenses;

    private struct AttackCommit
    {
        public ulong Attacker;
        public SlapDirection Dir;
        public int Power;
    }

    private struct DefenseCommit
    {
        public ulong Defender;
        public SlapDirection Dir;
    }

    private void OnEnable()
    {
        if (phaseChangedChannel != null)
            phaseChangedChannel.OnPhaseChanged += OnPhaseChanged;
    }

    private void OnDisable()
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

    // ----------------- ATTACK (client -> server) -----------------
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

        if (power < 0) power = 0;
        if (power > 999) power = 999;

        _attackCommits[sender] = new AttackCommit { Attacker = sender, Dir = dir, Power = power };

        if (AllExpectedAttackSubmissionsReceived())
            attackAllSubmittedChannel?.Raise();
    }

    private bool AllExpectedAttackSubmissionsReceived()
    {
        int need = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        return _attackCommits.Count >= need && need > 0;
    }

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

    // ----------------- DEFENSE (client -> server) -----------------
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

        // 1v1: each player defends exactly once (against the opponent's attack)
        // Later: in FFA we'll map attacker->defender sets and require multiple defenses.
        _defenseCommits[sender] = new DefenseCommit { Defender = sender, Dir = dir };

        if (AllExpectedDefenseSubmissionsReceived())
            defenseAllSubmittedChannel?.Raise();
    }

    private bool AllExpectedDefenseSubmissionsReceived()
    {
        // In 1v1, both players defend once
        int need = NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClientsIds.Count : 0;
        return _defenseCommits.Count >= need && need > 0;
    }

    public bool TryGetAllDefenseCommits(out Dictionary<ulong, SlapDirection> data)
    {
        data = null;
        if (!IsServer) return false;

        var dict = new Dictionary<ulong, SlapDirection>(_defenseCommits.Count);
        foreach (var kv in _defenseCommits)
            dict[kv.Key] = kv.Value.Dir;
        data = dict;
        return true;
    }
}
