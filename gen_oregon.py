#!/usr/bin/env python3
"""
Generates maps/oregon.json from a room layout.

HOW TO MAKE THIS A TRUE 1:1 OREGON
  The layout below is a BLOCKOUT built from memory. It has the right *kind* of
  structure (basement + 3 floors, hall spine, soft walls everywhere, soft
  floors/ceilings, 4 bomb-site pairs, outdoor attacker spawns) but it is not
  guaranteed to match the real map. To fix that:
    1. Get the Oregon blueprints (in-game map screens / community blueprint
       images) and trace each floor.
    2. Edit ROOMS (x, y, w, h in meters) so each room matches the real one.
    3. Edit EXTRA_DOORS / WINDOWS / HARD_ZONES for the exceptions.
    4. Run:  python3 tools/gen_oregon.py
  Everything else (walls, soft floors, hatches, validation) is regenerated.
"""
import json, itertools, os

FLOORS = [("B", "Basement", 0), ("1F", "First Floor", 1), ("2F", "Second Floor", 2), ("3F", "Tower", 3)]

# (id, name, floor, x, y, w, h, is_hall)
ROOMS = [
    # ---- Basement
    ("b_laundry",  "Laundry",        "B", 0, 0, 16, 12, False),
    ("b_supply",   "Supply Room",    "B", 16, 0, 14, 12, False),
    ("b_freezer",  "Freezer",        "B", 30, 0, 18, 12, False),
    ("b_hall",     "Basement Hall",  "B", 0, 12, 48, 6, True),
    ("b_boiler",   "Boiler Room",    "B", 0, 18, 16, 12, False),
    ("b_garage",   "Garage",         "B", 16, 18, 32, 12, False),
    # ---- First floor
    ("f1_dining",  "Dining Hall",    "1F", 0, 0, 16, 12, False),
    ("f1_kitchen", "Kitchen",        "1F", 16, 0, 14, 12, False),
    ("f1_meeting", "Meeting Hall",   "1F", 30, 0, 18, 12, False),
    ("f1_hall",    "Main Hall",      "1F", 0, 12, 48, 6, True),
    ("f1_lobby",   "Lobby",          "1F", 0, 18, 16, 12, False),
    ("f1_office",  "Office",         "1F", 16, 18, 14, 12, False),
    ("f1_storage", "Storage",        "1F", 30, 18, 18, 12, False),
    # ---- Second floor
    ("f2_kids",    "Kids Dorms",     "2F", 0, 0, 16, 12, False),
    ("f2_dorms",   "Dorms Main Hall","2F", 16, 0, 14, 12, False),
    ("f2_master",  "Master Bedroom", "2F", 30, 0, 18, 12, False),
    ("f2_hall",    "Upper Hall",     "2F", 0, 12, 48, 6, True),
    ("f2_gym",     "Gym",            "2F", 0, 18, 16, 12, False),
    ("f2_library", "Library",        "2F", 16, 18, 14, 12, False),
    ("f2_study",   "Study",          "2F", 30, 18, 18, 12, False),
    # ---- Tower (third floor)
    ("t_meeting",  "Tower Meeting Hall", "3F", 8, 6, 16, 12, False),
    ("t_landing",  "Tower Landing",      "3F", 24, 6, 16, 12, False),
]

# Bomb site pairs: id, name, floor, [room ids]
SITES = [
    ("site_basement", "Laundry / Supply Room",     "B",  ["b_laundry", "b_supply"]),
    ("site_1f",       "Dining Hall / Kitchen",     "1F", ["f1_dining", "f1_kitchen"]),
    ("site_2f",       "Kids Dorms / Dorms Main",   "2F", ["f2_kids", "f2_dorms"]),
    ("site_tower",    "Tower Meeting / Landing",   "3F", ["t_meeting", "t_landing"]),
]

# Outdoor attacker spawns (map units; building is 0..48 x 0..30, outside is negative / beyond)
ATTACKER_SPAWNS = [
    ("spawn_parking", "Parking Lot",  "1F", [24, 42]),
    ("spawn_front",   "Front Gate",   "1F", [-14, 24]),
    ("spawn_lake",    "Lake Shore",   "1F", [62, 15]),
    ("spawn_garden",  "Back Garden",  "1F", [24, -14]),
]

