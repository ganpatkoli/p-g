using System.Collections.Generic;
using System.Linq;
using PoolGame.Core.Geometry;
using PoolGame.Core.Physics;
using PoolGame.Core.Rules;
using Xunit;

namespace PoolGame.Core.Tests
{
    public class EightBallRulesTests
    {
        static readonly IRuleSet Rules = new EightBallRules();

        static RulesState AfterBreak()
        {
            var s = Rules.CreateInitialState(0);
            var o = Rules.Resolve(s, new ShotSummary { FirstContactBallId = 1, ObjectBallsTouchingRail = 4 });
            return o.NewState;
        }

        static RulesState Assigned(BallGroup p0 = BallGroup.Solids)
        {
            var s = AfterBreak(); // player 1 to shoot, open table
            s.CurrentPlayer = 0;
            s.PlayerGroups[0] = p0;
            s.PlayerGroups[1] = p0 == BallGroup.Solids ? BallGroup.Stripes : BallGroup.Solids;
            return s;
        }

        static ShotSummary Shot(int first, int rails = 1, params int[] potted) =>
            new ShotSummary { FirstContactBallId = first, RailContactsAfterFirstContact = rails, PocketedObjectIds = potted.ToList() };

        [Fact]
        public void Initial_BreakerHasBallInHand_AllBallsOnTable()
        {
            var s = Rules.CreateInitialState(1);
            Assert.Equal(1, s.CurrentPlayer); Assert.True(s.IsBreak); Assert.True(s.BallInHand);
            Assert.Equal(15, s.BallsOnTable.Count);
        }

