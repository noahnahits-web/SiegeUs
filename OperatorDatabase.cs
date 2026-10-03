using System;
using System.Collections.Generic;
using System.Linq;

namespace SiegeUs.Core
{
    public sealed class Operator
    {
        public string Name { get; init; } = "";
        public Side Side { get; init; }
        /// <summary>1 = slow/armored, 2 = medium, 3 = fast/light (same as Siege).</summary>
        public int Speed { get; init; }
        public string[] Primaries { get; init; } = Array.Empty<string>();
        public string[] Secondaries { get; init; } = Array.Empty<string>();
        public string[] Gadgets { get; init; } = Array.Empty<string>();
        public string Ability { get; init; } = "";

        // Siege values: speed 1 = 125 HP, speed 2 = 110 HP, speed 3 = 100 HP.
        public int MaxHealth => Speed switch { 1 => 125, 2 => 110, _ => 100 };
        // Top-down move speed multiplier relative to base player speed.
        public float MoveSpeed => Speed switch { 1 => 0.85f, 2 => 1.0f, _ => 1.15f };
    }

    /// <summary>
    /// NOTE: Loadouts and abilities are best-effort from memory of Siege and
    /// Siege changes them every season. Edit this file to match the current game.
    /// Every weapon name here must exist in WeaponDatabase (validated at startup).
    /// </summary>
    public static class OperatorDatabase
    {
        static string[] L(params string[] x) => x;

        static Operator A(string n, int spd, string[] pri, string[] sec, string[] gad, string ability) =>
            new() { Name = n, Side = Side.Attackers, Speed = spd, Primaries = pri, Secondaries = sec, Gadgets = gad, Ability = ability };
        static Operator D(string n, int spd, string[] pri, string[] sec, string[] gad, string ability) =>
            new() { Name = n, Side = Side.Defenders, Speed = spd, Primaries = pri, Secondaries = sec, Gadgets = gad, Ability = ability };

        static readonly string[] AtkGadgetsA = L("Frag Grenade", "Smoke Grenade");
        static readonly string[] AtkGadgetsB = L("Breach Charge", "Stun Grenade");
        static readonly string[] AtkGadgetsC = L("Claymore", "Hard Breach Charge");
        static readonly string[] AtkGadgetsD = L("Stun Grenade", "Claymore");
        static readonly string[] DefGadgetsA = L("Barbed Wire", "Impact Grenade");
        static readonly string[] DefGadgetsB = L("Deployable Shield", "Proximity Alarm");
        static readonly string[] DefGadgetsC = L("Bulletproof Camera", "Nitro Cell");
        static readonly string[] DefGadgetsD = L("Barbed Wire", "Bulletproof Camera");