# Extra openings: (floor, orientation 'h'|'v', fixed_coord, start, end)
EXTRA_DOORS = [
    ("1F", "h", 30, 6, 8),   # front door (bottom of lobby, building edge y=30)
    ("1F", "h", 0, 22, 24),  # back door (top of kitchen)
    ("B",  "h", 30, 30, 34), # garage door
]
# Windows (glass): (floor, orientation, fixed, start, end)
WINDOWS = [
    ("1F", "h", 0, 6, 8), ("1F", "h", 0, 38, 40), ("1F", "v", 0, 4, 6), ("1F", "v", 48, 4, 6),
    ("1F", "h", 30, 22, 24), ("1F", "h", 30, 38, 40), ("1F", "v", 0, 22, 24), ("1F", "v", 48, 22, 24),
    ("2F", "h", 0, 6, 8), ("2F", "h", 0, 22, 24), ("2F", "h", 0, 38, 40), ("2F", "v", 0, 22, 24),
    ("2F", "h", 30, 6, 8), ("2F", "h", 30, 22, 24), ("2F", "v", 48, 22, 24),
]
# Rooms whose floor stays HARD (everything else has soft ceilings/floors)
HARD_FLOOR_ROOMS = {"b_laundry", "b_supply", "b_freezer", "b_hall", "b_boiler", "b_garage"}  # basement slab is concrete
# Stairs (walkable hatches) between floors, in the hall spine
STAIRS = [("B", "1F", [22, 13, 4, 4]), ("1F", "2F", [22, 13, 4, 4]), ("2F", "3F", [22, 13, 4, 4])]

DOOR_W = 1.6


def merge(intervals):
    out = []
    for a, b in sorted(intervals):
        if out and a <= out[-1][1] + 1e-6:
            out[-1][1] = max(out[-1][1], b)
        else:
            out.append([a, b])
    return out


def subtract(intervals, gaps):
    out = []
    for a, b in intervals:
        cur = a
        for g0, g1 in sorted(gaps):
            if g1 <= cur or g0 >= b:
                continue
            if g0 > cur:
                out.append([cur, g0])
            cur = max(cur, g1)
        if cur < b:
            out.append([cur, b])
    return out


