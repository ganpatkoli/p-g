using System;
using System.Collections.Generic;
using System.Linq;
using PoolGame.Core.Geometry;
using PoolGame.Core.Physics;
using Xunit;

namespace PoolGame.Core.Tests
{
    public class PhysicsTests
    {
        static PhysicsConfig Cfg() => new PhysicsConfig();
        static TableConfig Table() => new TableConfig();
        static BallState Ball(int id, double x, double y) => new BallState(id, new Vec2(x, y));
        static PhysicsConfig Frictionless()
        {
            var c = Cfg(); c.SlidingFriction = 0; c.RollingResistance = 0; c.BallFriction = 0; c.CushionFriction = 0; return c;
        }

        [Fact]
        public void RollingBall_StopsAtAnalyticDistance()
        {
            var cfg = Cfg(); var sim = new ShotSimulator(cfg, Table());
            double power = 0.05, v = power * cfg.MaxShotSpeed;
            // SpinY = 0.8 -> b = 0.4R -> rolls without slipping from the start
            var res = sim.Simulate(new[] { Ball(0, -1.0, 0) }, new ShotParameters(0, power, 0, 0.8));
            double expected = v * v / (2 * cfg.RollingResistance * cfg.Gravity);
            double travelled = res.FinalBalls[0].Position.X + 1.0;
            Assert.InRange(travelled, expected * 0.98, expected * 1.02);
        }

        [Fact]
        public void StunShot_SlidesThenRolls_MatchesAnalyticDistance()
        {
            var cfg = Cfg();
            var sim = new ShotSimulator(cfg, Table());
            double v0 = 0.08 * cfg.MaxShotSpeed, mu = cfg.SlidingFriction, g = cfg.Gravity;
            double ts = 2 * v0 / (7 * mu * g);
            double slide = v0 * ts - 0.5 * mu * g * ts * ts;
            double v1 = 5.0 / 7.0 * v0;
            double roll = v1 * v1 / (2 * cfg.RollingResistance * g);
            var res = sim.Simulate(new[] { Ball(0, -1.2, 0) }, new ShotParameters(0, 0.08));
            double travelled = res.FinalBalls[0].Position.X + 1.2;
            Assert.InRange(travelled, (slide + roll) * 0.97, (slide + roll) * 1.03);
        }

        [Fact]
        public void HeadOnCollision_TransfersMomentum()
        {
            var cfg = Frictionless(); cfg.BallRestitution = 1.0;
            var world = new PhysicsWorld(cfg, Table(), new[] { Ball(0, -0.3, 0), Ball(1, 0, 0) });
            world.Get(0).Velocity = new Vec2(2, 0);
            for (int i = 0; i < 200 && !world.Events.Any(); i++) world.Step();
            Assert.Contains(world.Events, e => e.Type == ShotEventType.BallBall);
            Assert.InRange(world.Get(0).Velocity.X, -1e-6, 1e-6);
            Assert.InRange(world.Get(1).Velocity.X, 2 - 1e-6, 2 + 1e-6);
        }

        [Fact]
        public void Cushion_PerpendicularHit_ReturnsRestitutionSpeed()
        {
            var cfg = Frictionless(); var table = Table();
            var world = new PhysicsWorld(cfg, table, new[] { Ball(0, table.HalfLength - 0.2, 0.2) });
            world.Get(0).Velocity = new Vec2(3, 0);
            for (int i = 0; i < 2000 && !world.Events.Any(); i++) world.Step();
            Assert.Contains(world.Events, e => e.Type == ShotEventType.BallCushion);
            Assert.InRange(world.Get(0).Velocity.X, -3 * cfg.CushionRestitution - 1e-6, -3 * cfg.CushionRestitution + 1e-6);
        }

        [Fact]
        public void SideSpin_ChangesCushionRebound()
        {
            var cfg = Cfg(); var table = Table();
            // fire diagonally into the long rail; right English is "running" english for this direction and flattens the rebound
            Func<double, double> reboundAngle = spinX =>
            {
                var world = new PhysicsWorld(cfg, table, new[] { Ball(0, -0.5, 0.2) });
                CueStrike.Apply(world.Get(0), new ShotParameters(0.5, 0.5, spinX, 0), cfg);
                for (int i = 0; i < 20000; i++)
                {
                    world.Step();
                    if (world.Events.Any(e => e.Type == ShotEventType.BallCushion)) break;
                }
                var v = world.Get(0).Velocity;
                return Math.Atan2(v.Y, v.X);
            };
            double plain = reboundAngle(0), right = reboundAngle(0.8), left = reboundAngle(-0.8);
            Assert.True(right - plain > 0.03, $"running English should flatten the rebound ({right:F3} vs {plain:F3})");
            Assert.True(right - left > 0.05, "left and right English must give different rebounds");
        }

