using System.Collections.Generic;
using System.Linq;
using PoolGame.Core.Physics;

namespace PoolGame.Core.Rules
{
    public enum GameType { EightBall, NineBall }
    public enum BallGroup { None, Solids, Stripes }
    public enum FoulType { None, CueBallPocketed, NoBallHit, WrongBallFirst, NoRailAfterContact, IllegalBreak }
    public enum WinReason { None, LegalEightBall, EightBallEarly, EightBallScratch, LegalNineBall, Forfeit }

    /// <summary>Authoritative rules state between shots. Immutable by convention: Resolve returns a new copy.</summary>
    public sealed class RulesState
    {
        public GameType Game;
        public int CurrentPlayer;
        public BallGroup[] PlayerGroups = { BallGroup.None, BallGroup.None };
        public bool IsBreak = true;
        public bool BallInHand = true;       // cue ball in hand behind the head string for the break
        public bool IsOver;
        public int Winner = -1;
        public WinReason WinReason;
        public List<int> BallsOnTable = new List<int>();
        public int ConsecutiveFouls;

        public RulesState Clone()
        {
            var c = (RulesState)MemberwiseClone();
            c.PlayerGroups = (BallGroup[])PlayerGroups.Clone();
            c.BallsOnTable = new List<int>(BallsOnTable);
            return c;
        }
    }

    /// <summary>What the rules need to know about a shot, distilled from physics events.</summary>
    public sealed class ShotSummary
    {
        public int FirstContactBallId = -1;
        public List<int> PocketedObjectIds = new List<int>();     // in pocketing order, cue ball excluded
        public bool CueBallPocketed;
        /// <summary>Rail contacts by cue or object balls after the first ball-ball contact.</summary>
        public int RailContactsAfterFirstContact;
        /// <summary>Distinct object balls that touched a cushion at any time (break rule).</summary>
        public int ObjectBallsTouchingRail;

        public static ShotSummary From(IEnumerable<ShotEvent> events)
        {
            var s = new ShotSummary();
            var railBalls = new HashSet<int>();
            double firstContactTime = double.MaxValue;
            var list = events.OrderBy(e => e.Time).ToList();
            foreach (var e in list)
            {
                switch (e.Type)
                {
                    case ShotEventType.BallBall:
                        if (s.FirstContactBallId < 0 && (e.BallA == BallState.CueBallId || e.BallB == BallState.CueBallId))
                        {
                            s.FirstContactBallId = e.BallA == BallState.CueBallId ? e.BallB : e.BallA;
                            firstContactTime = e.Time;
                        }
                        break;
                    case ShotEventType.BallCushion:
                        if (e.BallA != BallState.CueBallId) railBalls.Add(e.BallA);
                        if (e.Time >= firstContactTime) s.RailContactsAfterFirstContact++;
                        break;
                    case ShotEventType.Pocketed:
                        if (e.BallA == BallState.CueBallId) s.CueBallPocketed = true;
                        else s.PocketedObjectIds.Add(e.BallA);
                        break;
                }
            }
            s.ObjectBallsTouchingRail = railBalls.Count;
            return s;
        }
    }

    public sealed class ShotOutcome
    {
        public RulesState NewState;
        public List<FoulType> Fouls = new List<FoulType>();
        public List<int> RespotBallIds = new List<int>();
        public bool IsFoul => Fouls.Count > 0;
        public bool TurnContinues;
        public bool GroupsAssigned;
    }

    public interface IRuleSet
    {
        GameType Game { get; }
        RulesState CreateInitialState(int breakingPlayer);
        ShotOutcome Resolve(RulesState state, ShotSummary shot);
        /// <summary>Ball ids the current player may legally contact first.</summary>
        IReadOnlyList<int> LegalFirstContacts(RulesState state);
    }

    public static class RuleSets
    {
        public static IRuleSet Create(GameType game)
        {
            switch (game)
            {
                case GameType.EightBall: return new EightBallRules();
                case GameType.NineBall: return new NineBallRules();
                default: throw new System.ArgumentOutOfRangeException(nameof(game));
            }
        }
    }
}
