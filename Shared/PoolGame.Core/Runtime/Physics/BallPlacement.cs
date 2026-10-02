using System;
using System.Collections.Generic;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    /// <summary>Validates where a cue ball may be placed (ball-in-hand, break).</summary>
    public static class BallPlacement
    {
        public static bool IsLegal(Vec2 pos, IReadOnlyList<BallState> balls, PhysicsConfig cfg, TableConfig table, bool behindHeadString)
        {
            double r = cfg.BallRadius;
            if (double.IsNaN(pos.X) || double.IsNaN(pos.Y)) return false;
            if (Math.Abs(pos.X) > table.HalfLength - r || Math.Abs(pos.Y) > table.HalfWidth - r) return false;
            if (behindHeadString && pos.X > table.HeadSpot.X) return false;
            foreach (var p in table.Pockets())
                if (Vec2.Distance(pos, p.Center) < p.Radius + r) return false;
            foreach (var b in balls)
                if (b.OnTable && b.Id != BallState.CueBallId && Vec2.Distance(pos, b.Position) < 2 * r + 1e-4) return false;
            return true;
        }
    
        /// <summary>
        /// Re-spot position: the requested spot, or the nearest free spot further along the long string (toward the foot rail).
        /// </summary>
        public static Vec2 FindFreeSpot(Vec2 spot, IReadOnlyList<BallState> balls, PhysicsConfig cfg, TableConfig table)
        {
            double step = 2 * cfg.BallRadius * 1.01;
            Vec2 p = spot;
            while (p.X <= table.HalfLength - cfg.BallRadius)
            {
                if (IsLegal(p, balls, cfg, table, false)) return p;
                p = new Vec2(p.X + step, p.Y);
            }
            return spot; // table completely blocked along the string: caller keeps the original spot
        }
    }
}
