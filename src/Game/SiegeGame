using System;
using System.Collections.Generic;
using System.Linq;
using SiegeUs.Core;
using UnityEngine;

namespace SiegeUs.Game
{
    /// <summary>
    /// Drives a Siege match inside the Among Us lobby. The host owns MatchState and ticks it;
    /// every client mirrors it from snapshots and handles only local input (move, shoot, interact).
    /// Plain C# (not a MonoBehaviour); SiegeDriver forwards Update/OnGUI to it.
    /// </summary>
    public sealed class SiegeGame
    {
        public static SiegeGame? Instance { get; private set; }

        public MatchConfig Config { get; } = new();
        public MapData Map { get; private set; } = null!;
        public MatchState Match { get; private set; } = null!;
        public MapBuilder? World { get; private set; }

        // ---- local combat state
        public int WeaponSlot { get; private set; }  // 0 primary, 1 secondary
        public int[] Ammo { get; } = new int[2];
        float _cooldown, _reload;
        public bool Reloading => _reload > 0;
        float _plantHold;
        Vector2 _lobbyPos;
        float _snapshotTimer;
        int _reinforcementsUsed;                      // host-side, per round
        const int ReinforcementsPerRound = 10;

        public bool InLobby => LobbyBehaviour.Instance != null;
        public bool MatchActive => Match.Phase != Phase.Lobby;

        float _pruneTimer;
        float _fullSyncTimer;

        int _gameId;

        public SiegeGame()
        {
            Instance = this;
            Map = MapData.LoadEmbedded("oregon");
            NewMatch();
        }

        void NewMatch()
        {
            Match = new MatchState(Config, Map);
            Match.PhaseChanged += OnHostPhaseChanged;
            Match.PhaseChangedRemote += OnPhaseApplied;
            Match.RoundEnded += _ => Broadcast();
            Match.MatchEnded += _ => Broadcast();
        }

        /// <summary>Joined a different lobby (or left): throw away any stale match so we never get stuck mid-phase.</summary>
        void ResetForNewLobby()
        {
            World?.Destroy();
            World = null;
            NewMatch();
            WeaponSlot = 0; _cooldown = 0; _reload = 0; _plantHold = 0;
            var me = PlayerControl.LocalPlayer;
            if (me != null) { me.moveable = true; me.MyPhysics.Speed = 2.5f; }
        }

        public PlayerSlot? LocalSlot =>
            PlayerControl.LocalPlayer != null && Match.Players.TryGetValue(PlayerControl.LocalPlayer.PlayerId, out var s) ? s : null;

        public bool AmHost => AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost;
        static bool IsFromHost(PlayerControl p) => AmongUsClient.Instance != null && p.OwnerId == AmongUsClient.Instance.HostId;

        // ============================================================== per-frame

        public void Tick()
        {
            if (PlayerControl.LocalPlayer == null) return;

            if (Input.GetKeyDown(KeyCode.F9)) SiegeUI.PanelHidden = !SiegeUI.PanelHidden;

            int gid = AmongUsClient.Instance != null ? AmongUsClient.Instance.GameId : 0;
            if (gid != _gameId) { _gameId = gid; if (MatchActive) ResetForNewLobby(); }

            if (AmHost)
            {
                PruneDisconnected();
                if (MatchActive)
                {
                    Match.Tick(Time.deltaTime);
                    _snapshotTimer -= Time.deltaTime;
                    if (_snapshotTimer <= 0) { Broadcast(); _snapshotTimer = 0.5f; }
                }
                else if (Match.Players.Count > 0)
                {
                    // lobby: re-send team lists every couple of seconds so late joiners catch up
                    _fullSyncTimer -= Time.deltaTime;
                    if (_fullSyncTimer <= 0) { Broadcast(); _fullSyncTimer = 2f; }
                }
            }
            else if (MatchActive)
            {
                // clients extrapolate the timers between snapshots so the HUD stays smooth
                Match.TickDisplayOnly(Time.deltaTime);
            }

            if (!MatchActive) return;
            ApplyMovementRules();
            SyncFloor();
            UpdateVisibility();
            if (Match.Phase == Phase.Prep || Match.Phase == Phase.Action) HandleInput();
        }

