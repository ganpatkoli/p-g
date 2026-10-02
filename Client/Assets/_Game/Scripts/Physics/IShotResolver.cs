using System.Collections.Generic;
using PoolGame.Core.Physics;

namespace PoolGame.Client.Physics
{
    /// <summary>A resolved shot plus the frames needed to replay it.</summary>
    public sealed class ResolvedShot
    {
        public ShotResult Result;
        public TrajectoryRecorder Trajectory;
    }

    /// <summary>
    /// Turns a submitted shot into an authoritative result. Locally this runs the shared core (practice, offline).
    /// In multiplayer (Phase 10) a remote implementation returns the server's result; the client only replays it.
    /// </summary>
    public interface IShotResolver
    {
        ResolvedShot Resolve(IReadOnlyList<BallState> balls, ShotParameters shot);
    }

    public sealed class LocalShotResolver : IShotResolver
    {
        readonly ShotSimulator _sim;
        public LocalShotResolver(PhysicsConfig cfg, TableConfig table) { _sim = new ShotSimulator(cfg, table); }

        public ResolvedShot Resolve(IReadOnlyList<BallState> balls, ShotParameters shot)
        {
            var rec = new TrajectoryRecorder(1.0 / 60.0);
            var result = _sim.Simulate(balls, shot, rec);
            return new ResolvedShot { Result = result, Trajectory = rec };
        }
    }
}
