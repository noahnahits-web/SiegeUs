using System.Collections.Generic;
using System.Linq;
using SiegeUs.Core;
using UnityEngine;

namespace SiegeUs.Game
{
    /// <summary>
    /// Builds the playable map from MapData. Each floor is its own root GameObject at the same
    /// x/y; only the floor the local player is on is active (so you only collide with, and see,
    /// your own floor). Movement in Among Us is client-authoritative, so this is safe locally.
    /// </summary>
    public sealed class MapBuilder
    {
        // Far away from the vanilla lobby / ship so nothing overlaps.
        public static readonly Vector2 Origin = new(400f, 400f);
        const float WallThickness = 0.3f;

        public MapData Data { get; }
        public Dictionary<string, GameObject> FloorRoots { get; } = new();
        public Dictionary<string, DestructibleWall> Walls { get; } = new();
        readonly List<DestructibleWall> _wallList = new();
        public Dictionary<string, GameObject> HatchObjects { get; } = new();
        public HashSet<string> BrokenHatches { get; } = new();
        public string CurrentFloor { get; private set; } = "1F";
        GameObject? _root;
        Sprite? _white;

        public MapBuilder(MapData data) { Data = data; }

        static Vector2 W(float[] p) => Origin + new Vector2(p[0], -p[1]);   // map y-down -> world y-up

        public static Vector2 ToWorld(float[] mapPos) => W(mapPos);

        Sprite White()
        {
            if (_white != null) return _white;
            var tex = Texture2D.whiteTexture;
            _white = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            return _white;
        }

        GameObject Quad(string name, Transform parent, Vector2 center, Vector2 size, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(center.x, center.y, 5f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = White();
            sr.color = color;
            sr.sortingOrder = order;
            return go;
        }

        public void Build()
        {
            Destroy();
            _root = new GameObject("SiegeMap_" + Data.Id);
            int shipLayer = LayerMask.NameToLayer("Ship");

            foreach (var f in Data.Floors)
            {
                var root = new GameObject("Floor_" + f.Id);
                root.transform.SetParent(_root.transform, false);
                FloorRoots[f.Id] = root;

                foreach (var r in Data.Rooms.Where(r => r.Floor == f.Id))
                {
                    var c = new Vector2(r.Rect[0] + r.Rect[2] / 2, r.Rect[1] + r.Rect[3] / 2);
                    var shade = 0.30f + 0.04f * ((r.Id.GetHashCode() & 3));
                    Quad("Room_" + r.Id, root.transform, Origin + new Vector2(c.x, -c.y),
                         new Vector2(r.Rect[2], r.Rect[3]), new Color(shade, shade, shade + 0.03f), -30);
                }

                foreach (var w in Data.Walls.Where(w => w.Floor == f.Id))
                {
                    var a = W(w.A); var b = W(w.B);
                    bool horizontal = Mathf.Abs(a.y - b.y) < 0.001f;
                    var size = horizontal ? new Vector2(Mathf.Abs(b.x - a.x) + WallThickness, WallThickness)
                                          : new Vector2(WallThickness, Mathf.Abs(b.y - a.y) + WallThickness);
                    var go = Quad("Wall_" + w.Id, root.transform, (a + b) / 2, size, WallColor(w.Kind), -28);
                    go.layer = shipLayer;
                    var col = go.AddComponent<BoxCollider2D>();
                    col.size = Vector2.one;
                    // Glass blocks movement but not shots (see SiegeGame.CastBullet)
                    var dw = new DestructibleWall { WallId = w.Id, FloorId = f.Id, Kind = w.Kind, Hp = w.Hp, Go = go };
                    var marker = go.AddComponent<WallMarker>();
                    marker.Index = _wallList.Count;
                    _wallList.Add(dw);
                    Walls[w.Id] = dw;
                }
            }

            foreach (var h in Data.Hatches)
            {
                // Draw the hatch on the lower floor's ceiling AND the upper floor's floor.
                foreach (var fid in new[] { h.FromFloor, h.ToFloor })
                {
                    var c = new Vector2(h.Rect[0] + h.Rect[2] / 2, h.Rect[1] + h.Rect[3] / 2);
                    var color = h.Walkable ? new Color(0.9f, 0.8f, 0.3f)
                              : h.Soft ? new Color(0.75f, 0.55f, 0.35f) : new Color(0.2f, 0.2f, 0.2f);
                    var go = Quad("Hatch_" + h.Id + "_" + fid, FloorRoots[fid].transform,
                                  Origin + new Vector2(c.x, -c.y), new Vector2(h.Rect[2], h.Rect[3]), color, -29);
                    HatchObjects[h.Id + "_" + fid] = go;
                }
            }

            SetFloor(CurrentFloor);
        }

        static Color WallColor(WallKind k) => k switch
        {
            WallKind.Hard => new Color(0.12f, 0.12f, 0.14f),
            WallKind.Soft => new Color(0.80f, 0.68f, 0.48f),
            WallKind.Reinforceable => new Color(0.62f, 0.45f, 0.28f),
            WallKind.Glass => new Color(0.6f, 0.85f, 1f, 0.45f),
            _ => Color.white
        };

        public void SetFloor(string floorId)
        {
            CurrentFloor = floorId;
            foreach (var kv in FloorRoots) kv.Value.SetActive(kv.Key == floorId);
        }

        public HatchDef? HatchAt(string floor, Vector2 worldPos, bool walkableOnly = false)
        {
            foreach (var h in Data.Hatches)
            {
                if (h.FromFloor != floor && h.ToFloor != floor) continue;
                if (walkableOnly && !h.Walkable) continue;
                var min = Origin + new Vector2(h.Rect[0], -(h.Rect[1] + h.Rect[3]));
                var rect = new Rect(min.x, min.y, h.Rect[2], h.Rect[3]);
                if (rect.Contains(worldPos)) return h;
            }
            return null;
        }

        /// <summary>Maps a raycast hit back to its wall state (null if the collider isn't one of ours).</summary>
        public DestructibleWall? WallFromCollider(Collider2D col)
        {
            if (col == null) return null;
            var m = col.GetComponent<WallMarker>();
            return m != null && m.Index >= 0 && m.Index < _wallList.Count ? _wallList[m.Index] : null;
        }

        public void BreakHatch(string hatchId)
        {
            BrokenHatches.Add(hatchId);
            foreach (var kv in HatchObjects.Where(k => k.Key.StartsWith(hatchId + "_")))
                kv.Value.GetComponent<SpriteRenderer>().color = new Color(0f, 0f, 0f, 0.8f);
        }

        public void Destroy()
        {
            if (_root != null) Object.Destroy(_root);
            _root = null;
            FloorRoots.Clear(); Walls.Clear(); _wallList.Clear(); HatchObjects.Clear(); BrokenHatches.Clear();
        }
    }
}
