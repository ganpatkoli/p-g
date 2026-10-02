using System;
using System.Collections.Generic;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    public enum RackType { EightBall, NineBall }

    /// <summary>Builds the opening layout. Uses its own tiny PRNG so results match on every platform.</summary>
    public static class Rack
    {
        public static List<BallState> Build(RackType type, PhysicsConfig cfg, TableConfig table, ulong seed)
        {
            var rng = new XorShift(seed);
            var balls = new List<BallState> { new BallState(BallState.CueBallId, table.HeadSpot) };
            double r = cfg.BallRadius;
            double spacing = 2 * r * 1.0005;
            double dx = spacing * Math.Sqrt(3) / 2;
            Vec2 apex = table.FootSpot;

            if (type == RackType.EightBall)
            {
                var ids = new List<int> { 1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15 };
                Shuffle(ids, rng);
                ids.Insert(4, 8); // centre of third row

                // back corners must be one solid and one stripe
                if (IsSolid(ids[10]) == IsSolid(ids[14]))
                {
                    for (int k = 5; k < 10; k++)
                        if (ids[k] != 8 && IsSolid(ids[k]) != IsSolid(ids[10])) { Swap(ids, 14, k); break; }
                }
                int n = 0;
                for (int col = 0; col < 5; col++)
                    for (int row = 0; row <= col; row++)
                        balls.Add(new BallState(ids[n++], new Vec2(apex.X + col * dx, apex.Y + (row - col / 2.0) * spacing)));
            }
            else
            {
                // diamond: 1 at the apex, 9 in the centre, others random
                var ids = new List<int> { 2, 3, 4, 5, 6, 7, 8 };
                Shuffle(ids, rng);
                int[] rows = { 1, 2, 3, 2, 1 };
                int next = 0;
                for (int col = 0; col < rows.Length; col++)
                    for (int row = 0; row < rows[col]; row++)
                    {
                        int id = (col == 0) ? 1 : (col == 2 && row == 1) ? 9 : ids[next++];
                        balls.Add(new BallState(id, new Vec2(apex.X + col * dx, apex.Y + (row - (rows[col] - 1) / 2.0) * spacing)));
                    }
            }
            return balls;
        }

        static bool IsSolid(int id) => id >= 1 && id <= 7;
        static void Swap(List<int> l, int a, int b) { int t = l[a]; l[a] = l[b]; l[b] = t; }

        static void Shuffle(List<int> l, XorShift rng)
        {
            for (int i = l.Count - 1; i > 0; i--) Swap(l, i, (int)(rng.Next() % (ulong)(i + 1)));
        }

        sealed class XorShift
        {
            ulong _s;
            public XorShift(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }
            public ulong Next() { _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17; return _s; }
        }
    }
}