        public static readonly IReadOnlyList<Operator> All = new List<Operator>
        {
            // ---------------------------------------------------------------- ATTACKERS
            A("Sledge",     2, L("L85A2","M590A1"),        L("P226 Mk 25","SMG-11"),     AtkGadgetsA, "Breaching Hammer: melee breaks soft walls/hatches silently"),
            A("Thatcher",   2, L("AR33","L85A2","M590A1"), L("P226 Mk 25"),              AtkGadgetsB, "EMP Grenades: disable electronic gadgets in radius"),
            A("Ash",        3, L("R4-C","G36C"),           L("5.7 USG","M45 MEUSOC"),    AtkGadgetsB, "Breaching Round: ranged breach of soft walls"),
            A("Thermite",   2, L("556XI","M1014"),         L("5.7 USG","M45 MEUSOC"),    AtkGadgetsD, "Exothermic Charge: opens reinforced walls/hatches"),
            A("Twitch",     2, L("F2","417","SG-CQB"),     L("P9","LFP586"),             AtkGadgetsA, "Shock Drone: destroys defender electronics, damages players"),
            A("Montagne",   1, L("Shield"),                L("P9","LFP586"),             AtkGadgetsA, "Extendable shield: full frontal protection"),
            A("Glaz",       2, L("OTs-03"),                L("PMM","GSh-18"),            AtkGadgetsA, "Flip Sight: sees through smoke"),
            A("Fuze",       1, L("6P41","AK-12"),          L("PMM","GSh-18"),            AtkGadgetsB, "Cluster Charge: sub-bombs through walls/ceilings"),
            A("Blitz",      2, L("Shield"),                L("P12"),                     AtkGadgetsA, "Flash Shield: blinds and slows enemies"),
            A("IQ",         3, L("552 Commando","G8A1"),   L("P12"),                     AtkGadgetsB, "Electronics Detector: shows nearby gadgets through walls"),
            A("Buck",       2, L("C8-SFW","CAMRS"),        L("MK1 9mm"),                 AtkGadgetsB, "Skeleton Key: underbarrel shotgun, breaches soft surfaces"),
            A("Blackbeard", 2, L("MK17 CQB","SR-25"),      L("D-50"),                    AtkGadgetsB, "Rifle Shield: absorbs headshots"),
            A("Capitão",    3, L("PARA-308"),              L("PRB92","Gonne-6"),         AtkGadgetsB, "Crossbow: smoke and fire bolts"),
            A("Hibana",     3, L("Type-89","SuperNova"),   L("P229","Bearing 9"),        AtkGadgetsD, "X-KAIROS: burst charges on reinforced walls"),
            A("Jackal",     2, L("C7E","PDW9","ITA12L"),   L("USP40"),                  AtkGadgetsA, "Eyenox: tracks footprints"),
            A("Ying",       2, L("T-95 LSW","SIX12"),      L("Q-929"),                   AtkGadgetsD, "Candela: flashbang-like blinding grenades"),
            A("Zofia",      2, L("LMG-E","M762"),          L("RG15"),                    AtkGadgetsA, "KS79 Lifeline: impact/concussion grenades"),
            A("Dokkaebi",   2, L("Mk 14 EBR","BOSG.12.2"), L("SMG-12","C75 Auto"),       AtkGadgetsA, "Logic Bomb: rings defender phones, hacks cameras"),
            A("Lion",       2, L("V308","417","SG-CQB"),   L("LFP586"),                  AtkGadgetsA, "EE-ONE-D: scan reveals moving enemies"),
            A("Finka",      2, L("SPEAR .308","6P41","SASG-12"), L("PMM","GSh-18"),      AtkGadgetsA, "Adrenal Surge: temporary health boost"),
            A("Maverick",   2, L("AR-15.50","M4"),         L("1911 TACOPS"),             AtkGadgetsB, "Blowtorch: burns small holes in reinforced walls"),
            A("Nomad",      2, L("AK-74M","ARX200"),       L(".44 Vendetta"),            AtkGadgetsB, "Airjab Launcher: knockback mines"),
            A("Gridlock",   1, L("F90","M249 SAW"),        L("Super Shorty","SDP 9mm"),  AtkGadgetsB, "Trax Stingers: caltrop-like area denial"),
            A("Nøkk",       2, L("FMG-9","SIX12 SD"),      L("D-50"),                    AtkGadgetsA, "HEL Presence Reduction: silent step, hides from cams"),
            A("Amaru",      2, L("G8A1","SMG-11"),         L("Gonne-6"),                 AtkGadgetsA, "Garra Hook: grapple through windows/hatches"),
            A("Kali",       2, L("CSRX 300"),              L("SPSMG9","C75 Auto"),       AtkGadgetsB, "LV Explosive Lance: damages gadgets/reinforcements"),
            A("Iana",       2, L("ARX200","G36C"),         L("MK1 9mm"),                 AtkGadgetsA, "Gemini Replicator: holographic clone"),
            A("Ace",        2, L("AK-12","M1014"),         L("P9"),                      AtkGadgetsA, "S.E.L.M.A. Aqua Breacher: breaches hard surfaces"),
            A("Zero",       2, L("SC3000K","MP7"),         L("5.7 USG","Gonne-6"),       AtkGadgetsA, "Argus Launcher: cameras that shoot through walls"),
            A("Flores",     2, L("AR33","SR-25"),          L("GSh-18"),                  AtkGadgetsA, "RCE-Ratero Charge: remote-driven explosive"),
            A("Osa",        2, L("556XI","PDW9"),          L("PMM"),                     AtkGadgetsA, "Talon-8 Clear Shield: transparent shield"),
            A("Sens",       2, L("POF-9","417"),           L("P-10C"),                   AtkGadgetsA, "R.O.U. Projector: deployable light-blocking screens"),
            A("Grim",       2, L("552 Commando","SG-CQB"), L("P229","SMG-11"),           AtkGadgetsA, "Kawan Hive Launcher: tracking bugs"),
            A("Brava",      2, L("PARA-308","CAMRS"),      L("Super Shorty","USP40"),    AtkGadgetsA, "Kludge Drone: hacks defender gadgets"),
            A("Ram",        2, L("R4-C","LMG-E"),          L("MK1 9mm"),                 AtkGadgetsA, "BU-GI Auto Breacher: rolling robot breaches"),
            A("Deimos",     2, L("AK-74M","M249"),         L("P9"),                      AtkGadgetsA, "DeathMARK: tracks a marked enemy"),
            A("Recruit ATK",2, L("R4-C","L85A2","M590A1"), L("P226 Mk 25"),              AtkGadgetsB, "None"),

            // ---------------------------------------------------------------- DEFENDERS
            D("Smoke",      2, L("FMG-9","M590A1"),        L("P226 Mk 25","SMG-11"),     DefGadgetsA, "Toxic Babes: remote gas grenades"),
            D("Mute",       2, L("MP5K","M590A1"),         L("P226 Mk 25","SMG-11"),     DefGadgetsB, "Signal Disruptors: jam drones and remote devices"),
            D("Castle",     1, L("UMP45","M1014"),         L("5.7 USG","Super Shorty"),  DefGadgetsA, "Armor Panels: bulletproof barricade"),
            D("Pulse",      3, L("UMP45","M1014"),         L("5.7 USG","M45 MEUSOC"),    DefGadgetsB, "Cardiac Sensor: heartbeat detection through walls"),
            D("Doc",        1, L("MP5","P90","SG-CQB"),    L("P9","LFP586"),             DefGadgetsA, "Stim Pistol: heal/overheal teammates"),
            D("Rook",       1, L("MP5","P90","SG-CQB"),    L("P9","LFP586"),             DefGadgetsA, "Armor packs: extra body armor for team"),
            D("Kapkan",     2, L("9x19VSN","SASG-12"),     L("PMM","GSh-18"),            DefGadgetsA, "Entry Denial Device: tripwire explosives"),
            D("Tachanka",   1, L("DP27","9x19VSN"),        L("PMM","GSh-18"),            DefGadgetsA, "Mounted LMG / Shumikha Launcher"),
            D("Jäger",      2, L("416-C Carbine","M870"),  L("P12"),                     DefGadgetsC, "Active Defense System: shoots down projectiles"),
            D("Bandit",     3, L("MP7","M870"),            L("P12"),                     DefGadgetsC, "Shock Wire: electrifies reinforcements/wire"),
            D("Frost",      2, L("9mm C1","Super 90"),     L("MK1 9mm"),                 DefGadgetsA, "Welcome Mat: bear trap"),
            D("Valkyrie",   2, L("MPX","SPAS-12"),         L("D-50"),                    DefGadgetsA, "Black Eye: throwable cameras"),
            D("Caveira",    3, L("M12","SPAS-15"),         L("Luison"),                  DefGadgetsA, "Silent Step + Interrogation"),
            D("Echo",       2, L("MP5SD","SuperNova"),     L("P229","Bearing 9"),        DefGadgetsA, "Yokai Drone: sonic burst disorients attackers"),
            D("Mira",       2, L("Vector .45 ACP","ITA12L"), L("USP40"),                 DefGadgetsA, "Black Mirror: one-way window"),
            D("Lesion",     2, L("T-5 SMG","SIX12 SD"),    L("Q-929"),                   DefGadgetsA, "Gu Mines: poison-damage mines"),
            D("Ela",        3, L("Scorpion EVO 3 A1","FO-12"), L("RG15"),                DefGadgetsA, "Grzmot Mines: concussion mines"),
            D("Vigil",      3, L("K1A","BOSG.12.2"),       L("C75 Auto","SMG-12"),       DefGadgetsA, "ERC-7: invisible to cameras/drones"),
            D("Maestro",    2, L("ALDA 5.56","ACS12"),     L("Keratos .357","Bailiff 410"), DefGadgetsA, "Evil Eye: armored turret cameras"),
            D("Alibi",      3, L("Mx4 Storm","ACS12"),     L("Keratos .357","Bailiff 410"), DefGadgetsA, "Prisma: holographic decoys"),
            D("Clash",      1, L("CCE Shield"),            L("SPSMG9"),                  DefGadgetsA, "CCE Shield: electrified, slows attackers"),
            D("Kaid",       1, L("AUG A3","TCSG12"),       L("LFP586"),                  DefGadgetsC, "Electroclaw: electrifies reinforced surfaces"),
            D("Mozzie",     2, L("Commando 9","P10 RONI"), L("SMG-12"),                  DefGadgetsA, "Pest Launcher: hijacks attacker drones"),
            D("Warden",     2, L("MPX","M590A1"),          L("P-10C"),                   DefGadgetsA, "Glance Smart Glasses: immune to flashes"),
            D("Goyo",       2, L("Vector .45 ACP","TCSG12"), L("P229"),                  DefGadgetsA, "Volcán Shield: fire canisters"),
            D("Wamai",      2, L("AUG A3","MP5K"),         L("KS79 Lifeline"),           DefGadgetsA, "Mag-NET System: catches grenades/projectiles"),
            D("Oryx",       1, L("T-5 SMG","SPAS-12"),     L("Bailiff 410","USP40"),     DefGadgetsA, "Remah Dash: charges through soft walls"),
            D("Melusi",     2, L("MP5","Super 90"),        L("RG15"),                    DefGadgetsA, "Banshee Sonic Defense: slows attackers"),
            D("Aruni",      2, L("P10 RONI"),              L("MK1 9mm"),                 DefGadgetsA, "Surya Gate: laser barrier"),
            D("Thunderbird",2, L("SPEAR .308"),            L("Q-929"),                   DefGadgetsA, "Kóna Station: healing stations"),
            D("Thorn",      2, L("UZK50GI","M870"),        L("1911 TACOPS","Super Shorty"), DefGadgetsA, "Razorbloom Shell: delayed explosive"),
            D("Azami",      2, L("9x19VSN","ACS12"),       L("D-50"),                    DefGadgetsA, "Kiba Barrier: sticky bulletproof foam barrier"),
            D("Solis",      2, L("P90","ITA12L"),          L("SPSMG9"),                  DefGadgetsA, "SPEC-IO Electro-Sensor: locks onto electronics"),
            D("Fenrir",     2, L("MP7","SASG-12"),         L("Bailiff 410","1911 TACOPS"), DefGadgetsA, "F-NATT Dread Mine: fear-inducing mines"),
            D("Tubarão",    2, L("MPX","M1014"),           L("MK1 9mm"),                 DefGadgetsA, "Zoto Canister: freezing grenades"),
            D("Recruit DEF",2, L("MP5","P90","M870"),      L("P9"),                      DefGadgetsA, "None"),
        };

        static readonly Dictionary<string, Operator> ByName =
            All.ToDictionary(o => o.Name, StringComparer.OrdinalIgnoreCase);

        public static Operator? Find(string name) => ByName.TryGetValue(name, out var o) ? o : null;

        public static Operator Recruit(Side side) => Find(side == Side.Attackers ? "Recruit ATK" : "Recruit DEF")!;

        public static IEnumerable<Operator> ForSide(Side side) => All.Where(o => o.Side == side);

        /// <summary>Returns a list of problems (unknown weapon names) so bad data fails loudly.</summary>
        public static List<string> Validate()
        {
            var errs = new List<string>();
            foreach (var o in All)
            {
                foreach (var w in o.Primaries.Concat(o.Secondaries))
                    if (WeaponDatabase.Find(w) == null) errs.Add($"{o.Name}: unknown weapon '{w}'");
            }
            return errs;
        }
    }
}
