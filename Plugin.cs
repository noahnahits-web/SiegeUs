using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using SiegeUs.Core;
using SiegeUs.Game;
using UnityEngine;

namespace SiegeUs
{
    [BepInPlugin(Id, "Siege Us", Version)]
    [BepInProcess("Among Us.exe")]
    public class SiegePlugin : BasePlugin
    {
        public const string Id = "gg.siegeus.mod";
        public const string Version = "0.1.0";

        public Harmony Harmony { get; } = new(Id);
        public static ManualLogSource Logger = null!;

        public override void Load()
        {
            Logger = Log;

            // 1. Fail loudly on bad data instead of in the middle of a match.
            var errors = OperatorDatabase.Validate().Concat(MapData.LoadEmbedded("oregon").Validate()).ToList();
            foreach (var e in errors) Log.LogError("Data error: " + e);
            Log.LogInfo($"Siege Us: {OperatorDatabase.All.Count} operators, {WeaponDatabase.All.Count} weapons, {errors.Count} data errors.");

            // 2. Plain-C# game object (all logic lives here, so no IL2CPP field/method injection issues).
            new SiegeGame();

            // 3. Register the two tiny Unity components, then create the persistent driver object.
            ClassInjector.RegisterTypeInIl2Cpp<SiegeDriver>();
            ClassInjector.RegisterTypeInIl2Cpp<WallMarker>();
            var go = new GameObject("SiegeUs");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<SiegeDriver>();

            // 4. Harmony patches (custom RPC receive hook lives in SiegeNet.cs).
            Harmony.PatchAll();
        }
    }

    /// <summary>The only persistent Unity component: forwards Update/OnGUI to the plain-C# SiegeGame.</summary>
    public class SiegeDriver : MonoBehaviour
    {
        public SiegeDriver(IntPtr ptr) : base(ptr) { }

        float _lastErr;

        void Update()
        {
            try { SiegeGame.Instance?.Tick(); }
            catch (Exception e) { LogThrottled("Update", e); }
        }

        void OnGUI()
        {
            try
            {
                var g = SiegeGame.Instance;
                if (g != null) SiegeUI.Draw(g);
            }
            catch (Exception e) { LogThrottled("OnGUI", e); }
        }

        void LogThrottled(string where, Exception e)
        {
            if (Time.realtimeSinceStartup - _lastErr < 5f) return;   // don't spam the log every frame
            _lastErr = Time.realtimeSinceStartup;
            SiegePlugin.Logger.LogError($"{where}: {e}");
        }
    }

    /// <summary>Tags a wall GameObject with its index so raycast hits can be mapped back to wall state.</summary>
    public class WallMarker : MonoBehaviour
    {
        public WallMarker(IntPtr ptr) : base(ptr) { }
        public int Index = -1;
    }
}
