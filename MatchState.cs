using System;
using System.Collections.Generic;
using System.Linq;

namespace SiegeUs.Core
{
    /// <summary>
    /// Host-authoritative match logic. No Unity / Among Us types in here on purpose,
    /// so it can be unit tested and reused. The game layer calls into this and
    /// mirrors the results to clients.
    /// </summary>
    public sealed partial class MatchState
    {
        public MatchConfig Config { get; }
        public Phase Phase { get; private set; } = Phase.Lobby;
        public int RoundNumber { get; private set; }
        public float PhaseTimeLeft { get; private set; }
        public TeamId AttackingTeam { get; private set; } = TeamId.Blue;
        public BombState Bomb { get; private set; } = BombState.Unplanted;
        public float BombTimeLeft { get; private set; }
        public TeamId? Winner { get; private set; }
        public RoundResult? LastRound { get; private set; }

        public Dictionary<TeamId, int> Score { get; } = new() { [TeamId.Blue] = 0, [TeamId.Orange] = 0 };
        public Dictionary<byte, PlayerSlot> Players { get; } = new();

        // votes for the current round
        readonly Dictionary<byte, string> _spawnVotes = new();
        readonly Dictionary<byte, string> _siteVotes = new();
        public string? ChosenAttackerSpawn { get; private set; }
        public string? ChosenSite { get; private set; }

        readonly MapData _map;

        public event Action<Phase>? PhaseChanged;
        public event Action<RoundResult>? RoundEnded;
        public event Action<TeamId>? MatchEnded;

        public MatchState(MatchConfig config, MapData map)
        {
            Config = config;
            _map = map;
        }

        // ---------------------------------------------------------------- lobby

        public Side SideOf(TeamId team) => team == AttackingTeam ? Side.Attackers : Side.Defenders;
        public Side SideOf(byte playerId) => SideOf(Players[playerId].Team);

        public bool TryJoinTeam(byte playerId, TeamId team, out string error)
        {
            error = "";
            if (Phase != Phase.Lobby) { error = "Teams are locked once the match starts."; return false; }
            int count = Players.Values.Count(p => p.Team == team && p.PlayerId != playerId);
            if (count >= 5) { error = "That team is full (5 players)."; return false; }
            if (!Players.TryGetValue(playerId, out var slot))
                Players[playerId] = slot = new PlayerSlot { PlayerId = playerId };
            slot.Team = team;
            return true;
        }

        public void RemovePlayer(byte playerId)
        {
            Players.Remove(playerId);
            _spawnVotes.Remove(playerId);
            _siteVotes.Remove(playerId);
            if (Phase == Phase.Action) EvaluateElimination();
        }

        public bool CanStart(out string why)
        {
            why = "";
            if (!Players.Values.Any(p => p.Team == TeamId.Blue) || !Players.Values.Any(p => p.Team == TeamId.Orange))
            { why = "Both teams need at least one player."; return false; }
            return true;
        }

        public bool StartMatch(out string why)
        {
            if (Phase != Phase.Lobby) { why = "Already started."; return false; }
            if (!CanStart(out why)) return false;
            Score[TeamId.Blue] = 0;
            Score[TeamId.Orange] = 0;
            RoundNumber = 0;
            Winner = null;
            AttackingTeam = TeamId.Blue; // TODO: coin flip if you prefer
            BeginRound();
            return true;
        }

        /// <summary>After MatchEnd the host can send everyone back to team select. Teams are kept.</summary>
        public void ResetToLobby()
        {
            Score[TeamId.Blue] = 0; Score[TeamId.Orange] = 0;
            RoundNumber = 0; Winner = null; LastRound = null;
            Bomb = BombState.Unplanted;
            foreach (var p in Players.Values) { p.Operator = null; p.Primary = null; p.Secondary = null; p.Alive = false; }
            SetPhase(Phase.Lobby, 0);
        }

        // ---------------------------------------------------------------- round flow

