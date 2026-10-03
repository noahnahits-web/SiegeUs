using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SiegeUs.Core
{
    /// <summary>Host -> clients state. Sent as a compact binary blob (see SnapshotCodec), not JSON.</summary>
    public sealed class SnapshotPlayer
    {
        public byte Id { get; set; }
        public TeamId Team { get; set; }
        public string? Operator { get; set; }
        public string? Primary { get; set; }
        public string? Secondary { get; set; }
        public bool Alive { get; set; }
        public int Hp { get; set; }
        public int MaxHp { get; set; }
        public string Floor { get; set; } = "1F";
    }

    public sealed class Snapshot
    {
        public Phase Phase { get; set; }
        public float TimeLeft { get; set; }
        public int Round { get; set; }
        public TeamId Attacking { get; set; }
        public int Blue { get; set; }
        public int Orange { get; set; }
        public BombState Bomb { get; set; }
        public float BombTime { get; set; }
        public string? Spawn { get; set; }
        public string? Site { get; set; }
        public TeamId? Winner { get; set; }
        public TeamId? LastWinner { get; set; }
        public RoundEndReason LastReason { get; set; }
        public int RoundsToWin { get; set; }
        public bool SwapSides { get; set; }
        public bool OneOperatorPerTeam { get; set; }
        public List<SnapshotPlayer> Players { get; set; } = new();
    }

    /// <summary>
    /// Compact wire format. Operators, weapons, floors, spawns and sites are sent as indexes into the
    /// (identical on every client) databases / map, so a 10 player snapshot is ~200 bytes, well under
    /// the packet limit. Bump Version whenever the layout changes.
    /// </summary>
    public static class SnapshotCodec
    {
        const byte Version = 1;

        static int Idx<T>(IReadOnlyList<T> list, string? name, Func<T, string> key)
        {
            if (name == null) return -1;
            for (int i = 0; i < list.Count; i++)
                if (string.Equals(key(list[i]), name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        static string? At<T>(IReadOnlyList<T> list, int i, Func<T, string> key) =>
            i >= 0 && i < list.Count ? key(list[i]) : null;

        public static byte[] Encode(Snapshot s, MapData map)
        {
            using var ms = new MemoryStream();
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Version);
                w.Write((byte)s.Phase);
                w.Write(s.TimeLeft);
                w.Write((byte)s.Round);
                w.Write((byte)s.Attacking);
                w.Write((byte)s.Blue);
                w.Write((byte)s.Orange);
                w.Write((byte)s.Bomb);
                w.Write(s.BombTime);
                w.Write((byte)s.RoundsToWin);
                w.Write((byte)((s.SwapSides ? 1 : 0) | (s.OneOperatorPerTeam ? 2 : 0)));
                w.Write((sbyte)Idx(map.AttackerSpawns, s.Spawn, x => x.Id));
                w.Write((sbyte)Idx(map.BombSites, s.Site, x => x.Id));
                w.Write((sbyte)(s.Winner.HasValue ? (int)s.Winner.Value : -1));
                w.Write((sbyte)(s.LastWinner.HasValue ? (int)s.LastWinner.Value : -1));
                w.Write((byte)s.LastReason);
                w.Write((byte)s.Players.Count);
                foreach (var p in s.Players)
                {
                    w.Write(p.Id);
                    w.Write((byte)p.Team);
                    w.Write(p.Alive);
                    w.Write((short)p.Hp);
                    w.Write((short)p.MaxHp);
                    w.Write((sbyte)Idx(map.Floors, p.Floor, f => f.Id));
                    w.Write((short)Idx(OperatorDatabase.All, p.Operator, o => o.Name));
                    w.Write((short)Idx(WeaponDatabase.All, p.Primary, x => x.Name));
                    w.Write((short)Idx(WeaponDatabase.All, p.Secondary, x => x.Name));
                }
            }
            return ms.ToArray();
        }

        public static Snapshot Decode(byte[] data, MapData map)
        {
            using var ms = new MemoryStream(data);
            using var r = new BinaryReader(ms);
            if (r.ReadByte() != Version) throw new InvalidDataException("Snapshot version mismatch - everyone needs the same Siege Us version.");
            var s = new Snapshot
            {
                Phase = (Phase)r.ReadByte(),
                TimeLeft = r.ReadSingle(),
                Round = r.ReadByte(),
                Attacking = (TeamId)r.ReadByte(),
                Blue = r.ReadByte(),
                Orange = r.ReadByte(),
                Bomb = (BombState)r.ReadByte(),
                BombTime = r.ReadSingle(),
                RoundsToWin = r.ReadByte(),
            };
            int flags = r.ReadByte();
            s.SwapSides = (flags & 1) != 0;
            s.OneOperatorPerTeam = (flags & 2) != 0;
            s.Spawn = At(map.AttackerSpawns, r.ReadSByte(), x => x.Id);
            s.Site = At(map.BombSites, r.ReadSByte(), x => x.Id);
            int win = r.ReadSByte(); s.Winner = win >= 0 ? (TeamId)win : (TeamId?)null;
            int lw = r.ReadSByte(); s.LastWinner = lw >= 0 ? (TeamId)lw : (TeamId?)null;
            s.LastReason = (RoundEndReason)r.ReadByte();
            int n = r.ReadByte();
            for (int i = 0; i < n; i++)
            {
                var p = new SnapshotPlayer
                {
                    Id = r.ReadByte(),
                    Team = (TeamId)r.ReadByte(),
                    Alive = r.ReadBoolean(),
                    Hp = r.ReadInt16(),
                    MaxHp = r.ReadInt16(),
                };
                p.Floor = At(map.Floors, r.ReadSByte(), f => f.Id) ?? "1F";
                p.Operator = At(OperatorDatabase.All, r.ReadInt16(), o => o.Name);
                p.Primary = At(WeaponDatabase.All, r.ReadInt16(), x => x.Name);
                p.Secondary = At(WeaponDatabase.All, r.ReadInt16(), x => x.Name);
                s.Players.Add(p);
            }
            return s;
        }
    }

    public sealed partial class MatchState
    {
        public Snapshot Export() => new()
        {
            Phase = Phase, TimeLeft = PhaseTimeLeft, Round = RoundNumber, Attacking = AttackingTeam,
            Blue = Score[TeamId.Blue], Orange = Score[TeamId.Orange], Bomb = Bomb, BombTime = BombTimeLeft,
            Spawn = ChosenAttackerSpawn, Site = ChosenSite, Winner = Winner,
            LastWinner = LastRound?.Winner, LastReason = LastRound?.Reason ?? RoundEndReason.None,
            RoundsToWin = Config.RoundsToWin, SwapSides = Config.SwapSidesEachRound, OneOperatorPerTeam = Config.OneOperatorPerTeam,
            Players = Players.Values.Select(p => new SnapshotPlayer
            {
                Id = p.PlayerId, Team = p.Team, Operator = p.Operator, Primary = p.Primary, Secondary = p.Secondary,
                Alive = p.Alive, Hp = p.Hp, MaxHp = p.MaxHp, Floor = p.Floor
            }).ToList()
        };

        /// <summary>Base64 so it travels safely inside the JSON envelope.</summary>
        public string ExportBase64() => Convert.ToBase64String(SnapshotCodec.Encode(Export(), _map));

        /// <summary>Client side. Returns false (and changes nothing) if the blob is corrupt or from another version.</summary>
        public bool ImportBase64(string b64, out string error)
        {
            error = "";
            Snapshot snap;
            try { snap = SnapshotCodec.Decode(Convert.FromBase64String(b64), _map); }
            catch (Exception e) { error = e.Message; return false; }
            Import(snap);
            return true;
        }

        /// <summary>Client side: overwrite local state with the host's snapshot.</summary>
        public void Import(Snapshot s)
        {
            bool phaseChanged = Phase != s.Phase;
            Config.RoundsToWin = s.RoundsToWin;
            Config.SwapSidesEachRound = s.SwapSides;
            Config.OneOperatorPerTeam = s.OneOperatorPerTeam;
            RoundNumber = s.Round; AttackingTeam = s.Attacking;
            Score[TeamId.Blue] = s.Blue; Score[TeamId.Orange] = s.Orange;
            Bomb = s.Bomb; BombTimeLeft = s.BombTime; PhaseTimeLeft = s.TimeLeft;
            ChosenAttackerSpawn = s.Spawn; ChosenSite = s.Site; Winner = s.Winner;
            LastRound = s.LastWinner.HasValue
                ? new RoundResult { RoundNumber = s.Round, Winner = s.LastWinner.Value, Reason = s.LastReason }
                : null;

            var seen = new HashSet<byte>();
            foreach (var sp in s.Players)
            {
                seen.Add(sp.Id);
                if (!Players.TryGetValue(sp.Id, out var slot)) Players[sp.Id] = slot = new PlayerSlot { PlayerId = sp.Id };
                slot.Team = sp.Team; slot.Operator = sp.Operator; slot.Primary = sp.Primary; slot.Secondary = sp.Secondary;
                slot.Alive = sp.Alive; slot.Hp = sp.Hp; slot.MaxHp = sp.MaxHp; slot.Floor = sp.Floor;
            }
            foreach (var id in Players.Keys.Where(k => !seen.Contains(k)).ToList()) Players.Remove(id);

            if (phaseChanged) SetPhaseFromRemote(s.Phase);
        }

        /// <summary>Clients call this each frame so the HUD timers count down between host snapshots.</summary>
        public void TickDisplayOnly(float dt)
        {
            if (Phase == Phase.Lobby || Phase == Phase.MatchEnd) return;
            if (PhaseTimeLeft > 0) PhaseTimeLeft -= dt;
            if (Bomb == BombState.Planted && BombTimeLeft > 0) BombTimeLeft -= dt;
        }

        void SetPhaseFromRemote(Phase p) { Phase = p; PhaseChangedRemote?.Invoke(p); }
        public event System.Action<Phase>? PhaseChangedRemote;

        /// <summary>Damage applied by the host after validating a hit message.</summary>
        public bool ApplyDamage(byte shooter, byte victim, int dmg)
        {
            if (Phase != Phase.Action) return false;
            if (!Players.TryGetValue(shooter, out var s) || !Players.TryGetValue(victim, out var v)) return false;
            if (!s.Alive || !v.Alive) return false;
            if (s.Team == v.Team) return false;               // no friendly fire
            if (s.Floor != v.Floor) return false;             // must be on the same floor
            v.Hp -= dmg;
            if (v.Hp <= 0) ReportDeath(victim);
            return true;
        }
    }
}
