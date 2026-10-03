using System.Linq;
using SiegeUs.Core;
using UnityEngine;

namespace SiegeUs.Game
{
    /// <summary>
    /// Immediate-mode UI. Uses explicit Rects (GUI.*) instead of GUILayout because GUILayout's
    /// params arrays are awkward under IL2CPP interop. Plain, but robust.
    /// </summary>
    public static class SiegeUI
    {
        /// <summary>Tiny vertical layout helper: hands out stacked Rects.</summary>
        sealed class Col
        {
            readonly float _x, _w; float _y;
            public Col(float x, float y, float w) { _x = x; _y = y; _w = w; }
            public Rect Next(float h) { var r = new Rect(_x, _y, _w, h); _y += h + 4; return r; }
            public void Space(float s) => _y += s;
            public float Y => _y;
        }

        /// <summary>F9 hides the lobby panel so the mod stays out of the way during normal Among Us games.</summary>
        public static bool PanelHidden;

        static string? _selOp, _selPri, _selSec, _selSpawn, _selSite;
        static Vector2 _scroll;
        static Phase _lastPhase;
        static GUIStyle? _big, _mid, _small;

        static void Styles()
        {
            if (_big != null) return;
            _big = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _mid = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14 };
        }

        public static void Draw(SiegeGame g)
        {
            if (PlayerControl.LocalPlayer == null) return;
            Styles();
            float s = Screen.height / 1080f;                       // lay out as if the screen were 1920x1080
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1));
            float W = Screen.width / s, H = 1080f;

            if (g.Match.Phase != _lastPhase) { _lastPhase = g.Match.Phase; _selSpawn = _selSite = null; if (_lastPhase == Phase.SpawnPick) _selOp = null; }

            switch (g.Match.Phase)
            {
                case Phase.Lobby:
                    if (!g.InLobby) break;
                    if (PanelHidden) GUI.Label(new Rect(40, 120, 400, 24), "Siege Us - press F9 to open", _small);
                    else DrawLobby(g);
                    break;
                case Phase.SpawnPick: DrawHud(g, W, H); DrawSpawnPick(g, W); break;
                case Phase.OperatorSelect: DrawHud(g, W, H); DrawOperatorSelect(g); break;
                case Phase.Prep:
                case Phase.Action: DrawHud(g, W, H); DrawRoomLabels(g); break;
                case Phase.RoundEnd: DrawHud(g, W, H); DrawRoundEnd(g, W); break;
                case Phase.MatchEnd: DrawHud(g, W, H); DrawMatchEnd(g, W); break;
            }
        }

        static string PlayerName(byte id)
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
                if (pc != null && pc.PlayerId == id) return pc.Data != null ? pc.Data.PlayerName : "P" + id;
            return "P" + id;
        }

        // ------------------------------------------------------------ lobby

        static void DrawLobby(SiegeGame g)
        {
            GUI.Box(new Rect(40, 120, 430, 600), "");
            var c = new Col(55, 130, 400);
            GUI.Label(c.Next(30), "SIEGE US - Private Match  (F9 hides)", _mid);
            c.Space(6);
            GUI.Label(c.Next(22), "Map", _small);
            GUI.Toggle(c.Next(30), true, " Oregon");                  // add more maps to this list later

            c.Space(8);
            GUI.Label(c.Next(22), "Pick your team", _small);
            var me = g.LocalSlot;
            var row = c.Next(42);
            int i = 0;
            foreach (var t in new[] { TeamId.Blue, TeamId.Orange })
            {
                int n = g.Match.Players.Values.Count(p => p.Team == t);
                bool mine = me != null && me.Team == t;
                var r = new Rect(row.x + i++ * 205, row.y, 195, row.height);
                if (GUI.Toggle(r, mine, $"{t} ({n}/5)", GUI.skin.button) && !mine) g.JoinTeam(t);
            }
            foreach (var t in new[] { TeamId.Blue, TeamId.Orange })
                GUI.Label(c.Next(40), t + ": " + string.Join(", ", g.Match.Players.Values.Where(p => p.Team == t).Select(p => PlayerName(p.PlayerId))), _small);

            if (g.AmHost)
            {
                c.Space(8);
                GUI.Label(c.Next(22), $"First to {g.Config.RoundsToWin} round wins", _small);
                g.Config.RoundsToWin = Mathf.RoundToInt(GUI.HorizontalSlider(c.Next(22), g.Config.RoundsToWin, 1, 12));
                g.Config.SwapSidesEachRound = GUI.Toggle(c.Next(28), g.Config.SwapSidesEachRound, " Swap attack/defense every round");
                g.Config.OneOperatorPerTeam = GUI.Toggle(c.Next(28), g.Config.OneOperatorPerTeam, " One of each operator per team");
                c.Space(10);
                if (GUI.Button(c.Next(54), "START MATCH")) g.RequestStart();
            }
            else GUI.Label(c.Next(26), "Waiting for the host to start...", _small);
        }

        // ------------------------------------------------------------ HUD

        static void DrawHud(SiegeGame g, float W, float H)
        {
            var m = g.Match;
            GUI.Label(new Rect(W / 2 - 300, 8, 600, 40), $"BLUE {m.Score[TeamId.Blue]}  -  {m.Score[TeamId.Orange]} ORANGE", _big);
            string phase = m.Phase switch
            {
                Phase.SpawnPick => "CHOOSE SPAWN / SITE", Phase.OperatorSelect => "OPERATOR SELECT",
                Phase.Prep => "PREP PHASE", Phase.Action => "ACTION PHASE",
                Phase.RoundEnd => "ROUND OVER", Phase.MatchEnd => "MATCH OVER", _ => ""
            };
            var me = g.LocalSlot;
            string side = me != null ? (m.SideOf(me.Team) == Side.Attackers ? "ATTACKING" : "DEFENDING") : "";
            GUI.Label(new Rect(W / 2 - 400, 46, 800, 28),
                $"Round {m.RoundNumber}  |  First to {m.Config.RoundsToWin}  |  {side}  |  {phase}  {Mathf.Max(0, Mathf.CeilToInt(m.PhaseTimeLeft))}s", _mid);
            if (m.Bomb == BombState.Planted)
                GUI.Label(new Rect(W / 2 - 300, 76, 600, 28), $"BOMB PLANTED - {Mathf.CeilToInt(m.BombTimeLeft)}s", _mid);

            if (me == null) return;
            GUI.Box(new Rect(30, H - 130, 320, 100), "");
            GUI.Label(new Rect(40, H - 124, 300, 28), me.Operator ?? "(no operator)", _mid);
            GUI.Label(new Rect(40, H - 90, 300, 28), me.Alive ? $"HP {Mathf.Max(0, me.Hp)} / {me.MaxHp}" : "ELIMINATED", _mid);

            var w = g.CurrentWeapon(me);
            if (w != null)
            {
                GUI.Box(new Rect(W - 350, H - 130, 320, 100), "");
                GUI.Label(new Rect(W - 340, H - 124, 300, 28), w.Name, _mid);
                GUI.Label(new Rect(W - 340, H - 90, 300, 28), g.Reloading ? "RELOADING" : $"{g.Ammo[g.WeaponSlot]} / {w.MagSize}", _mid);
            }
            if (g.World != null)
                GUI.Label(new Rect(30, 8, 700, 30), $"Floor {g.World.CurrentFloor}  |  1/2 weapon  R reload  V melee  E up  Q down  F plant/defuse  T reinforce", _small);
            if (g.BombProgress > 0)
                GUI.Box(new Rect(W / 2 - 150, H - 200, 300f * g.BombProgress / 7f, 18), "");
        }

        static void DrawRoomLabels(SiegeGame g)
        {
            var cam = Camera.main; if (cam == null || g.World == null) return;
            float s = Screen.height / 1080f;
            foreach (var r in g.Map.Rooms.Where(r => r.Floor == g.World.CurrentFloor))
            {
                var world = MapBuilder.Origin + new Vector2(r.Rect[0] + r.Rect[2] / 2, -(r.Rect[1] + r.Rect[3] / 2));
                var sp = cam.WorldToScreenPoint(world);
                if (sp.z < 0) continue;
                GUI.Label(new Rect(sp.x / s - 80, (Screen.height - sp.y) / s - 10, 160, 20), r.Name, _small);
            }
        }

        // ------------------------------------------------------------ spawn / site pick

        static void DrawSpawnPick(SiegeGame g, float W)
        {
            var me = g.LocalSlot; if (me == null) return;
            bool atk = g.Match.SideOf(me.Team) == Side.Attackers;
            GUI.Box(new Rect(W / 2 - 260, 260, 520, 440), "");
            var c = new Col(W / 2 - 245, 270, 490);
            GUI.Label(c.Next(30), atk ? "Choose your attacker spawn" : "Choose the bomb site to defend", _mid);
            GUI.Label(c.Next(22), "Majority of your team wins the vote.", _small);
            c.Space(6);
            if (atk)
                foreach (var s in g.Map.AttackerSpawns)
                {
                    if (GUI.Toggle(c.Next(46), _selSpawn == s.Id, s.Name, GUI.skin.button) && _selSpawn != s.Id)
                    { _selSpawn = s.Id; g.VoteSpawn(s.Id); }
                }
            else
                foreach (var s in g.Map.BombSites)
                {
                    if (GUI.Toggle(c.Next(46), _selSite == s.Id, $"[{s.Floor}] {s.Name}", GUI.skin.button) && _selSite != s.Id)
                    { _selSite = s.Id; g.VoteSite(s.Id); }
                }
        }

        // ------------------------------------------------------------ operator select

        static void DrawOperatorSelect(SiegeGame g)
        {
            var me = g.LocalSlot; if (me == null) return;
            var side = g.Match.SideOf(me.Team);
            var taken = g.Match.Players.Values.Where(p => p.Team == me.Team && p.PlayerId != me.PlayerId && p.Operator != null)
                .Select(p => p.Operator!).ToHashSet();
            var ops = OperatorDatabase.ForSide(side).ToList();

            GUI.Box(new Rect(60, 120, 560, 800), "");
            GUI.Label(new Rect(70, 125, 540, 30), $"Operators ({side})", _mid);
            const float rowH = 38f;
            var view = new Rect(0, 0, 520, ops.Count * rowH);
            _scroll = GUI.BeginScrollView(new Rect(70, 160, 540, 750), _scroll, view);
            for (int i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                bool isTaken = g.Match.Config.OneOperatorPerTeam && !op.Name.StartsWith("Recruit") && taken.Contains(op.Name);
                GUI.enabled = !isTaken;
                string label = $"{op.Name}   (speed {op.Speed}, {op.MaxHealth} HP)" + (isTaken ? "  - taken" : "");
                if (GUI.Toggle(new Rect(0, i * rowH, 520, rowH - 4), _selOp == op.Name, label, GUI.skin.button) && _selOp != op.Name)
                { _selOp = op.Name; _selPri = op.Primaries.FirstOrDefault(); _selSec = op.Secondaries.FirstOrDefault(); }
                GUI.enabled = true;
            }
            GUI.EndScrollView();

            GUI.Box(new Rect(640, 120, 620, 800), "");
            var sel = _selOp != null ? OperatorDatabase.Find(_selOp) : null;
            var c = new Col(655, 130, 590);
            if (sel == null) { GUI.Label(c.Next(30), "Pick an operator", _mid); return; }

            GUI.Label(c.Next(40), sel.Name, _big);
            GUI.Label(c.Next(44), "Ability: " + sel.Ability, _small);
            GUI.Label(c.Next(44), "Gadgets: " + string.Join(", ", sel.Gadgets), _small);
            c.Space(6);
            GUI.Label(c.Next(22), "Primary", _small);
            foreach (var p in sel.Primaries)
            {
                var w = WeaponDatabase.Find(p);
                if (GUI.Toggle(c.Next(32), _selPri == p, $"{p}   ({w?.Damage} dmg, {w?.Rpm} rpm, {w?.MagSize} mag)", GUI.skin.button)) _selPri = p;
            }
            c.Space(6);
            GUI.Label(c.Next(22), "Secondary", _small);
            foreach (var p in sel.Secondaries)
            {
                var w = WeaponDatabase.Find(p);
                if (GUI.Toggle(c.Next(32), _selSec == p, $"{p}   ({w?.Damage} dmg, {w?.Rpm} rpm, {w?.MagSize} mag)", GUI.skin.button)) _selSec = p;
            }
            c.Space(10);
            if (GUI.Button(c.Next(54), "LOCK IN")) g.PickOperator(sel.Name, _selPri, _selSec);
            if (me.Operator != null) GUI.Label(c.Next(30), "Locked in: " + me.Operator, _mid);
        }

        // ------------------------------------------------------------ end screens

        static void DrawRoundEnd(SiegeGame g, float W)
        {
            var r = g.Match.LastRound; if (r == null) return;
            GUI.Box(new Rect(W / 2 - 300, 300, 600, 120), "");
            GUI.Label(new Rect(W / 2 - 300, 310, 600, 50), $"{r.Winner} WINS THE ROUND", _big);
            GUI.Label(new Rect(W / 2 - 300, 365, 600, 30), r.Reason.ToString(), _mid);
        }

        static void DrawMatchEnd(SiegeGame g, float W)
        {
            GUI.Box(new Rect(W / 2 - 320, 300, 640, 230), "");
            GUI.Label(new Rect(W / 2 - 320, 320, 640, 60), $"{g.Match.Winner} WINS THE MATCH", _big);
            GUI.Label(new Rect(W / 2 - 320, 385, 640, 40), $"{g.Match.Score[TeamId.Blue]} - {g.Match.Score[TeamId.Orange]}", _big);
            if (g.AmHost && GUI.Button(new Rect(W / 2 - 120, 450, 240, 50), "BACK TO LOBBY")) g.BackToLobby();
        }
    }
}
