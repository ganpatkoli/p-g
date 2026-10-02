using NUnit.Framework;
using PoolGame.Client.Core;
using PoolGame.Client.Physics;
using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Tests
{
    public class ClientFoundationTests
    {
        struct Ping { public int Value; }

        [Test]
        public void EventBus_DeliversToSubscribers_AndStopsAfterUnsubscribe()
        {
            var bus = new EventBus(); int sum = 0;
            void H(Ping p) => sum += p.Value;
            bus.Subscribe<Ping>(H);
            bus.Publish(new Ping { Value = 2 });
            bus.Unsubscribe<Ping>(H);
            bus.Publish(new Ping { Value = 5 });
            Assert.AreEqual(2, sum);
        }

        [Test]
        public void PhysicsConfigAsset_ProducesValidatedCopy()
        {
            var asset = ScriptableObject.CreateInstance<PhysicsConfigAsset>();
            var cfg = asset.ToConfig();
            Assert.AreNotSame(asset.Config, cfg);
            Assert.AreEqual(asset.Config.BallRadius, cfg.BallRadius);
            asset.Config.FixedTimestep = 1.0;
            Assert.Throws<System.ArgumentException>(() => asset.ToConfig());
        }

        [Test]
        public void LocalShotResolver_ReturnsReplayableShot()
        {
            var resolver = new LocalShotResolver(new PhysicsConfig(), new TableConfig());
            var balls = new[] { new BallState(0, new PoolGame.Core.Geometry.Vec2(-0.5, 0)) };
            var r = resolver.Resolve(balls, new ShotParameters(0, 0.2, 0, 0.8));
            Assert.AreEqual(ShotRejection.None, r.Result.Rejection);
            Assert.Greater(r.Trajectory.Frames.Count, 2);
        }
    }
}