        void BeginRound()
        {
            RoundNumber++;
            if (RoundNumber > 1 && Config.SwapSidesEachRound)
                AttackingTeam = Other(AttackingTeam);

            Bomb = BombState.Unplanted;
            BombTimeLeft = 0;
            _spawnVotes.Clear();
            _siteVotes.Clear();
            ChosenAttackerSpawn = null;
            ChosenSite = null;
            foreach (var p in Players.Values)
            {
                p.Operator = null; p.Primary = null; p.Secondary = null;
                p.Alive = false; p.Hp = 0; p.MaxHp = 0;
            }
            SetPhase(Phase.SpawnPick, Config.SpawnPickSeconds);
        }

        void SetPhase(Phase p, float seconds)
        {
            Phase = p;
            PhaseTimeLeft = seconds;
            PhaseChanged?.Invoke(p);
        }

        // Host calls this every frame with Time.deltaTime.
        public void Tick(float dt)
        {
            if (Phase == Phase.Lobby || Phase == Phase.MatchEnd) return;
            PhaseTimeLeft -= dt;

            if (Phase == Phase.Action && Bomb == BombState.Planted)
            {
                BombTimeLeft -= dt;
                if (BombTimeLeft <= 0) { Bomb = BombState.Detonated; EndRound(AttackingTeam, RoundEndReason.BombDetonated); return; }
            }

            if (PhaseTimeLeft > 0) return;

            switch (Phase)
            {
                case Phase.SpawnPick:
                    ResolveSpawnVotes();
                    SetPhase(Phase.OperatorSelect, Config.OperatorSelectSeconds);
                    break;
                case Phase.OperatorSelect:
                    AutoAssignMissingOperators();
                    SpawnAllPlayers();
                    SetPhase(Phase.Prep, Config.PrepSeconds);
                    break;
                case Phase.Prep:
                    SetPhase(Phase.Action, Config.ActionSeconds);
                    break;
                case Phase.Action:
                    // R6 rule: time out with no plant = defenders win. With a plant, the fuse decides.
                    if (Bomb == BombState.Unplanted)
                        EndRound(Other(AttackingTeam), RoundEndReason.TimeExpired);
                    else
                        PhaseTimeLeft = 0.01f; // hold; bomb timer ends the round
                    break;
                case Phase.RoundEnd:
                    if (Winner != null) SetPhase(Phase.MatchEnd, 0);
                    else BeginRound();
                    break;
            }
        }

        // ---------------------------------------------------------------- spawn / site vote

        public bool TryVoteAttackerSpawn(byte playerId, string spawnId, out string error)
        {
            error = "";
            if (Phase != Phase.SpawnPick) { error = "Not in spawn selection."; return false; }
            if (SideOf(playerId) != Side.Attackers) { error = "Only attackers choose the spawn."; return false; }
            if (!_map.AttackerSpawns.Any(s => s.Id == spawnId)) { error = "Unknown spawn."; return false; }
            _spawnVotes[playerId] = spawnId;
            return true;
        }

        public bool TryVoteSite(byte playerId, string siteId, out string error)
        {
            error = "";
            if (Phase != Phase.SpawnPick) { error = "Not in site selection."; return false; }
            if (SideOf(playerId) != Side.Defenders) { error = "Only defenders choose the bomb site."; return false; }
            if (!_map.BombSites.Any(s => s.Id == siteId)) { error = "Unknown site."; return false; }
            _siteVotes[playerId] = siteId;
            return true;
        }

        void ResolveSpawnVotes()
        {
            ChosenAttackerSpawn = Majority(_spawnVotes.Values) ?? _map.AttackerSpawns.First().Id;
            ChosenSite = Majority(_siteVotes.Values) ?? _map.BombSites.First().Id;
        }

        static string? Majority(IEnumerable<string> votes) =>
            votes.GroupBy(v => v).OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault();

        // ---------------------------------------------------------------- operators

