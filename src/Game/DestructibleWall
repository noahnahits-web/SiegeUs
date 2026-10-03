using SiegeUs.Core;
using UnityEngine;

namespace SiegeUs.Game
{
    /// <summary>Wall state (plain C#). The GameObject carries a WallMarker that points back here.</summary>
    public sealed class DestructibleWall
    {
        public string WallId = "";
        public string FloorId = "";
        public WallKind Kind;
        public int Hp;
        public int ReinforcementHp;     // added by defenders in Prep (Reinforceable walls only)
        public GameObject? Go;

        public bool IsDestroyed => Hp <= 0;
        public Vector2 Position => Go != null ? (Vector2)Go.transform.position : Vector2.zero;

        /// <summary>Bullets / melee. Returns true if this hit destroyed the wall.</summary>
        public bool ApplyDamage(int dmg)
        {
            if (Kind == WallKind.Hard || IsDestroyed) return false;
            if (ReinforcementHp > 0) return false;   // bullets don't hurt reinforcement; only gadgets do
            Hp -= dmg;
            if (Hp > 0) return false;
            Break();
            return true;
        }

        public void ApplyGadgetDamage(int dmg)
        {
            if (Kind == WallKind.Hard || IsDestroyed) return;
            if (ReinforcementHp > 0) { ReinforcementHp -= dmg; if (ReinforcementHp > 0) return; dmg = 0; }
            Hp -= dmg;
            if (Hp <= 0) Break();
        }

        public void Reinforce()
        {
            if (Kind != WallKind.Reinforceable || IsDestroyed || Go == null) return;
            ReinforcementHp = 1000;
            var sr = Go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.25f, 0.25f, 0.28f);
        }

        void Break()
        {
            if (Go == null) return;
            var col = Go.GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            var sr = Go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }
}