def main():
    rooms_by_floor = {}
    for r in ROOMS:
        rooms_by_floor.setdefault(r[2], []).append(r)
    room_by_id = {r[0]: r for r in ROOMS}

    walls, wid = [], 0

    for fid, _, _ in FLOORS:
        rs = rooms_by_floor.get(fid, [])
        xs = [r[3] for r in rs] + [r[3] + r[5] for r in rs]
        ys = [r[4] for r in rs] + [r[4] + r[6] for r in rs]
        bx0, bx1, by0, by1 = min(xs), max(xs), min(ys), max(ys)

        # collect edge intervals per line, remembering which rooms touch it
        lines = {}  # (orient, coord) -> list of (a, b, roomid)
        for rid, _, _, x, y, w, h, hall in rs:
            lines.setdefault(("h", y), []).append((x, x + w, rid))
            lines.setdefault(("h", y + h), []).append((x, x + w, rid))
            lines.setdefault(("v", x), []).append((y, y + h, rid))
            lines.setdefault(("v", x + w), []).append((y, y + h, rid))

        # doors between adjacent rooms: middle of each shared overlap >= 3m
        gaps = {}
        for key, segs in lines.items():
            for (a1, b1, r1), (a2, b2, r2) in itertools.combinations(segs, 2):
                if r1 == r2:
                    continue
                lo, hi = max(a1, a2), min(b1, b2)
                if hi - lo >= 3:
                    mid = (lo + hi) / 2
                    gaps.setdefault(key, []).append((mid - DOOR_W / 2, mid + DOOR_W / 2))
        for (f, o, c, a, b) in EXTRA_DOORS:
            if f == fid:
                gaps.setdefault((o, c), []).append((a, b))
        win = {}
        for (f, o, c, a, b) in WINDOWS:
            if f == fid:
                win.setdefault((o, c), []).append((a, b))

        for (o, c), segs in sorted(lines.items()):
            merged = merge([(a, b) for a, b, _ in segs])
            exterior = (o == "h" and c in (by0, by1)) or (o == "v" and c in (bx0, bx1))
            kind_roomids = {rid for _, _, rid in segs}
            touches_hall = any(room_by_id[r][7] for r in kind_roomids)
            for a, b in subtract(merged, gaps.get((o, c), [])):
                # carve windows out of exterior walls
                pieces = subtract([[a, b]], win.get((o, c), [])) if exterior else [[a, b]]
                for pa, pb in pieces:
                    if pb - pa < 0.05:
                        continue
                    wid += 1
                    kind = "Hard" if exterior else ("Soft" if touches_hall else "Reinforceable")
                    hp = 99999 if exterior else 100
                    A = [pa, c] if o == "h" else [c, pa]
                    B = [pb, c] if o == "h" else [c, pb]
                    walls.append({"id": f"w{wid}", "floor": fid, "a": A, "b": B, "kind": kind, "hp": hp})
                if exterior:
                    for wa, wb in win.get((o, c), []):
                        if wa >= a - 1e-6 and wb <= b + 1e-6:
                            wid += 1
                            A = [wa, c] if o == "h" else [c, wa]
                            B = [wb, c] if o == "h" else [c, wb]
                            walls.append({"id": f"w{wid}", "floor": fid, "a": A, "b": B, "kind": "Glass", "hp": 20})

    # Soft floors / ceilings: every upper room that sits over another room gets a breakable hatch
    hatches, hid = [], 0
    floor_order = [f[0] for f in FLOORS]
    for upper in ROOMS:
        ui = floor_order.index(upper[2])
        if ui == 0:
            continue
        lower_floor = floor_order[ui - 1]
        for lower in rooms_by_floor.get(lower_floor, []):
            if lower[0] in HARD_FLOOR_ROOMS and upper[2] == "1F":
                continue  # basement ceiling under 1F is concrete in this blockout
            ox0, oy0 = max(upper[3], lower[3]), max(upper[4], lower[4])
            ox1, oy1 = min(upper[3] + upper[5], lower[3] + lower[5]), min(upper[4] + upper[6], lower[4] + lower[6])
            if ox1 - ox0 >= 2 and oy1 - oy0 >= 2:
                # one soft hatch, 2x2m, centered in the overlap
                cx, cy = (ox0 + ox1) / 2, (oy0 + oy1) / 2
                hid += 1
                hatches.append({"id": f"h{hid}", "fromFloor": lower_floor, "toFloor": upper[2],
                                "rect": [cx - 1, cy - 1, 2, 2], "soft": True, "walkable": False})
    for i, (a, b, rect) in enumerate(STAIRS):
        hatches.append({"id": f"stairs{i+1}", "fromFloor": a, "toFloor": b, "rect": rect, "soft": False, "walkable": True})

    rooms = [{"id": r[0], "name": r[1], "floor": r[2], "rect": [r[3], r[4], r[5], r[6]]} for r in ROOMS]

    sites = []
    for sid, name, floor, rids in SITES:
        rects = [room_by_id[r] for r in rids]
        x0 = min(r[3] for r in rects); y0 = min(r[4] for r in rects)
        x1 = max(r[3] + r[5] for r in rects); y1 = max(r[4] + r[6] for r in rects)
        spawns = []
        for r in rects:
            for k, (fx, fy) in enumerate([(0.25, 0.3), (0.7, 0.3), (0.5, 0.75)]):
                spawns.append({"id": f"{sid}_{r[0]}_{k}", "name": r[1], "floor": floor,
                               "pos": [round(r[3] + r[5] * fx, 2), round(r[4] + r[6] * fy, 2)], "radius": 1.5})
        sites.append({"id": sid, "name": name, "floor": floor, "rooms": rids,
                      "rect": [x0, y0, x1 - x0, y1 - y0], "defenderSpawns": spawns[:5]})

    out = {
        "id": "oregon",
        "name": "Oregon",
        "notes": "Blockout layout generated by tools/gen_oregon.py. Retrace against real blueprints for 1:1 accuracy.",
        "floors": [{"id": f, "name": n, "index": i} for f, n, i in FLOORS],
        "rooms": rooms,
        "walls": walls,
        "hatches": hatches,
        "attackerSpawns": [{"id": i, "name": n, "floor": f, "pos": p, "radius": 3} for i, n, f, p in ATTACKER_SPAWNS],
        "bombSites": sites,
    }
    path = os.path.join(os.path.dirname(__file__), "..", "maps", "oregon.json")
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(out, fh, indent=1)
    kinds = {}
    for w in walls:
        kinds[w["kind"]] = kinds.get(w["kind"], 0) + 1
    print(f"wrote {path}: {len(rooms)} rooms, {len(walls)} walls {kinds}, {len(hatches)} hatches, {len(sites)} sites")


if __name__ == "__main__":
    main()
