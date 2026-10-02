using System;
using System.Collections.Generic;

namespace PoolGame.Core.Physics
{
    public sealed class ShotResult
    {
        public List<BallState> FinalBalls = new List<BallState>();
        public List<ShotEvent> Events = new List<ShotEvent>();
        public double Duration;
        public bool TimedOut;
        public ShotRejection Rejection;
    }

    /// <summary>Receives sampled frames while a shot runs, so a client can replay the server result.</summary>
    public interface IShotRecorder
    {
        double SampleInterval { get; }
        void Record(double time, IReadOnlyList<BallState> balls);
    }

    /// <summary>Records full-ball snapshots at a fixed interval (e.g. 1/60 s).</summary>
    public sealed class TrajectoryRecorder : IShotRecorder
    {
        public double SampleInterval { get; }
        public readonly List<double> Times = new List<double>();
        public readonly List<BallState[]> Frames = new List<BallState[]>();

        public TrajectoryRecorder(double sampleInterval = 1.0 / 60.0) { SampleInterval = sampleInterval; }

        public void Record(double time, IReadOnlyList<BallState> balls)
        {
            var copy = new BallState[balls.Count];
            for (int i = 0; i < copy.Length; i++) copy[i] = balls[i].Clone();
            Times.Add(time);
            Frames.Add(copy);
        }
    }

    /// <summary>Runs one complete shot from a resting layout until every ball stops.</summary>
    public sealed class ShotSimulator
    {
        readonly PhysicsConfig _cfg;
        readonly TableConfig _table;

        public ShotSimulator(PhysicsConfig cfg, TableConfig table)
        {
            cfg.Validate();
            _cfg = cfg; _table = table;
        }

        public ShotResult Simulate(IEnumerable<BallState> start, ShotParameters shot, IShotRecorder recorder = null)
        {
            var world = new PhysicsWorld(_cfg, _table, start);
            var cue = world.Get(BallState.CueBallId);
            var rejection = ShotValidator.Validate(shot, cue != null && cue.OnTable);
            if (rejection != ShotRejection.None)
                return new ShotResult { Rejection = rejection, FinalBalls = world.Balls };

            CueStrike.Apply(cue, shot, _cfg);
            recorder?.Record(0, world.Balls);

            double nextSample = recorder?.SampleInterval ?? double.MaxValue;
            bool timedOut = false;
            while (!world.IsAtRest)
            {
                if (world.Time >= _cfg.MaxSimSeconds) { timedOut = true; break; }
                world.Step();
                if (recorder != null && world.Time >= nextSample)
                {
                    recorder.Record(world.Time, world.Balls);
                    nextSample += recorder.SampleInterval;
                }
            }
            recorder?.Record(world.Time, world.Balls);

            return new ShotResult { FinalBalls = world.Balls, Events = world.Events, Duration = world.Time, TimedOut = timedOut };
        }
    }
}
