using System;
using System.Collections.Generic;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    public struct AimPrediction
    {
        public bool HitsBall;
        public int ObjectBallId;         // -1 if none
        public Vec2 GhostBallCenter;     // cue ball centre at first contact (or at the cushion)
        public Vec2 ObjectDirection;     // object ball departure direction (line of centres)
        public Vec2 CueDirectionAfter;   // cue ball tangent direction for a stun shot
        public double Distance;          // travel distance to first contact
    }

    /// <summary>
    /// Cheap geometric aim guide (ghost ball). Ignores spin and throw; the real result always comes
    /// from <see cref="ShotSimulator"/>. Used by the cue aiming UI and as AI candidate generation.
    /// </summary>
    public static class AimPredictor
    {
        public static AimPrediction Predict(IReadOnlyList<BallState> balls, double angle, PhysicsConfig cfg, TableConfig table)
        {
            BallState cue = null;
            foreach (var b in balls) if (b.Id == BallState.CueBallId && b.OnTable) cue = b;
            var result = new AimPrediction { ObjectBallId = -1 };
            if (cue == null) return result;

            Vec2 d = Vec2.FromAngle(angle);
            double diameter = 2 * cfg.BallRadius;
            double best = double.MaxValue;
            BallState hit = null;

            foreach (var b in balls)
            {
                if (!b.OnTable || b.Id == BallState.CueBallId) continue;
                Vec2 f = cue.Position - b.Position;
                double bq = Vec2.Dot(f, d);
                double cq = f.SqrLength - diameter * diameter;
                double disc = bq * bq - cq;
                if (disc < 0) continue;
                double t = -bq - Math.Sqrt(disc);
                if (t > 0 && t < best) { best = t; hit = b; }
            }

            double wall = DistanceToCushion(cue.Position, d, cfg, table);
            if (hit != null && best <= wall)
            {
                result.HitsBall = true;
                result.ObjectBallId = hit.Id;
                result.Distance = best;
                result.GhostBallCenter = cue.Position + d * best;
                result.ObjectDirection = (hit.Position - result.GhostBallCenter).Normalized();
                Vec2 n = result.ObjectDirection;
                Vec2 tangent = d - n * Vec2.Dot(d, n);
                result.CueDirectionAfter = tangent.Normalized();
            }
            else
            {
                result.Distance = wall;
                result.GhostBallCenter = cue.Position + d * wall;
            }
            return result;
        }

        static double DistanceToCushion(Vec2 p, Vec2 d, PhysicsConfig cfg, TableConfig table)
        {
            double xMax = table.HalfLength - cfg.BallRadius, yMax = table.HalfWidth - cfg.BallRadius;
            double t = double.MaxValue;
            if (d.X > 1e-12) t = Math.Min(t, (xMax - p.X) / d.X);
            if (d.X < -1e-12) t = Math.Min(t, (-xMax - p.X) / d.X);
            if (d.Y > 1e-12) t = Math.Min(t, (yMax - p.Y) / d.Y);
            if (d.Y < -1e-12) t = Math.Min(t, (-yMax - p.Y) / d.Y);
            return t;
        }
    }
}