        [Fact]
        public void CornerPocket_PotsBallAimedAtIt()
        {
            var cfg = Cfg(); var table = Table(); var sim = new ShotSimulator(cfg, table);
            double hl = table.HalfLength, hw = table.HalfWidth;
            var start = new[] { Ball(0, hl - 0.6, hw - 0.3) };
            double angle = Math.Atan2(hw - (hw - 0.3), hl - (hl - 0.6)); // straight at the corner
            var res = sim.Simulate(start, new ShotParameters(angle, 0.5, 0, 0.8));
            Assert.Contains(res.Events, e => e.Type == ShotEventType.Pocketed && e.BallA == 0);
            Assert.False(res.FinalBalls[0].OnTable);
        }

        [Fact]
        public void SidePocket_PotsBallAimedAtIt()
        {
            var cfg = Cfg(); var table = Table(); var sim = new ShotSimulator(cfg, table);
            var res = sim.Simulate(new[] { Ball(0, 0, 0.1) }, new ShotParameters(Math.PI / 2, 0.5, 0, 0.8));
            Assert.Contains(res.Events, e => e.Type == ShotEventType.Pocketed && (e.PocketIndex == 4 || e.PocketIndex == 5));
        }

        [Fact]
        public void DrawShot_CueBallComesBack_FollowShotGoesThrough()
        {
            var cfg = Cfg(); var table = Table();
            // displacement of the cue ball in the 0.4 s after it hits a straight-on object ball
            Func<double, double> displacement = spinY =>
            {
                var w = new PhysicsWorld(cfg, table, new[] { Ball(0, -0.6, 0), Ball(1, 0, 0) });
                CueStrike.Apply(w.Get(0), new ShotParameters(0, 0.3, 0, spinY), cfg);
                while (!w.Events.Any(e => e.Type == ShotEventType.BallBall)) w.Step();
                double x0 = w.Get(0).Position.X, t0 = w.Time;
                while (w.Time - t0 < 0.4) w.Step();
                return w.Get(0).Position.X - x0;
            };
            double draw = displacement(-1), stun = displacement(0), follow = displacement(1);
            Assert.True(draw < -0.05, $"draw should pull back ({draw:F3})");
            Assert.True(Math.Abs(stun) < 0.12, $"stun should stop near the contact point ({stun:F3})");
            Assert.True(follow > 0.15, $"follow should roll through ({follow:F3})");
        }

        [Fact]
        public void Simulation_IsDeterministic()
        {
            var cfg = Cfg(); var table = Table(); var sim = new ShotSimulator(cfg, table);
            var rack = Rack.Build(RackType.EightBall, cfg, table, 42);
            var shot = new ShotParameters(0.01, 1.0, 0.3, -0.2);
            var a = sim.Simulate(rack, shot); var b = sim.Simulate(rack, shot);
            Assert.Equal(a.Events.Count, b.Events.Count);
            for (int i = 0; i < a.FinalBalls.Count; i++)
            {
                Assert.Equal(a.FinalBalls[i].Position.X, b.FinalBalls[i].Position.X);
                Assert.Equal(a.FinalBalls[i].Position.Y, b.FinalBalls[i].Position.Y);
            }
        }

        [Fact]
        public void Break_StaysValid_NoNaN_BallsInsideTable_AndEnds()
        {
            var cfg = Cfg(); var table = Table(); var sim = new ShotSimulator(cfg, table);
            for (ulong seed = 1; seed <= 20; seed++)
            {
                var rack = Rack.Build(RackType.EightBall, cfg, table, seed);
                var res = sim.Simulate(rack, new ShotParameters(0.0 + seed * 0.001, 1.0, 0, 0));
                Assert.False(res.TimedOut);
                foreach (var b in res.FinalBalls.Where(b => b.OnTable))
                {
                    Assert.False(double.IsNaN(b.Position.X) || double.IsNaN(b.Position.Y));
                    Assert.InRange(b.Position.X, -table.HalfLength, table.HalfLength);
                    Assert.InRange(b.Position.Y, -table.HalfWidth, table.HalfWidth);
                }
                Assert.Contains(res.Events, e => e.Type == ShotEventType.BallBall);
            }
        }

        [Fact]
        public void Break_BallsNeverOverlapAtRest()
        {
            var cfg = Cfg(); var table = Table(); var sim = new ShotSimulator(cfg, table);
            var res = sim.Simulate(Rack.Build(RackType.EightBall, cfg, table, 7), new ShotParameters(0.0, 1.0));
            var on = res.FinalBalls.Where(b => b.OnTable).ToList();
            for (int i = 0; i < on.Count; i++)
                for (int j = i + 1; j < on.Count; j++)
                    Assert.True(Vec2.Distance(on[i].Position, on[j].Position) > 2 * cfg.BallRadius * 0.98);
        }

        [Fact]
        public void Recorder_CapturesFramesAtInterval()
        {
            var cfg = Cfg(); var sim = new ShotSimulator(cfg, Table());
            var rec = new TrajectoryRecorder(1.0 / 60);
            var res = sim.Simulate(new[] { Ball(0, -1, 0) }, new ShotParameters(0, 0.2, 0, 0.8), rec);
            Assert.True(rec.Frames.Count > 10);
            Assert.Equal(rec.Times.Count, rec.Frames.Count);
            Assert.Equal(res.FinalBalls[0].Position.X, rec.Frames.Last()[0].Position.X);
        }
    }

