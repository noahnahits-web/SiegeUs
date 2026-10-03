using System;
using System.Collections.Generic;
using System.Linq;

namespace SiegeUs.Core
{
    public enum WeaponCategory { AssaultRifle, SMG, LMG, DMR, Shotgun, Handgun, MachinePistol, Sniper, Shield, Special }

    public sealed class Weapon
    {
        public string Name { get; init; } = "";
        public WeaponCategory Category { get; init; }
        /// <summary>Damage per bullet (per pellet for shotguns).</summary>
        public int Damage { get; init; }
        public int Rpm { get; init; }
        public int MagSize { get; init; }
        public int Pellets { get; init; } = 1;
        public float ReloadSeconds { get; init; }

        public bool IsAutomatic => Category is WeaponCategory.AssaultRifle or WeaponCategory.SMG
            or WeaponCategory.LMG or WeaponCategory.MachinePistol;
        public float SecondsPerShot => Rpm > 0 ? 60f / Rpm : 0f;
        /// <summary>Spread half-angle in degrees for the top-down shooting model.</summary>
        public float SpreadDegrees => Category switch
        {
            WeaponCategory.Shotgun => 7f,
            WeaponCategory.SMG or WeaponCategory.MachinePistol => 2.5f,
            WeaponCategory.LMG => 2.0f,
            WeaponCategory.Handgun => 1.5f,
            _ => 1.0f,
        };
    }

    /// <summary>
    /// NOTE: Stats are approximations for balance in a top-down game, not exact Siege values.
    /// Tune freely. Every gun name referenced by OperatorDatabase must exist here.
    /// </summary>
    public static class WeaponDatabase
    {
        static Weapon W(string n, WeaponCategory c, int dmg, int rpm, int mag, int pellets, float reload) =>
            new() { Name = n, Category = c, Damage = dmg, Rpm = rpm, MagSize = mag, Pellets = pellets, ReloadSeconds = reload };