        /// <summary>Host: drop players who left the lobby so a dead slot can't stall a round.</summary>
        void PruneDisconnected()
        {
            _pruneTimer -= Time.deltaTime;
            if (_pruneTimer > 0) return;
            _pruneTimer = 1f;
            var present = new HashSet<byte>();
            foreach (var pc in PlayerControl.AllPlayerControls) if (pc != null) present.Add(pc.PlayerId);
            var gone = Match.Players.Keys.Where(k => !present.Contains(k)).ToList();
            foreach (var id in gone) Match.RemovePlayer(id);
            if (gone.Count > 0) Broadcast();
        }

        /// <summary>The host owns which floor you are on; make the local view follow it.</summary>
        void SyncFloor()
        {
            var me = LocalSlot;
            if (me == null || World == null) return;
            bool live = Match.Phase == Phase.Prep || Match.Phase == Phase.Action || Match.Phase == Phase.RoundEnd;
            if (live && World.CurrentFloor != me.Floor) World.SetFloor(me.Floor);
        }

        void ApplyMovementRules()
        {
            var me = LocalSlot;
            if (me == null) return;
            var side = Match.SideOf(me.Team);
            bool canMove = me.Alive &&
                (Match.Phase == Phase.Action || (Match.Phase == Phase.Prep && side == Side.Defenders));
            PlayerControl.LocalPlayer.moveable = canMove;
        }