        [Fact]
        public void LegalBreak_WithFourRails_PassesTurnWithoutFoul()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, ObjectBallsTouchingRail = 4 });
            Assert.False(o.IsFoul); Assert.Equal(1, o.NewState.CurrentPlayer); Assert.False(o.NewState.IsBreak);
        }

        [Fact]
        public void IllegalBreak_IsFoul_BallInHandForOpponent()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, ObjectBallsTouchingRail = 2 });
            Assert.Contains(FoulType.IllegalBreak, o.Fouls);
            Assert.Equal(1, o.NewState.CurrentPlayer); Assert.True(o.NewState.BallInHand);
        }

        [Fact]
        public void Break_PottingBall_KeepsTurn_TableStaysOpen()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, PocketedObjectIds = { 3 } });
            Assert.False(o.IsFoul); Assert.True(o.TurnContinues); Assert.Equal(0, o.NewState.CurrentPlayer);
            Assert.All(o.NewState.PlayerGroups, g => Assert.Equal(BallGroup.None, g));
            Assert.DoesNotContain(3, o.NewState.BallsOnTable);
        }

        [Fact]
        public void Break_EightBallPotted_IsRespotted_NotAWin()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, PocketedObjectIds = { 8 } });
            Assert.False(o.NewState.IsOver); Assert.Contains(8, o.NewState.BallsOnTable); Assert.Contains(8, o.RespotBallIds);
        }

        [Fact]
        public void OpenTable_PottingSolid_AssignsGroups()
        {
            var o = Rules.Resolve(AfterBreak().Also(s => s.CurrentPlayer = 0), Shot(2, 1, 2));
            Assert.True(o.GroupsAssigned);
            Assert.Equal(BallGroup.Solids, o.NewState.PlayerGroups[0]);
            Assert.Equal(BallGroup.Stripes, o.NewState.PlayerGroups[1]);
            Assert.True(o.TurnContinues);
        }

        [Fact]
        public void OpenTable_PottingBothTypes_StaysOpen_AndContinues()
        {
            var o = Rules.Resolve(AfterBreak().Also(s => s.CurrentPlayer = 0), Shot(2, 1, 2, 10));
            Assert.False(o.GroupsAssigned); Assert.True(o.TurnContinues);
            Assert.Equal(BallGroup.None, o.NewState.PlayerGroups[0]);
        }

        [Fact]
        public void OpenTable_HittingEightFirst_IsFoul()
        {
            var o = Rules.Resolve(AfterBreak().Also(s => s.CurrentPlayer = 0), Shot(8));
            Assert.Contains(FoulType.WrongBallFirst, o.Fouls);
        }

        [Fact]
        public void PottingOwnBall_KeepsTurn()
        {
            var o = Rules.Resolve(Assigned(), Shot(3, 0, 3));
            Assert.False(o.IsFoul); Assert.True(o.TurnContinues); Assert.Equal(0, o.NewState.CurrentPlayer);
        }

        [Fact]
        public void Miss_PassesTurn_NoFoulIfRailTouched()
        {
            var o = Rules.Resolve(Assigned(), Shot(3, 1));
            Assert.False(o.IsFoul); Assert.Equal(1, o.NewState.CurrentPlayer); Assert.False(o.NewState.BallInHand);
        }

        [Fact]
        public void PottingOnlyOpponentBall_PassesTurn()
        {
            var o = Rules.Resolve(Assigned(), Shot(3, 1, 10));
            Assert.False(o.IsFoul); Assert.False(o.TurnContinues); Assert.Equal(1, o.NewState.CurrentPlayer);
        }

        [Fact]
        public void WrongBallFirst_IsFoul_AndGivesBallInHand()
        {
            var o = Rules.Resolve(Assigned(), Shot(10, 1));
            Assert.Contains(FoulType.WrongBallFirst, o.Fouls);
            Assert.Equal(1, o.NewState.CurrentPlayer); Assert.True(o.NewState.BallInHand);
        }

        [Fact]
        public void NoContact_IsFoul()
        {
            var o = Rules.Resolve(Assigned(), new ShotSummary());
            Assert.Contains(FoulType.NoBallHit, o.Fouls);
        }

        [Fact]
        public void NoRailAfterContact_AndNoPot_IsFoul()
        {
            var o = Rules.Resolve(Assigned(), Shot(3, 0));
            Assert.Contains(FoulType.NoRailAfterContact, o.Fouls);
        }

        [Fact]
        public void Scratch_IsFoul_EvenIfBallPotted_AndNoGroupAssigned()
        {
            var s = AfterBreak().Also(x => x.CurrentPlayer = 0);
            var o = Rules.Resolve(s, new ShotSummary { FirstContactBallId = 2, PocketedObjectIds = { 2 }, CueBallPocketed = true });
            Assert.Contains(FoulType.CueBallPocketed, o.Fouls);
            Assert.False(o.GroupsAssigned); Assert.True(o.NewState.BallInHand); Assert.Equal(1, o.NewState.CurrentPlayer);
        }

        [Fact]
        public void EightBall_EarlyPot_LosesGame()
        {
            var o = Rules.Resolve(Assigned(), Shot(3, 1, 3, 8));
            Assert.True(o.NewState.IsOver); Assert.Equal(1, o.NewState.Winner); Assert.Equal(WinReason.EightBallEarly, o.NewState.WinReason);
        }

        static RulesState OnEight()
        {
            var s = Assigned(); s.BallsOnTable = new List<int> { 8, 9, 10 }; return s;
        }

        [Fact]
        public void LegalEightBall_AfterClearingGroup_Wins()
        {
            var o = Rules.Resolve(OnEight(), Shot(8, 0, 8));
            Assert.True(o.NewState.IsOver); Assert.Equal(0, o.NewState.Winner); Assert.Equal(WinReason.LegalEightBall, o.NewState.WinReason);
        }

        [Fact]
        public void EightBall_PottedWithScratch_Loses()
        {
            var o = Rules.Resolve(OnEight(), new ShotSummary { FirstContactBallId = 8, PocketedObjectIds = { 8 }, CueBallPocketed = true });
            Assert.Equal(1, o.NewState.Winner); Assert.Equal(WinReason.EightBallScratch, o.NewState.WinReason);
        }

        [Fact]
        public void OnEight_MustHitEightFirst()
        {
            var o = Rules.Resolve(OnEight(), Shot(9, 1));
            Assert.Contains(FoulType.WrongBallFirst, o.Fouls);
            Assert.Equal(new[] { 8 }, EightBallRulesLegal(OnEight()));
        }

        static int[] EightBallRulesLegal(RulesState s) => Rules.LegalFirstContacts(s).ToArray();

        [Fact]
        public void LegalFirstContacts_ForGroupPlayer_AreOwnBalls()
        {
            var legal = Rules.LegalFirstContacts(Assigned());
            Assert.All(legal, id => Assert.InRange(id, 1, 7));
        }

        [Fact]
        public void ConsecutiveFouls_AreCounted_AndReset()
        {
            var o1 = Rules.Resolve(Assigned(), Shot(10, 1));
            Assert.Equal(1, o1.NewState.ConsecutiveFouls);
            var o2 = Rules.Resolve(o1.NewState, Shot(10, 1));  // player 1 is now Stripes
            Assert.False(o2.IsFoul); Assert.Equal(0, o2.NewState.ConsecutiveFouls);
        }

        [Fact]
        public void Resolve_DoesNotMutateInputState()
        {
            var s = Assigned();
            Rules.Resolve(s, Shot(3, 0, 3));
            Assert.Contains(3, s.BallsOnTable);
        }

        [Fact]
        public void GameOver_StateIsFrozen()
        {
            var s = Assigned(); s.IsOver = true; s.Winner = 0;
            var o = Rules.Resolve(s, Shot(3, 0, 3));
            Assert.Contains(3, o.NewState.BallsOnTable); Assert.Equal(0, o.NewState.Winner);
        }
    }

    public class NineBallRulesTests
    {
        static readonly IRuleSet Rules = new NineBallRules();
        static RulesState InPlay() { var s = Rules.CreateInitialState(0); s.IsBreak = false; s.BallInHand = false; return s; }
        static ShotSummary Shot(int first, int rails = 1, params int[] potted) =>
            new ShotSummary { FirstContactBallId = first, RailContactsAfterFirstContact = rails, PocketedObjectIds = potted.ToList() };

        [Fact] public void LowestBall_IsOnlyLegalContact() => Assert.Equal(new[] { 1 }, Rules.LegalFirstContacts(InPlay()).ToArray());

        [Fact]
        public void HittingWrongBallFirst_IsFoul()
        {
            var o = Rules.Resolve(InPlay(), Shot(2, 1));
            Assert.Contains(FoulType.WrongBallFirst, o.Fouls); Assert.True(o.NewState.BallInHand); Assert.Equal(1, o.NewState.CurrentPlayer);
        }

        [Fact]
        public void PottingBall_KeepsTurn()
        {
            var o = Rules.Resolve(InPlay(), Shot(1, 0, 1));
            Assert.True(o.TurnContinues); Assert.Equal(0, o.NewState.CurrentPlayer);
            Assert.Equal(new[] { 2 }, Rules.LegalFirstContacts(o.NewState).ToArray());
        }

        [Fact]
        public void NineBall_Legal_Wins_EvenOutOfOrder()
        {
            var o = Rules.Resolve(InPlay(), Shot(1, 0, 9));
            Assert.True(o.NewState.IsOver); Assert.Equal(0, o.NewState.Winner); Assert.Equal(WinReason.LegalNineBall, o.NewState.WinReason);
        }

        [Fact]
        public void NineBall_OnFoul_IsRespotted_AndNoWin()
        {
            var o = Rules.Resolve(InPlay(), Shot(2, 1, 9));
            Assert.False(o.NewState.IsOver); Assert.Contains(9, o.NewState.BallsOnTable); Assert.Contains(9, o.RespotBallIds);
            Assert.Equal(1, o.NewState.CurrentPlayer);
        }

        [Fact]
        public void Scratch_IsFoul()
        {
            var o = Rules.Resolve(InPlay(), new ShotSummary { FirstContactBallId = 1, CueBallPocketed = true, RailContactsAfterFirstContact = 1 });
            Assert.Contains(FoulType.CueBallPocketed, o.Fouls);
        }

        [Fact]
        public void NineOnBreak_Wins()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, PocketedObjectIds = { 9 }, ObjectBallsTouchingRail = 4 });
            Assert.Equal(0, o.NewState.Winner);
        }

        [Fact]
        public void IllegalBreak_IsFoul()
        {
            var o = Rules.Resolve(Rules.CreateInitialState(0), new ShotSummary { FirstContactBallId = 1, ObjectBallsTouchingRail = 1 });
            Assert.Contains(FoulType.IllegalBreak, o.Fouls);
        }
    }

    public class ShotSummaryIntegrationTests
    {
        [Fact]
        public void Summary_ReadsPhysicsEvents_FromSimulatedShot()
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            var sim = new ShotSimulator(cfg, table);
            var rack = Rack.Build(RackType.EightBall, cfg, table, 3);
            var res = sim.Simulate(rack, new ShotParameters(0, 1.0));
            var sum = ShotSummary.From(res.Events);
            Assert.True(sum.FirstContactBallId > 0);
            Assert.True(sum.ObjectBallsTouchingRail >= 1);
        }

        [Fact]
        public void EndToEnd_PhysicsResultFeedsRules_DeterministicOutcome()
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            var sim = new ShotSimulator(cfg, table); var rules = RuleSets.Create(GameType.EightBall);
            var state = rules.CreateInitialState(0);
            var rack = Rack.Build(RackType.EightBall, cfg, table, 11);
            var shot = new ShotParameters(0.0, 1.0);
            var o1 = rules.Resolve(state, ShotSummary.From(sim.Simulate(rack, shot).Events));
            var o2 = rules.Resolve(state, ShotSummary.From(sim.Simulate(rack, shot).Events));
            Assert.Equal(o1.NewState.CurrentPlayer, o2.NewState.CurrentPlayer);
            Assert.Equal(o1.Fouls, o2.Fouls);
            Assert.Equal(o1.NewState.BallsOnTable, o2.NewState.BallsOnTable);
        }

        [Fact]
        public void Summary_CueBallScratch_IsDetected()
        {
            var events = new List<ShotEvent> { new ShotEvent { Type = ShotEventType.Pocketed, BallA = 0, BallB = -1, PocketIndex = 1 } };
            Assert.True(ShotSummary.From(events).CueBallPocketed);
        }
    }

    static class Ext { public static T Also<T>(this T x, System.Action<T> a) { a(x); return x; } }
}
