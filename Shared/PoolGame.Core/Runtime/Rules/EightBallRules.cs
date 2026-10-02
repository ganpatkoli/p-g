using System.Collections.Generic;
using System.Linq;
using PoolGame.Core.Physics;

namespace PoolGame.Core.Rules
{
    /// <summary>
    /// 8-Ball (simplified WPA). Open table until a legal pot; foul = ball in hand for the opponent.
    /// Design choices: 8 pocketed on the break is re-spotted; no re-rack option; no three-foul rule.
    /// </summary>
    public sealed class EightBallRules : IRuleSet
    {
        public GameType Game => GameType.EightBall;

        public RulesState CreateInitialState(int breakingPlayer)
        {
            return new RulesState
            {
                Game = GameType.EightBall,
                CurrentPlayer = breakingPlayer,
                BallsOnTable = Enumerable.Range(1, 15).ToList(),
            };
        }

        public static bool IsSolid(int id) => id >= 1 && id <= 7;
        public static bool IsStripe(int id) => id >= 9 && id <= 15;
        static BallGroup GroupOf(int id) => IsSolid(id) ? BallGroup.Solids : IsStripe(id) ? BallGroup.Stripes : BallGroup.None;

        static bool HasClearedGroup(RulesState s, BallGroup g) =>
            g != BallGroup.None && !s.BallsOnTable.Any(id => GroupOf(id) == g);

        public IReadOnlyList<int> LegalFirstContacts(RulesState state)
        {
            var g = state.PlayerGroups[state.CurrentPlayer];
            if (state.IsBreak) return state.BallsOnTable.Where(id => id != 8).ToList();
            if (g == BallGroup.None) return state.BallsOnTable.Where(id => id != 8).ToList();
            if (HasClearedGroup(state, g)) return new List<int> { 8 };
            return state.BallsOnTable.Where(id => GroupOf(id) == g).ToList();
        }

        public ShotOutcome Resolve(RulesState state, ShotSummary shot)
        {
            var o = new ShotOutcome { NewState = state.Clone() };
            var ns = o.NewState;
            int me = state.CurrentPlayer, opp = 1 - me;

            if (state.IsOver) return o;

            bool eightPocketed = shot.PocketedObjectIds.Contains(8);
            var pocketedNon8 = shot.PocketedObjectIds.Where(id => id != 8).ToList();
            bool onEight = !state.IsBreak && HasClearedGroup(state, state.PlayerGroups[me]);

            if (shot.CueBallPocketed) o.Fouls.Add(FoulType.CueBallPocketed);

            if (state.IsBreak)
            {
                bool legalBreak = pocketedNon8.Count > 0 || shot.ObjectBallsTouchingRail >= 4;
                if (!legalBreak) o.Fouls.Add(FoulType.IllegalBreak);
                if (shot.FirstContactBallId < 0) o.Fouls.Add(FoulType.NoBallHit);
            }
            else
            {
                if (shot.FirstContactBallId < 0) o.Fouls.Add(FoulType.NoBallHit);
                else if (!LegalFirstContacts(state).Contains(shot.FirstContactBallId)) o.Fouls.Add(FoulType.WrongBallFirst);
                if (shot.FirstContactBallId >= 0 && shot.PocketedObjectIds.Count == 0 && shot.RailContactsAfterFirstContact == 0)
                    o.Fouls.Add(FoulType.NoRailAfterContact);
            }

            // remove pocketed balls from the table
            foreach (var id in shot.PocketedObjectIds) ns.BallsOnTable.Remove(id);

            if (eightPocketed)
            {
                if (state.IsBreak)
                {
                    ns.BallsOnTable.Add(8);               // re-spot, game continues
                    o.RespotBallIds.Add(8);
                }
                else
                {
                    bool legal = onEight && !o.IsFoul;
                    ns.IsOver = true;
                    ns.Winner = legal ? me : opp;
                    ns.WinReason = legal ? WinReason.LegalEightBall
                                 : shot.CueBallPocketed ? WinReason.EightBallScratch : WinReason.EightBallEarly;
                    ns.BallInHand = false;
                    ns.IsBreak = false;
                    return o;
                }
            }

            // group assignment on an open table (never on the break, never on a foul)
            if (!state.IsBreak && state.PlayerGroups[me] == BallGroup.None && !o.IsFoul && pocketedNon8.Count > 0)
            {
                bool solids = pocketedNon8.Any(IsSolid), stripes = pocketedNon8.Any(IsStripe);
                if (solids != stripes)
                {
                    var mine = solids ? BallGroup.Solids : BallGroup.Stripes;
                    ns.PlayerGroups[me] = mine;
                    ns.PlayerGroups[opp] = mine == BallGroup.Solids ? BallGroup.Stripes : BallGroup.Solids;
                    o.GroupsAssigned = true;
                }
            }

            bool scored;
            var myGroup = ns.PlayerGroups[me];
            if (myGroup == BallGroup.None) scored = pocketedNon8.Count > 0;
            else scored = pocketedNon8.Any(id => GroupOf(id) == myGroup);

            ns.IsBreak = false;
            if (o.IsFoul)
            {
                ns.CurrentPlayer = opp; ns.BallInHand = true; ns.ConsecutiveFouls = state.ConsecutiveFouls + 1;
                o.TurnContinues = false;
            }
            else
            {
                ns.ConsecutiveFouls = 0; ns.BallInHand = false;
                o.TurnContinues = scored;
                ns.CurrentPlayer = scored ? me : opp;
            }
            return o;
        }
    }
}
