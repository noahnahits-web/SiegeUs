using System;
using System.Text.Json;
using HarmonyLib;
using Hazel;

namespace SiegeUs.Game
{
    /// <summary>
    /// One envelope for everything. Clients send "intent" messages; the host validates them
    /// against MatchState and broadcasts a "Snapshot". Hits on walls are broadcast directly.
    /// Small on purpose: the Snapshot payload (A) is a base64 binary blob, everything else is tiny.
    /// </summary>
    public sealed class SiegeMsg
    {
        public string Type { get; set; } = "";   // JoinTeam, Start, Lobby, VoteSpawn, VoteSite, Pick, Hit, WallHit, HatchBreak, Plant, Defuse, Floor, Reinforce, ReinforceOK, Snapshot
        public byte Player { get; set; }          // informational; receivers trust the RPC sender instead
        public string A { get; set; } = "";
        public string B { get; set; } = "";
        public string C { get; set; } = "";
        public int N { get; set; }
    }

    /// <summary>
    /// Networking without any extra dependency: a custom RPC id on the sender's PlayerControl,
    /// received through a Harmony prefix on PlayerControl.HandleRpc. Every client needs the mod.
    /// </summary>
    public static class SiegeNet
    {
        public const byte RpcId = 214;   // vanilla uses 0..~65; stay far away from it

        public static void Send(SiegeMsg msg)
        {
            var me = PlayerControl.LocalPlayer;
            if (me == null || AmongUsClient.Instance == null) return;
            msg.Player = me.PlayerId;
            try
            {
                var w = AmongUsClient.Instance.StartRpcImmediately(me.NetId, RpcId, SendOption.Reliable, -1);
                w.Write(JsonSerializer.Serialize(msg));
                AmongUsClient.Instance.FinishRpcImmediately(w);
            }
            catch (Exception e) { SiegePlugin.Logger.LogError("Send failed: " + e.Message); }

            // RPCs are not echoed back to the sender, so apply it locally too.
            SiegeGame.Instance?.OnMessage(me, msg);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.HandleRpc))]
    public static class HandleRpcPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] byte callId, [HarmonyArgument(1)] MessageReader reader)
        {
            if (callId != SiegeNet.RpcId) return true;       // not ours: let the game handle it
            try
            {
                var msg = JsonSerializer.Deserialize<SiegeMsg>(reader.ReadString());
                if (msg != null) SiegeGame.Instance?.OnMessage(__instance, msg);
            }
            catch (Exception e) { SiegePlugin.Logger.LogError("Bad Siege RPC: " + e.Message); }
            return false;
        }
    }
}