        public bool TryPickOperator(byte playerId, string operatorName, string? primary, string? secondary, out string error)
        {
            error = "";
            if (Phase != Phase.OperatorSelect) { error = "Not in operator select."; return false; }
            if (!Players.TryGetValue(playerId, out var slot)) { error = "You are not on a team."; return false; }
            var side = SideOf(slot.Team);
            var op = OperatorDatabase.Find(operatorName);
            if (op == null) { error = "Unknown operator."; return false; }
            if (op.Side != side) { error = $"{op.Name} is a {op.Side} operator."; return false; }
            if (Config.OneOperatorPerTeam && !op.Name.StartsWith("Recruit") &&
                Players.Values.Any(p => p.Team == slot.Team && p.PlayerId != playerId && p.Operator == op.Name))
            { error = $"{op.Name} is already taken on your team."; return false; }

            primary ??= op.Primaries.FirstOrDefault();
            secondary ??= op.Secondaries.FirstOrDefault();
            if (primary != null && !op.Primaries.Contains(primary)) { error = "Invalid primary."; return false; }
            if (secondary != null && !op.Secondaries.Contains(secondary)) { error = "Invalid secondary."; return false; }

            slot.Operator = op.Name; slot.Primary = primary; slot.Secondary = secondary;
            slot.MaxHp = op.MaxHealth; slot.Hp = op.MaxHealth;
            return true;
        }

        void AutoAssignMissingOperators()
        {
            foreach (var slot in Players.Values.Where(p => p.Operator == null))
            {
                var side = SideOf(slot.Team);
                var pool = OperatorDatabase.All.Where(o => o.Side == side)
                    .Where(o => !Players.Values.Any(p => p.Team == slot.Team && p.Operator == o.Name))
                    .ToList();
                // Recruit is always allowed to duplicate
                var pick = pool.FirstOrDefault() ?? OperatorDatabase.Recruit(side);
                TryPickOperator(slot.PlayerId, pick.Name, null, null, out _);
            }
        }

        void SpawnAllPlayers()
        {
            foreach (var p in Players.Values) { p.Alive = true; p.Hp = p.MaxHp; }
        }

        // ---------------------------------------------------------------- live round events

        public void ReportDeath(byte playerId)
        {
            if (Phase != Phase.Action || !Players.TryGetValue(playerId, out var p)) return;
            p.Alive = false; p.Hp = 0;
            EvaluateElimination();
        }

        public void ReportPlanted()
        {
            if (Phase != Phase.Action || Bomb != BombState.Unplanted) return;
            Bomb = BombState.Planted;
            BombTimeLeft = Config.BombFuseSeconds;
        }

        public void ReportDefused()
        {
            if (Phase != Phase.Action || Bomb != BombState.Planted) return;
            Bomb = BombState.Defused;
            EndRound(Other(AttackingTeam), RoundEndReason.BombDefused);
        }

        void EvaluateElimination()
        {
            if (Phase != Phase.Action) return;
            bool atkAlive = Players.Values.Any(p => p.Alive && SideOf(p.Team) == Side.Attackers);
            bool defAlive = Players.Values.Any(p => p.Alive && SideOf(p.Team) == Side.Defenders);

            if (!defAlive) { EndRound(AttackingTeam, RoundEndReason.DefendersEliminated); return; }
            // If bomb is planted the defenders must still defuse, so attackers dying doesn't end it.
            if (!atkAlive && Bomb == BombState.Unplanted)
                EndRound(Other(AttackingTeam), RoundEndReason.AttackersEliminated);
        }

        void EndRound(TeamId winner, RoundEndReason reason)
        {
            Score[winner]++;
            LastRound = new RoundResult { RoundNumber = RoundNumber, Winner = winner, Reason = reason };
            if (Score[winner] >= Config.RoundsToWin)
            {
                Winner = winner;
                MatchEnded?.Invoke(winner);
            }
            RoundEnded?.Invoke(LastRound);
            SetPhase(Phase.RoundEnd, Config.RoundEndSeconds);
        }

        static TeamId Other(TeamId t) => t == TeamId.Blue ? TeamId.Orange : TeamId.Blue;
    }
}