        float _visTimer;
        void UpdateVisibility()
        {
            _visTimer -= Time.deltaTime;
            if (_visTimer > 0 || World == null) return;
            _visTimer = 0.2f;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc == null || pc.AmOwner) continue;
                if (!Match.Players.TryGetValue(pc.PlayerId, out var s)) continue;
                pc.Visible = s.Alive && s.Floor == World.CurrentFloor;
            }
        }

        // ============================================================== local input

        void HandleInput()
        {
            var me = LocalSlot;
            if (me == null || !me.Alive) return;
            var op = me.Operator != null ? OperatorDatabase.Find(me.Operator) : null;
            if (op == null) return;
            bool live = Match.Phase == Phase.Action;

            if (Input.GetKeyDown(KeyCode.Alpha1)) WeaponSlot = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) WeaponSlot = 1;
            var weapon = CurrentWeapon(me);

            if (_cooldown > 0) _cooldown -= Time.deltaTime;
            if (_reload > 0)
            {
                _reload -= Time.deltaTime;
                if (_reload <= 0 && weapon != null) Ammo[WeaponSlot] = weapon.MagSize;
            }

            // --- shooting
            if (live && weapon != null && weapon.Category != WeaponCategory.Shield && !Reloading)
            {
                bool wantFire = weapon.IsAutomatic ? Input.GetMouseButton(0) : Input.GetMouseButtonDown(0);
                if (Input.GetKeyDown(KeyCode.R) && Ammo[WeaponSlot] < weapon.MagSize) _reload = weapon.ReloadSeconds;
                else if (wantFire && _cooldown <= 0)
                {
                    if (Ammo[WeaponSlot] <= 0) _reload = weapon.ReloadSeconds;
                    else { Ammo[WeaponSlot]--; _cooldown = weapon.SecondsPerShot; Fire(me, weapon); }
                }
            }

            // --- melee / breach (V)
            if (live && Input.GetKeyDown(KeyCode.V)) Melee(me);

            // --- floors (E up, Q down)
            if (Input.GetKeyDown(KeyCode.E)) ChangeFloor(me, up: true);
            if (Input.GetKeyDown(KeyCode.Q)) ChangeFloor(me, up: false);

            // --- reinforce (T) during prep, defenders only
            if (Match.Phase == Phase.Prep && Match.SideOf(me.Team) == Side.Defenders && Input.GetKeyDown(KeyCode.T))
                TryReinforce();

            // --- plant (attackers) / defuse (defenders): hold F
            if (live) HandleBombInput(me);
        }

        public Weapon? CurrentWeapon(PlayerSlot me)
        {
            var name = WeaponSlot == 0 ? me.Primary : me.Secondary;
            return name != null ? WeaponDatabase.Find(name) : null;
        }

        Vector2 LocalPos => PlayerControl.LocalPlayer.transform.position;
        Vector2 AimDir()
        {
            var cam = Camera.main;
            var target = cam != null ? (Vector2)cam.ScreenToWorldPoint(Input.mousePosition) : LocalPos + Vector2.right;
            var d = target - LocalPos;
            return d.sqrMagnitude < 0.0001f ? Vector2.right : d.normalized;
        }

        void Fire(PlayerSlot me, Weapon w)
        {
            var origin = LocalPos;
            var baseDir = AimDir();
            for (int p = 0; p < Math.Max(1, w.Pellets); p++)
            {
                float ang = UnityEngine.Random.Range(-w.SpreadDegrees, w.SpreadDegrees);
                var dir = (Vector2)(Quaternion.Euler(0, 0, ang) * baseDir);
                CastBullet(origin, dir, w);
            }
        }

        void CastBullet(Vector2 origin, Vector2 dir, Weapon w)
        {
            // RaycastAll is sorted nearest-first. Walk until something stops the bullet.
            var hits = Physics2D.RaycastAll(origin + dir * 0.4f, dir, 40f);
            Vector2 end = origin + dir * 40f;
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                var wall = World?.WallFromCollider(h.collider);
                if (wall != null)
                {
                    if (wall.IsDestroyed) continue;
                    SiegeNet.Send(new SiegeMsg { Type = "WallHit", A = wall.WallId, N = w.Damage });
                    if (wall.Kind == WallKind.Glass) continue;     // bullets pass through breakable glass
                    end = h.point; break;
                }
                var pc = h.collider.GetComponentInParent<PlayerControl>();
                if (pc != null && !pc.AmOwner)
                {
                    // players on another floor aren't in the bullet's plane
                    if (!Match.Players.TryGetValue(pc.PlayerId, out var vs) || !vs.Alive) continue;      // spectators / dead players don't stop bullets
                    if (World != null && vs.Floor != World.CurrentFloor) continue;
                    var mine = LocalSlot;
                    if (mine != null && vs.Team == mine.Team) continue;                                  // teammates are not hit
                    SiegeNet.Send(new SiegeMsg { Type = "Hit", A = pc.PlayerId.ToString(), N = w.Damage });
                    end = h.point; break;
                }
            }
            Tracer.Draw(origin, end);
        }

        void Melee(PlayerSlot me)
        {
            var dir = AimDir();
            var hits = Physics2D.RaycastAll(LocalPos, dir, 1.6f);
            foreach (var h in hits)
            {
                var wall = h.collider != null ? World?.WallFromCollider(h.collider) : null;
                if (wall != null && !wall.IsDestroyed) { SiegeNet.Send(new SiegeMsg { Type = "WallHit", A = wall.WallId, N = 40 }); return; }
                var pc = h.collider != null ? h.collider.GetComponentInParent<PlayerControl>() : null;
                if (pc != null && !pc.AmOwner && Match.Players.TryGetValue(pc.PlayerId, out var mv) && mv.Alive && mv.Team != me.Team)
                { SiegeNet.Send(new SiegeMsg { Type = "Hit", A = pc.PlayerId.ToString(), N = 40 }); return; }
            }
            // breach a soft floor/ceiling under or over us
            var hatch = World?.HatchAt(me.Floor, LocalPos);
            if (hatch != null && hatch.Soft) SiegeNet.Send(new SiegeMsg { Type = "HatchBreak", A = hatch.Id });
        }

        void ChangeFloor(PlayerSlot me, bool up)
        {
            if (World == null) return;
            var h = World.HatchAt(me.Floor, LocalPos);
            if (h == null || !(h.Walkable || World.BrokenHatches.Contains(h.Id))) return;
            string target = up ? (me.Floor == h.FromFloor ? h.ToFloor : "") : (me.Floor == h.ToFloor ? h.FromFloor : "");
            if (string.IsNullOrEmpty(target)) return;
            SiegeNet.Send(new SiegeMsg { Type = "Floor", A = target });
        }

        void TryReinforce()
        {
            if (World == null) return;
            DestructibleWall? best = null; float bestD = 2.5f;
            foreach (var w in World.Walls.Values)
            {
                if (w.FloorId != World.CurrentFloor || w.Kind != WallKind.Reinforceable || w.ReinforcementHp > 0 || w.IsDestroyed) continue;
                var d = Vector2.Distance(LocalPos, w.Position);
                if (d < bestD) { bestD = d; best = w; }
            }
            if (best != null) SiegeNet.Send(new SiegeMsg { Type = "Reinforce", A = best.WallId });
        }

        BombSiteDef? ActiveSite => Map.BombSites.FirstOrDefault(s => s.Id == Match.ChosenSite);
        bool InsideSite(BombSiteDef s)
        {
            var me = LocalSlot; if (me == null || me.Floor != s.Floor) return false;
            var p = LocalPos - MapBuilder.Origin; var m = new Vector2(p.x, -p.y);
            return m.x >= s.Rect[0] && m.x <= s.Rect[0] + s.Rect[2] && m.y >= s.Rect[1] && m.y <= s.Rect[1] + s.Rect[3];
        }

        public float BombProgress => _plantHold;

        void HandleBombInput(PlayerSlot me)
        {
            var site = ActiveSite;
            bool atk = Match.SideOf(me.Team) == Side.Attackers;
            bool planting = atk && Match.Bomb == BombState.Unplanted;
            bool defusing = !atk && Match.Bomb == BombState.Planted;
            if (site != null && (planting || defusing) && InsideSite(site) && Input.GetKey(KeyCode.F))
            {
                _plantHold += Time.deltaTime;
                if (_plantHold >= 7f)
                {
                    _plantHold = 0;
                    SiegeNet.Send(new SiegeMsg { Type = planting ? "Plant" : "Defuse" });
                }
            }
            else _plantHold = 0;
        }

        // ============================================================== lobby actions (called by UI)

        public void JoinTeam(TeamId t) => SiegeNet.Send(new SiegeMsg { Type = "JoinTeam", A = t.ToString() });
        public void BackToLobby() => SiegeNet.Send(new SiegeMsg { Type = "Lobby" });
        public void RequestStart() => SiegeNet.Send(new SiegeMsg { Type = "Start" });
        public void VoteSpawn(string id) => SiegeNet.Send(new SiegeMsg { Type = "VoteSpawn", A = id });
        public void VoteSite(string id) => SiegeNet.Send(new SiegeMsg { Type = "VoteSite", A = id });
        public void PickOperator(string op, string? pri, string? sec) =>
            SiegeNet.Send(new SiegeMsg { Type = "Pick", A = op, B = pri ?? "", C = sec ?? "" });

        // ============================================================== message handling

        public void OnMessage(PlayerControl sender, SiegeMsg m)
        {
            // ---- messages every client applies
            switch (m.Type)
            {
                case "Snapshot":
                    if (!IsFromHost(sender) || AmHost) return;      // host already has authoritative state
                    if (!Match.ImportBase64(m.A, out var impErr)) SiegePlugin.Logger.LogWarning("Snapshot rejected: " + impErr);
                    return;
                case "WallHit":
                    if (World != null && World.Walls.TryGetValue(m.A, out var w)) w.ApplyDamage(Math.Clamp(m.N, 0, 200));
                    return;
                case "HatchBreak":
                    World?.BreakHatch(m.A);
                    return;
                case "ReinforceOK":
                    if (IsFromHost(sender) && World != null && World.Walls.TryGetValue(m.A, out var rw)) rw.Reinforce();
                    return;
            }

            // ---- everything else is an intent; only the host acts on it
            if (!AmHost) return;
            byte pid = sender.PlayerId;               // never trust the id inside the payload
            bool changed = true;
            string err;
            switch (m.Type)
            {
                case "JoinTeam":
                    changed = Enum.TryParse<TeamId>(m.A, out var team) && Match.TryJoinTeam(pid, team, out err);
                    break;
                case "Start":
                    changed = IsFromHost(sender) && Match.StartMatch(out err);
                    break;
                case "Lobby":
                    if (IsFromHost(sender) && Match.Phase == Phase.MatchEnd) Match.ResetToLobby(); else changed = false;
                    break;
                case "VoteSpawn": changed = Match.TryVoteAttackerSpawn(pid, m.A, out err); break;
                case "VoteSite": changed = Match.TryVoteSite(pid, m.A, out err); break;
                case "Pick":
                    changed = Match.TryPickOperator(pid, m.A, NullIfEmpty(m.B), NullIfEmpty(m.C), out err);
                    break;
                case "Hit":
                    if (byte.TryParse(m.A, out var victim)) changed = Match.ApplyDamage(pid, victim, Math.Clamp(m.N, 1, 200));
                    else changed = false;
                    break;
                case "Floor":
                    if (Match.Players.TryGetValue(pid, out var fs) && Map.Floors.Any(f => f.Id == m.A)) fs.Floor = m.A;
                    break;
                case "Plant":
                    if (Match.Players.TryGetValue(pid, out var pp) && Match.SideOf(pp.Team) == Side.Attackers) Match.ReportPlanted();
                    break;
                case "Defuse":
                    if (Match.Players.TryGetValue(pid, out var dp) && Match.SideOf(dp.Team) == Side.Defenders) Match.ReportDefused();
                    break;
                case "Reinforce":
                    if (Match.Phase == Phase.Prep && _reinforcementsUsed < ReinforcementsPerRound
                        && Match.Players.TryGetValue(pid, out var rp) && Match.SideOf(rp.Team) == Side.Defenders)
                    {
                        _reinforcementsUsed++;
                        SiegeNet.Send(new SiegeMsg { Type = "ReinforceOK", A = m.A });
                    }
                    changed = false;
                    break;
                default: changed = false; break;
            }
            if (changed) Broadcast();
        }

        static string? NullIfEmpty(string s) => string.IsNullOrEmpty(s) ? null : s;

        void Broadcast()
        {
            if (!AmHost) return;
            SiegeNet.Send(new SiegeMsg { Type = "Snapshot", A = Match.ExportBase64() });
        }

        // ============================================================== phase transitions

        // Host only: set per-round server state when a phase starts.
        void OnHostPhaseChanged(Phase p)
        {
            if (p == Phase.SpawnPick) _reinforcementsUsed = 0;
            if (p == Phase.Prep)
            {
                var site = Map.BombSites.FirstOrDefault(s => s.Id == Match.ChosenSite) ?? Map.BombSites[0];
                var spawn = Map.AttackerSpawns.FirstOrDefault(s => s.Id == Match.ChosenAttackerSpawn) ?? Map.AttackerSpawns[0];
                foreach (var pl in Match.Players.Values)
                    pl.Floor = Match.SideOf(pl.Team) == Side.Attackers ? spawn.Floor : site.Floor;
            }
            Broadcast();
            OnPhaseApplied(p); // host also runs the local side effects
        }

        // Everyone: local side effects when the phase becomes p.
        void OnPhaseApplied(Phase p)
        {
            if (p != Phase.SpawnPick && p != Phase.Lobby && (World == null || World.Walls.Count == 0))
            {
                World ??= new MapBuilder(Map);
                World.Build();     // missed the SpawnPick snapshot: make sure the arena exists
            }
            switch (p)
            {
                case Phase.SpawnPick:
                    if (Match.RoundNumber <= 1) _lobbyPos = LocalPos;   // remember where to return after the match
                    if (World == null) World = new MapBuilder(Map);
                    World.Build();                       // rebuilds walls/hatches => round reset
                    WeaponSlot = 0; _cooldown = 0; _reload = 0;
                    break;
                case Phase.Prep:
                    TeleportToSpawn();
                    ApplyOperatorSpeed();
                    var me = LocalSlot;
                    if (me != null)
                    {
                        var pw = me.Primary != null ? WeaponDatabase.Find(me.Primary) : null;
                        var sw = me.Secondary != null ? WeaponDatabase.Find(me.Secondary) : null;
                        Ammo[0] = pw?.MagSize ?? 0; Ammo[1] = sw?.MagSize ?? 0;
                    }
                    break;
                case Phase.MatchEnd:
                case Phase.Lobby:
                    PlayerControl.LocalPlayer.moveable = true;
                    PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f;
                    if (p == Phase.Lobby)
                    {
                        World?.Destroy();
                        PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(_lobbyPos);
                        foreach (var pc in PlayerControl.AllPlayerControls) if (pc != null) pc.Visible = true;
                    }
                    break;
            }
        }

        void TeleportToSpawn()
        {
            var me = LocalSlot; if (me == null || World == null) return;
            var side = Match.SideOf(me.Team);
            int idx = Match.Players.Values.Where(p => p.Team == me.Team).OrderBy(p => p.PlayerId).ToList().FindIndex(p => p.PlayerId == me.PlayerId);
            float[] pos;
            if (side == Side.Attackers)
            {
                var spawn = Map.AttackerSpawns.FirstOrDefault(s => s.Id == Match.ChosenAttackerSpawn) ?? Map.AttackerSpawns[0];
                pos = new[] { spawn.Pos[0] + (idx % 3 - 1) * 1.2f, spawn.Pos[1] + (idx / 3) * 1.2f };
                World.SetFloor(spawn.Floor);
            }
            else
            {
                var site = ActiveSite ?? Map.BombSites[0];
                var sp = site.DefenderSpawns[Math.Max(0, idx) % site.DefenderSpawns.Length];
                pos = sp.Pos;
                World.SetFloor(site.Floor);
            }
            PlayerControl.LocalPlayer.NetTransform.RpcSnapTo(MapBuilder.ToWorld(pos));
        }

        void ApplyOperatorSpeed()
        {
            var me = LocalSlot; if (me?.Operator == null) return;
            var op = OperatorDatabase.Find(me.Operator); if (op == null) return;
            // Base Among Us speed is 2.5. Property name may differ by game version.
            PlayerControl.LocalPlayer.MyPhysics.Speed = 2.5f * op.MoveSpeed;
        }

    }

    /// <summary>Short-lived bullet line. Cheap placeholder for real effects.</summary>
    public static class Tracer
    {
        static Material? _mat;
        public static void Draw(Vector2 a, Vector2 b)
        {
            var go = new GameObject("Tracer");
            var lr = go.AddComponent<LineRenderer>();
            _mat ??= new Material(Shader.Find("Sprites/Default"));
            lr.material = _mat;
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(a.x, a.y, -1f));
            lr.SetPosition(1, new Vector3(b.x, b.y, -1f));
            lr.startWidth = lr.endWidth = 0.05f;
            lr.sortingOrder = 100;
            lr.startColor = lr.endColor = new Color(1f, 0.9f, 0.4f, 0.9f);
            UnityEngine.Object.Destroy(go, 0.06f);
        }
    }
}
