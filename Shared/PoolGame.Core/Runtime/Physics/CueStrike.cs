using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    /// <summary>Turns a cue strike into the cue ball's initial linear and angular velocity.</summary>
    public static class CueStrike
    {
        /// <summary>
        /// Impulse at height b above centre gives w = 2.5 * v * b / R^2 (solid sphere, I = 0.4 m R^2).
        /// b = 0.4R is exactly "rolls immediately"; b = 0 is a stun shot (pure slide).
        /// </summary>
        public static void Apply(BallState cue, ShotParameters p, PhysicsConfig cfg)
        {
            double r = cfg.BallRadius;
            double angle = p.AngleRadians + cfg.SquirtCoefficient * p.SpinX; // right English squirts left
            Vec2 d = Vec2.FromAngle(angle);
            double v = p.Power * cfg.MaxShotSpeed;
            double a = p.SpinX * cfg.MaxStrikeOffset * r; // side offset (+ = right of aim)
            double b = p.SpinY * cfg.MaxStrikeOffset * r; // height offset (+ = above centre)
            double k = 2.5 * v / (r * r);

            cue.Velocity = d * v;
            // Horizontal axis perpendicular to travel; positive b gives forward roll.
            cue.Wx = -d.Y * k * b;
            cue.Wy = d.X * k * b;
            cue.Wz = k * a;
        }
    }
}