        public static readonly IReadOnlyList<Weapon> All = new List<Weapon>
        {
            W("G8A1", WeaponCategory.LMG, 40, 750, 50, 1, 3.5f),
            W("R4-C", WeaponCategory.AssaultRifle, 39, 860, 30, 1, 2.4f),
            W("416-C Carbine", WeaponCategory.AssaultRifle, 40, 740, 30, 1, 2.4f),
            W("AK-12", WeaponCategory.AssaultRifle, 44, 700, 30, 1, 2.5f),
            W("AR33", WeaponCategory.AssaultRifle, 41, 749, 30, 1, 2.5f),
            W("C8-SFW", WeaponCategory.AssaultRifle, 40, 837, 30, 1, 2.4f),
            W("F2", WeaponCategory.AssaultRifle, 40, 980, 30, 1, 2.4f),
            W("Type-89", WeaponCategory.AssaultRifle, 41, 850, 30, 1, 2.5f),
            W("G36C", WeaponCategory.AssaultRifle, 40, 780, 30, 1, 2.4f),
            W("L85A2", WeaponCategory.AssaultRifle, 42, 670, 30, 1, 2.6f),
            W("556XI", WeaponCategory.AssaultRifle, 38, 800, 30, 1, 2.5f),
            W("552 Commando", WeaponCategory.AssaultRifle, 42, 690, 30, 1, 2.5f),
            W("6P41", WeaponCategory.LMG, 44, 650, 50, 1, 3.5f),
            W("MK17 CQB", WeaponCategory.AssaultRifle, 47, 600, 20, 1, 2.5f),
            W("PARA-308", WeaponCategory.AssaultRifle, 47, 620, 20, 1, 2.6f),
            W("SPEAR .308", WeaponCategory.AssaultRifle, 44, 650, 25, 1, 2.5f),
            W("V308", WeaponCategory.AssaultRifle, 47, 650, 25, 1, 2.6f),
            W("ARX200", WeaponCategory.AssaultRifle, 47, 600, 20, 1, 2.5f),
            W("AUG A3", WeaponCategory.AssaultRifle, 41, 720, 30, 1, 2.5f),
            W("417", WeaponCategory.DMR, 52, 400, 20, 1, 2.8f),
            W("SR-25", WeaponCategory.DMR, 54, 360, 20, 1, 2.8f),
            W("Mk 14 EBR", WeaponCategory.DMR, 54, 430, 20, 1, 3.0f),
            W("CAMRS", WeaponCategory.DMR, 52, 380, 20, 1, 2.8f),
            W("OTs-03", WeaponCategory.DMR, 64, 100, 10, 1, 3.6f),
            W("CSRX 300", WeaponCategory.Sniper, 72, 60, 5, 1, 4.0f),
            W("AK-74M", WeaponCategory.AssaultRifle, 43, 650, 30, 1, 2.5f),
            W("AR-15.50", WeaponCategory.DMR, 66, 150, 10, 1, 3.2f),
            W("M4", WeaponCategory.AssaultRifle, 38, 800, 30, 1, 2.4f),
            W("C7E", WeaponCategory.AssaultRifle, 41, 800, 30, 1, 2.5f),
            W("F90", WeaponCategory.AssaultRifle, 40, 780, 30, 1, 2.5f),
            W("ALDA 5.56", WeaponCategory.AssaultRifle, 40, 820, 30, 1, 2.5f),
            W("SC3000K", WeaponCategory.AssaultRifle, 38, 800, 30, 1, 2.5f),
            W("M762", WeaponCategory.AssaultRifle, 48, 640, 30, 1, 2.6f),
            W("POF-9", WeaponCategory.AssaultRifle, 48, 540, 20, 1, 2.6f),
            W("DP27", WeaponCategory.LMG, 45, 550, 47, 1, 4.0f),
            W("LMG-E", WeaponCategory.LMG, 40, 650, 50, 1, 4.0f),
            W("M249", WeaponCategory.LMG, 45, 650, 100, 1, 5.0f),
            W("M249 SAW", WeaponCategory.LMG, 45, 650, 100, 1, 5.0f),
            W("T-95 LSW", WeaponCategory.LMG, 45, 600, 100, 1, 5.0f),
            W("T-5 SMG", WeaponCategory.SMG, 33, 900, 30, 1, 2.2f),
            W("SMG-12", WeaponCategory.MachinePistol, 31, 1000, 32, 1, 2.0f),
            W("MP5K", WeaponCategory.SMG, 33, 800, 30, 1, 2.2f),
            W("MP5", WeaponCategory.SMG, 32, 800, 30, 1, 2.2f),
            W("MP7", WeaponCategory.SMG, 30, 950, 30, 1, 2.2f),
            W("P90", WeaponCategory.SMG, 28, 900, 50, 1, 3.0f),
            W("UMP45", WeaponCategory.SMG, 38, 600, 25, 1, 2.4f),
            W("Vector .45 ACP", WeaponCategory.SMG, 36, 1200, 25, 1, 2.2f),
            W("MPX", WeaponCategory.SMG, 32, 850, 30, 1, 2.2f),
            W("MP5SD", WeaponCategory.SMG, 32, 800, 30, 1, 2.2f),
            W("FMG-9", WeaponCategory.MachinePistol, 31, 800, 30, 1, 2.0f),
            W("Mx4 Storm", WeaponCategory.SMG, 33, 900, 30, 1, 2.2f),
            W("9x19VSN", WeaponCategory.SMG, 34, 750, 30, 1, 2.2f),
            W("K1A", WeaponCategory.SMG, 35, 800, 30, 1, 2.2f),
            W("Scorpion EVO 3 A1", WeaponCategory.SMG, 33, 1050, 40, 1, 2.3f),
            W("9mm C1", WeaponCategory.SMG, 33, 800, 30, 1, 2.2f),
            W("Commando 9", WeaponCategory.AssaultRifle, 31, 780, 30, 1, 2.2f),
            W("P10 RONI", WeaponCategory.MachinePistol, 30, 900, 20, 1, 2.0f),
            W("PDW9", WeaponCategory.SMG, 32, 900, 30, 1, 2.2f),
            W("UZK50GI", WeaponCategory.SMG, 33, 850, 30, 1, 2.2f),
            W("SMG-11", WeaponCategory.MachinePistol, 32, 1270, 16, 1, 2.0f),
            W("SPSMG9", WeaponCategory.MachinePistol, 34, 1000, 30, 1, 2.0f),
            W("SASG-12", WeaponCategory.Shotgun, 42, 200, 8, 1, 3.0f),
            W("M590A1", WeaponCategory.Shotgun, 26, 60, 7, 10, 3.5f),
            W("M870", WeaponCategory.Shotgun, 26, 60, 8, 10, 3.5f),
            W("SG-CQB", WeaponCategory.Shotgun, 23, 60, 8, 10, 3.5f),
            W("M1014", WeaponCategory.Shotgun, 26, 170, 8, 10, 3.0f),
            W("SuperNova", WeaponCategory.Shotgun, 26, 60, 8, 10, 3.5f),
            W("ITA12L", WeaponCategory.Shotgun, 26, 60, 8, 10, 3.5f),
            W("ACS12", WeaponCategory.Shotgun, 20, 300, 6, 10, 3.5f),
            W("BOSG.12.2", WeaponCategory.Shotgun, 34, 60, 2, 10, 3.0f),
            W("TCSG12", WeaponCategory.Shotgun, 60, 180, 6, 1, 3.2f),
            W("FO-12", WeaponCategory.Shotgun, 26, 300, 10, 10, 3.0f),
            W("SIX12", WeaponCategory.Shotgun, 26, 170, 6, 10, 3.0f),
            W("SIX12 SD", WeaponCategory.Shotgun, 26, 170, 6, 10, 3.0f),
            W("Super 90", WeaponCategory.Shotgun, 26, 60, 7, 10, 3.5f),
            W("SPAS-12", WeaponCategory.Shotgun, 26, 60, 7, 10, 3.5f),
            W("SPAS-15", WeaponCategory.Shotgun, 26, 170, 9, 10, 3.0f),
            W("M12", WeaponCategory.SMG, 34, 550, 30, 1, 2.2f),
            W("Super Shorty", WeaponCategory.Shotgun, 26, 60, 5, 10, 3.0f),
            W("Bailiff 410", WeaponCategory.Shotgun, 28, 60, 2, 8, 3.0f),
            W("P226 Mk 25", WeaponCategory.Handgun, 36, 300, 15, 1, 1.8f),
            W("5.7 USG", WeaponCategory.Handgun, 40, 250, 20, 1, 1.8f),
            W("M45 MEUSOC", WeaponCategory.Handgun, 48, 250, 12, 1, 1.8f),
            W("P9", WeaponCategory.Handgun, 36, 300, 15, 1, 1.8f),
            W("LFP586", WeaponCategory.Handgun, 60, 100, 6, 1, 2.5f),
            W("PMM", WeaponCategory.Handgun, 44, 300, 12, 1, 1.8f),
            W("GSh-18", WeaponCategory.Handgun, 40, 300, 18, 1, 1.8f),
            W("P12", WeaponCategory.Handgun, 36, 300, 15, 1, 1.8f),
            W("MK1 9mm", WeaponCategory.Handgun, 38, 300, 15, 1, 1.8f),
            W("D-50", WeaponCategory.Handgun, 70, 100, 7, 1, 2.5f),
            W("Bearing 9", WeaponCategory.MachinePistol, 29, 1100, 26, 1, 2.0f),
            W("PRB92", WeaponCategory.Handgun, 38, 300, 15, 1, 1.8f),
            W("P229", WeaponCategory.Handgun, 40, 300, 12, 1, 1.8f),
            W("Keratos .357", WeaponCategory.Handgun, 60, 100, 6, 1, 2.5f),
            W("Q-929", WeaponCategory.Handgun, 40, 300, 12, 1, 1.8f),
            W("RG15", WeaponCategory.Handgun, 40, 300, 12, 1, 1.8f),
            W("SDP 9mm", WeaponCategory.Handgun, 36, 300, 15, 1, 1.8f),
            W("C75 Auto", WeaponCategory.Handgun, 40, 300, 15, 1, 1.8f),
            W("Gonne-6", WeaponCategory.Handgun, 100, 50, 1, 1, 3.0f),
            W("USP40", WeaponCategory.Handgun, 40, 300, 13, 1, 1.8f),
            W("1911 TACOPS", WeaponCategory.Handgun, 50, 250, 8, 1, 1.8f),
            W("P-10C", WeaponCategory.Handgun, 38, 300, 15, 1, 1.8f),
            W(".44 Vendetta", WeaponCategory.Handgun, 60, 150, 8, 1, 2.2f),
            W("Luison", WeaponCategory.Handgun, 45, 250, 15, 1, 1.8f),
            W("KS79 Lifeline", WeaponCategory.Handgun, 45, 200, 8, 1, 2.0f),
            W("Shield", WeaponCategory.Shield, 0, 0, 0, 1, 0f),
            W("CCE Shield", WeaponCategory.Shield, 0, 0, 0, 1, 0f),
        };

        static readonly Dictionary<string, Weapon> ByName =
            All.ToDictionary(w => w.Name, StringComparer.OrdinalIgnoreCase);

        public static Weapon? Find(string name) => ByName.TryGetValue(name, out var w) ? w : null;
    }
}
