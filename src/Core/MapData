using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SiegeUs.Core
{
    public enum WallKind
    {
        Hard,          // indestructible exterior / structural
        Soft,          // breakable by explosives, shotguns, melee, gadgets
        Reinforceable, // soft wall that defenders can armor-plate during Prep
        Glass          // barricade-able, see-through, breakable
    }

    public sealed class FloorDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public int Index { get; set; }
    }

    public sealed class RoomDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Floor { get; set; } = "";
        /// <summary>x, y, width, height in map units (1 unit = 1 meter)</summary>
        public float[] Rect { get; set; } = new float[4];
    }

    public sealed class WallDef
    {
        public string Id { get; set; } = "";
        public string Floor { get; set; } = "";
        public float[] A { get; set; } = new float[2];
        public float[] B { get; set; } = new float[2];
        public WallKind Kind { get; set; } = WallKind.Hard;
        public int Hp { get; set; } = 100;
    }

    /// <summary>A rectangle in the floor/ceiling that connects two floors (stairs, trapdoor, soft hatch).</summary>
    public sealed class HatchDef
    {
        public string Id { get; set; } = "";
        public string FromFloor { get; set; } = "";
        public string ToFloor { get; set; } = "";
        public float[] Rect { get; set; } = new float[4];
        public bool Soft { get; set; }        // soft floor/ceiling: can be breached to create a vertical sightline
        public bool Walkable { get; set; }    // stairs / ladders
    }

    public sealed class SpawnDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Floor { get; set; } = "1F";
        public float[] Pos { get; set; } = new float[2];
        public float Radius { get; set; } = 3f;
    }

    public sealed class BombSiteDef
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Floor { get; set; } = "";
        public string[] Rooms { get; set; } = Array.Empty<string>();
        public float[] Rect { get; set; } = new float[4];
        public SpawnDef[] DefenderSpawns { get; set; } = Array.Empty<SpawnDef>();
    }

    public sealed class MapData
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Notes { get; set; } = "";
        public FloorDef[] Floors { get; set; } = Array.Empty<FloorDef>();
        public RoomDef[] Rooms { get; set; } = Array.Empty<RoomDef>();
        public WallDef[] Walls { get; set; } = Array.Empty<WallDef>();
        public HatchDef[] Hatches { get; set; } = Array.Empty<HatchDef>();
        public SpawnDef[] AttackerSpawns { get; set; } = Array.Empty<SpawnDef>();
        public BombSiteDef[] BombSites { get; set; } = Array.Empty<BombSiteDef>();

        static readonly JsonSerializerOptions Opts = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public static MapData FromJson(string json) =>
            JsonSerializer.Deserialize<MapData>(json, Opts) ?? throw new InvalidDataException("Map JSON is empty.");

        /// <summary>Loads a map embedded in the plugin DLL, e.g. "oregon".</summary>
        public static MapData LoadEmbedded(string id)
        {
            var asm = Assembly.GetExecutingAssembly();
            using var s = asm.GetManifestResourceStream($"SiegeUs.maps.{id}.json")
                ?? throw new FileNotFoundException($"No embedded map '{id}'.");
            using var r = new StreamReader(s);
            return FromJson(r.ReadToEnd());
        }

        /// <summary>Basic sanity checks so a bad edit to the JSON fails loudly at load time.</summary>
        public List<string> Validate()
        {
            var errs = new List<string>();
            var floorIds = new HashSet<string>();
            foreach (var f in Floors) floorIds.Add(f.Id);
            foreach (var r in Rooms) if (!floorIds.Contains(r.Floor)) errs.Add($"Room {r.Id}: unknown floor {r.Floor}");
            foreach (var w in Walls) if (!floorIds.Contains(w.Floor)) errs.Add($"Wall {w.Id}: unknown floor {w.Floor}");
            foreach (var h in Hatches)
            {
                if (!floorIds.Contains(h.FromFloor)) errs.Add($"Hatch {h.Id}: unknown floor {h.FromFloor}");
                if (!floorIds.Contains(h.ToFloor)) errs.Add($"Hatch {h.Id}: unknown floor {h.ToFloor}");
            }
            if (AttackerSpawns.Length == 0) errs.Add("No attacker spawns.");
            if (BombSites.Length == 0) errs.Add("No bomb sites.");
            return errs;
        }
    }
}
