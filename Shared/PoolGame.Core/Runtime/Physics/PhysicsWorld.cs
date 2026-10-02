using System;
using System.Collections.Generic;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    /// <summary>
    /// Deterministic fixed-step simulation of the balls. Pure C#, no engine or network dependency.
    /// Order of every loop is fixed (ball id ascending) so identical input gives identical output on any machine.
    /// </summary>
    public sealed class PhysicsWorld
    {
        readonly PhysicsConfig _cfg;
        readonly TableConfig _table;
        readonly IReadOnlyList<Pocket> _pockets;
        readonly IReadOnlyList<Vec2> _jaws;

        public List<BallState> Balls { get; }
        public List<ShotEvent> Events { get; } = new List<ShotEvent>();
        public double Time { get; private set; }

        public PhysicsWorld(PhysicsConfig cfg, TableConfig table, IEnumerable<BallState> balls)
        {
            _cfg = cfg; _table = table;
            _pockets = table.Pockets();
            _jaws = table.JawPosts();
            Balls = new List<BallState>();
            foreach (var b in balls) Balls.Add(b.Clone());
            Balls.Sort((a, b) => a.Id.CompareTo(b.Id));
        }

        public bool IsAtRest
        {
            get { foreach (var b in Balls) if (b.OnTable && b.IsMoving) return false; return true; }
        }

        public BallState Get(int id) { foreach (var b in Balls) if (b.Id == id) return b; return null; }

        public void Step() => Step(_cfg.FixedTimestep);

        public void Step(double dt)
        {
            foreach (var b in Balls)
            {
                if (!b.OnTable) continue;
                ApplyCloth(b, dt);
                b.Position += b.Velocity * dt;
            }
            for (int i = 0; i < Balls.Count; i++)
            {
                var a = Balls[i];
                if (!a.OnTable) continue;
                for (int j = i + 1; j < Balls.Count; j++)
                {
                    var c = Balls[j];
                    if (c.OnTable) CollideBalls(a, c);
                }
            }
            foreach (var b in Balls)
            {
                if (!b.OnTable) continue;
                CollideJaws(b);
                CollideCushions(b);
                CheckPocket(b);
            }
            Time += dt;
        }

        // ---- cloth: sliding -> rolling, rolling resistance, side spin decay ----
        void ApplyCloth(BallState b, double dt)
        {
            double r = _cfg.BallRadius, g = _cfg.Gravity;
            double remaining = dt;

            double ux = b.Velocity.X - r * b.Wy;
            double uy = b.Velocity.Y + r * b.Wx;
            double um = Math.Sqrt(ux * ux + uy * uy);

            if (um > _cfg.SlideEpsilon)
            {
                double mu = _cfg.SlidingFriction;
                double slipDecel = 3.5 * mu * g;                 // rate at which |u| shrinks
                double ts = um / slipDecel;                      // time to stop slipping
                double t = Math.Min(ts, remaining);
                double ax = ux / um, ay = uy / um;
                b.Velocity = new Vec2(b.Velocity.X - mu * g * ax * t, b.Velocity.Y - mu * g * ay * t);
                b.Wx -= 2.5 * mu * g * ay * t / r;
                b.Wy += 2.5 * mu * g * ax * t / r;
                remaining -= t;
                if (ts > dt) { DecaySideSpin(b, dt); return; }   // still sliding for the whole step
            }

            if (remaining > 0)
            {
                double speed = b.Velocity.Length;
                double ns = speed - _cfg.RollingResistance * g * remaining;
                if (ns < _cfg.StopSpeed)
                {
                    b.Velocity = Vec2.Zero; b.Wx = 0; b.Wy = 0; b.Wz = 0;
                    return;
                }
                if (speed > 0) b.Velocity = b.Velocity * (ns / speed);
                b.Wx = -b.Velocity.Y / r;      // rolling constraint
                b.Wy = b.Velocity.X / r;
            }
            DecaySideSpin(b, dt);
        }

        void DecaySideSpin(BallState b, double dt)
        {
            if (b.Wz == 0) return;
            // Side spin decays by cloth torque; here proportional to sliding friction, constant deceleration.
            double dec = 2.5 * _cfg.SlidingFriction * _cfg.Gravity / _cfg.BallRadius * 0.05 * dt;
            if (Math.Abs(b.Wz) <= dec) b.Wz = 0; else b.Wz -= Math.Sign(b.Wz) * dec;
        }

        // ---- ball vs ball ----
        void CollideBalls(BallState a, BallState c)
        {
            double r = _cfg.BallRadius, m = _cfg.BallMass, i = _cfg.MomentOfInertia;
            Vec2 d = c.Position - a.Position;
            double dist = d.Length;
            if (dist >= 2 * r || dist < 1e-12) return;

            Vec2 n = d / dist;
            double overlap = 2 * r - dist;
            a.Position -= n * (overlap / 2);
            c.Position += n * (overlap / 2);

            Vec2 rel = a.Velocity - c.Velocity;
            double vn = Vec2.Dot(rel, n);
            if (vn <= 0) return; // separating

            Vec2 t = new Vec2(-n.Y, n.X);
            double ut = Vec2.Dot(rel, t) + r * (a.Wz + c.Wz); // contact-point slip along tangent

            double jn = (1 + _cfg.BallRestitution) * m * vn / 2; // impulse magnitude, equal masses
            a.Velocity -= n * (jn / m);
            c.Velocity += n * (jn / m);

            double jtFull = Math.Abs(ut) * m / 7.0;
            double jt = Math.Min(_cfg.BallFriction * jn, jtFull) * Math.Sign(ut);
            a.Velocity -= t * (jt / m);
            c.Velocity += t * (jt / m);
            a.Wz -= r * jt / i;
            c.Wz -= r * jt / i;

            Events.Add(new ShotEvent { Type = ShotEventType.BallBall, Time = Time, BallA = a.Id, BallB = c.Id, PocketIndex = -1, Speed = vn });
        }

        // ---- pocket jaws ----
        void CollideJaws(BallState b)
        {
            double minDist = _cfg.BallRadius + _cfg.JawRadius;
            for (int k = 0; k < _jaws.Count; k++)
            {
                Vec2 d = b.Position - _jaws[k];
                double dist = d.Length;
                if (dist >= minDist || dist < 1e-12) continue;
                Vec2 n = d / dist;
                b.Position = _jaws[k] + n * minDist;
                double vn = Vec2.Dot(b.Velocity, n);
                if (vn >= 0) continue;
                b.Velocity -= n * ((1 + _cfg.JawRestitution) * vn);
                Events.Add(new ShotEvent { Type = ShotEventType.BallJaw, Time = Time, BallA = b.Id, BallB = -1, PocketIndex = -1, Speed = -vn });
            }
        }

        // ---- cushions (only where a cushion segment exists; pocket mouths are open) ----
        void CollideCushions(BallState b)
        {
            double r = _cfg.BallRadius;
            double xMin = -_table.HalfLength + r, xMax = _table.HalfLength - r;
            double yMin = -_table.HalfWidth + r, yMax = _table.HalfWidth - r;
            double cm = _table.CornerMouth, sh = _table.SideMouthHalf;
            double hl = _table.HalfLength, hw = _table.HalfWidth;
            double ax = Math.Abs(b.Position.X), ay = Math.Abs(b.Position.Y);

            // short rails (x walls): segment spans |y| < hw - cm
            if (ay < hw - cm)
            {
                if (b.Position.X < xMin) Bounce(b, new Vec2(1, 0), xMin, true);
                else if (b.Position.X > xMax) Bounce(b, new Vec2(-1, 0), xMax, true);
            }
            // long rails (y walls): segments span sh < |x| < hl - cm
            if (ax > sh && ax < hl - cm)
            {
                if (b.Position.Y < yMin) Bounce(b, new Vec2(0, 1), yMin, false);
                else if (b.Position.Y > yMax) Bounce(b, new Vec2(0, -1), yMax, false);
            }
        }

        void Bounce(BallState b, Vec2 n, double planeCoord, bool xAxis)
        {
            double r = _cfg.BallRadius, m = _cfg.BallMass, inertia = _cfg.MomentOfInertia;
            if (xAxis) b.Position = new Vec2(planeCoord, b.Position.Y); else b.Position = new Vec2(b.Position.X, planeCoord);

            double vn = Vec2.Dot(b.Velocity, n);   // negative when moving into the wall
            if (vn >= 0) return;
            Vec2 t = new Vec2(-n.Y, n.X);
            double vt = Vec2.Dot(b.Velocity, t);
            double ut = vt - r * b.Wz;             // contact-point slip along the cushion

            double jn = (1 + _cfg.CushionRestitution) * m * (-vn);
            double jtFull = Math.Abs(ut) * m / 3.5;
            double jt = Math.Min(_cfg.CushionFriction * jn, jtFull) * Math.Sign(ut);

            b.Velocity = b.Velocity + n * (jn / m) - t * (jt / m);
            b.Wz += r * jt / inertia;

            Events.Add(new ShotEvent { Type = ShotEventType.BallCushion, Time = Time, BallA = b.Id, BallB = -1, PocketIndex = -1, Speed = -vn });
        }

        // ---- pockets ----
        void CheckPocket(BallState b)
        {
            double hl = _table.HalfLength, hw = _table.HalfWidth;
            bool beyondFace = Math.Abs(b.Position.X) > hl || Math.Abs(b.Position.Y) > hw;
            int best = -1; double bestD = double.MaxValue;
            for (int k = 0; k < _pockets.Count; k++)
            {
                double d = Vec2.Distance(b.Position, _pockets[k].Center);
                if (d < _pockets[k].Radius && d < bestD) { best = k; bestD = d; }
            }
            if (best < 0 && beyondFace)
            {
                for (int k = 0; k < _pockets.Count; k++)
                {
                    double d = Vec2.Distance(b.Position, _pockets[k].Center);
                    if (d < bestD) { best = k; bestD = d; }
                }
            }
            if (best < 0) return;

            b.OnTable = false;
            b.Velocity = Vec2.Zero; b.Wx = b.Wy = b.Wz = 0;
            Events.Add(new ShotEvent { Type = ShotEventType.Pocketed, Time = Time, BallA = b.Id, BallB = -1, PocketIndex = best, Speed = 0 });
        }
    }
}
