using System;
using System.Collections.Generic;

namespace SiegeUs.Core
{
    public enum Side { Attackers, Defenders }
    public enum TeamId { Blue, Orange }

    public enum Phase
    {
        Lobby,
        SpawnPick,      // attackers vote spawn, defenders vote bomb site
        OperatorSelect, // everyone picks an operator
        Prep,           // defenders reinforce, attackers fly drones
        Action,         // live round
        RoundEnd,
        MatchEnd
    }

    public enum RoundEndReason
    {
        None,
        AttackersEliminated,
        DefendersEliminated,
        BombDefused,
        BombDetonated,
        TimeExpired
    }

    public enum BombState { Unplanted, Planted, Defused, Detonated }

    public sealed class MatchConfig
    {
        public string MapId = "oregon";
        public int RoundsToWin = 8;                 // first team to 8 round wins takes the match
        public bool SwapSidesEachRound = true;      // R6 ranked style
        public bool OneOperatorPerTeam = true;      // no duplicate operator picks on a team
        public float SpawnPickSeconds = 20f;
        public float OperatorSelectSeconds = 30f;
        public float PrepSeconds = 45f;
        public float ActionSeconds = 180f;
        public float RoundEndSeconds = 8f;
        public float BombFuseSeconds = 45f;
    }

    public sealed class PlayerSlot
    {
        public byte PlayerId;
        public TeamId Team;
        public string? Operator;
        public string? Primary;
        public string? Secondary;
        public bool Alive;
        public int Hp;
        public int MaxHp;
        public string Floor = "1F";
    }

    public sealed class RoundResult
    {
        public int RoundNumber;
        public TeamId Winner;
        public RoundEndReason Reason;
    }
}