    public class ShotValidationTests
    {
        [Theory]
        [InlineData(double.NaN, 0.5, 0, 0, ShotRejection.NonFiniteValue)]
        [InlineData(0.0, 1.2, 0, 0, ShotRejection.PowerOutOfRange)]
        [InlineData(0.0, -0.1, 0, 0, ShotRejection.PowerOutOfRange)]
        [InlineData(0.0, 0.5, 0.9, 0.9, ShotRejection.SpinOutsideDisc)]
        [InlineData(0.0, 0.5, 0.5, 0.5, ShotRejection.None)]
        public void Validate(double angle, double power, double sx, double sy, ShotRejection expected)
        {
            Assert.Equal(expected, ShotValidator.Validate(new ShotParameters(angle, power, sx, sy)));
        }

        [Fact]
        public void Simulator_RejectsInvalidShot_WithoutSimulating()
        {
            var sim = new ShotSimulator(new PhysicsConfig(), new TableConfig());
            var res = sim.Simulate(new[] { new BallState(0, Vec2.Zero) }, new ShotParameters(0, 5));
            Assert.Equal(ShotRejection.PowerOutOfRange, res.Rejection);
            Assert.Empty(res.Events);
        }

        [Fact]
        public void Config_RejectsTunnellingTimestep()
        {
            var c = new PhysicsConfig { FixedTimestep = 0.01 };
            Assert.Throws<ArgumentException>(() => c.Validate());
        }
    }

    public class AimAndPlacementTests
    {
        [Fact]
        public void AimPredictor_FindsGhostBallAndObjectDirection()
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            var balls = new[] { new BallState(0, new Vec2(-0.5, 0)), new BallState(1, new Vec2(0.2, 0.01)) };
            var p = AimPredictor.Predict(balls, 0, cfg, table);
            Assert.True(p.HitsBall);
            Assert.Equal(1, p.ObjectBallId);
            Assert.InRange(Vec2.Distance(p.GhostBallCenter, balls[1].Position), 2 * cfg.BallRadius - 1e-9, 2 * cfg.BallRadius + 1e-9);
            Assert.True(p.ObjectDirection.X > 0.9);
        }

        [Fact]
        public void AimPredictor_MissesToCushion()
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            var p = AimPredictor.Predict(new[] { new BallState(0, new Vec2(0, 0)) }, 0, cfg, table);
            Assert.False(p.HitsBall);
            Assert.InRange(p.GhostBallCenter.X, table.HalfLength - cfg.BallRadius - 1e-9, table.HalfLength - cfg.BallRadius + 1e-9);
        }

        [Fact]
        public void Placement_RejectsOverlapOutOfBoundsAndBehindHeadStringViolation()
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            var balls = new[] { new BallState(1, new Vec2(0, 0)) };
            Assert.False(BallPlacement.IsLegal(new Vec2(0.01, 0), balls, cfg, table, false));
            Assert.False(BallPlacement.IsLegal(new Vec2(5, 0), balls, cfg, table, false));
            Assert.False(BallPlacement.IsLegal(new Vec2(0.5, 0.3), balls, cfg, table, true)); // beyond head string
            Assert.True(BallPlacement.IsLegal(new Vec2(-0.8, 0.1), balls, cfg, table, true));
        }

        [Theory]
        [InlineData(RackType.EightBall, 15)]
        [InlineData(RackType.NineBall, 9)]
        public void Rack_HasAllBalls_NoOverlap_AndSpecialBallsInPlace(RackType type, int objectBalls)
        {
            var cfg = new PhysicsConfig(); var table = new TableConfig();
            for (ulong seed = 1; seed < 30; seed++)
            {
                var r = Rack.Build(type, cfg, table, seed);
                Assert.Equal(objectBalls + 1, r.Count);
                Assert.Equal(r.Count, r.Select(b => b.Id).Distinct().Count());
                for (int i = 0; i < r.Count; i++)
                    for (int j = i + 1; j < r.Count; j++)
                        Assert.True(Vec2.Distance(r[i].Position, r[j].Position) >= 2 * cfg.BallRadius - 1e-9);
                if (type == RackType.EightBall)
                {
                    var centre = r.First(b => b.Id == 8);
                    Assert.InRange(centre.Position.X, table.FootSpot.X + 2 * 0.0495 - 0.01, table.FootSpot.X + 2 * 0.0495 + 0.01);
                    var corners = r.Where(b => b.Id != 0).OrderByDescending(b => b.Position.X).ThenBy(b => b.Position.Y).ToList();
                    var back = r.Where(b => b.Id != 0 && b.Position.X > table.FootSpot.X + 0.19).OrderBy(b => b.Position.Y).ToList();
                    Assert.Equal(5, back.Count);
                    Assert.NotEqual(back.First().Id <= 7, back.Last().Id <= 7);
                }
                else
                {
                    Assert.Equal(table.FootSpot.X, r.First(b => b.Id == 1).Position.X, 6);
                    Assert.InRange(r.First(b => b.Id == 9).Position.Y, -1e-9, 1e-9);
                }
            }
        }
    }
}
