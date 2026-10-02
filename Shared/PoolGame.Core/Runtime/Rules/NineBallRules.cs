using System.Collections.Generic;
using System.Linq;
using PoolGame.Core.Physics;

namespace PoolGame.Core.Rules
{
    /// <summary>
    /// 9-Ball (simplified WPA, rotation game). Lowest ball on the table must be hit first.
    /// Design choices: 9 on a foul is re-spotted; no push-out; no three-foul rule.
    /// </summary>
    public sealed class NineBallRules : IRuleSet
    {
        public GameType Game => GameType.NineBall;

        public RulesState CreateInitialState(int breakingPlayer)
        {
            return new RulesState
            {
                Game = GameType.NineBall,
                CurrentPlayer = breakingPlayer,
                BallsOnTable = Enumerable.Range(1, 9).ToList(),
            };
        }

        public IReadOnlyList<int> LegalFirstContacts(RulesState state)
        {
            if (state.BallsOnTable.Count == 0) return new List<int>();
            return new List<int> { state.BallsOnTable.Min() };
        }

        public ShotOutcome Resolve(RulesState state, ShotSummary shot)
        {
            var o = new ShotOutcome { NewState = state.Clone() };
            var ns = o.NewState;
            int me = state.CurrentPlayer, opp = 1 - me;
            if (state.IsOver) return o;

            int lowest = state.BallsOnTable.Min();

            if (shot.CueBallPocketed) o.Fouls.Add(FoulType.CueBallPocketed);
            if (shot.FirstContactBallId < 0) o.Fouls.Add(FoulType.NoBallHit);
            else if (shot.FirstContactBallId != lowest) o.Fouls.Add(FoulType.WrongBallFirst);

            if (state.IsBreak)
            {
                bool legalBreak = shot.PocketedObjectIds.Count > 0 || shot.ObjectBallsTouchingRail >= 4;
                if (!legalBreak) o.Fouls.Add(FoulType.IllegalBreak);
            }
            else if (shot.FirstContactBallId >= 0 && shot.PocketedObjectIds.Count == 0 && shot.RailContactsAfterFirstContact == 0)
            {
                o.Fouls.Add(FoulType.NoRailAfterContact);
            }

            bool nine = shot.PocketedObjectIds.Contains(9);
            foreach (var id in shot.PocketedObjectIds) ns.BallsOnTable.Remove(id);
            ns.IsBreak = false;

            if (nine)
            {
                if (!o.IsFoul)
                {
                    ns.IsOver = true; ns.Winner = me; ns.WinReason = WinReason.LegalNineBall; ns.BallInHand = false;
                    return o;
                }
                ns.BallsOnTable.Add(9);       // fouled 9 is re-spotted
                o.RespotBallIds.Add(9);
            }

            if (o.IsFoul)
            {
                ns.CurrentPlayer = opp; ns.BallInHand = true; ns.ConsecutiveFouls = state.ConsecutiveFouls + 1;
            }
            else
            {
                ns.ConsecutiveFouls = 0; ns.BallInHand = false;
                o.TurnContinues = shot.PocketedObjectIds.Count > 0;
                ns.CurrentPlayer = o.TurnContinues ? me : opp;
            }
            return o;
        }
    }
}
