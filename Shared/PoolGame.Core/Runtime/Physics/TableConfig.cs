using System.Collections.Generic;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    public struct Pocket
    {
        public Vec2 Center;
        public double Radius;
        public bool IsSide;
    }

    /// <summary>
    /// Table geometry. Origin is the table centre; X runs along the length. Playing surface is the
    /// rectangle bounded by the cushion faces. Head spot is at -Length/4, foot spot at +Length/4.
    /// </summary>
    public sealed class TableConfig
    {
        public double Length = 2.54;
        public double Width = 1.27;
        public double CornerMouth = 0.06;        // along-rail distance from the corner where the cushion starts
        public double SideMouthHalf = 0.05;      // half opening of each side pocket
        public double CornerPocketRadius = 0.07;
        public double SidePocketRadius = 0.06;

        public double HalfLength => Length / 2;
        public double HalfWidth => Width / 2;
        public Vec2 HeadSpot => new Vec2(-Length / 4, 0);
        public Vec2 FootSpot => new Vec2(Length / 4, 0);

        public TableConfig Clone() => (TableConfig)MemberwiseClone();

        public IReadOnlyList<Pocket> Pockets()
        {
            double hl = HalfLength, hw = HalfWidth;
            return new[]
            {
                new Pocket { Center = new Vec2(-hl, -hw), Radius = CornerPocketRadius },
                new Pocket { Center = new Vec2(hl, -hw), Radius = CornerPocketRadius },
                new Pocket { Center = new Vec2(-hl, hw), Radius = CornerPocketRadius },
                new Pocket { Center = new Vec2(hl, hw), Radius = CornerPocketRadius },
                new Pocket { Center = new Vec2(0, -hw), Radius = SidePocketRadius, IsSide = true },
                new Pocket { Center = new Vec2(0, hw), Radius = SidePocketRadius, IsSide = true },
            };
        }

        /// <summary>Static jaw posts at the end of each cushion segment (pocket rim).</summary>
        public IReadOnlyList<Vec2> JawPosts()
        {
            double hl = HalfLength, hw = HalfWidth, cm = CornerMouth, sh = SideMouthHalf;
            var p = new List<Vec2>(12);
            foreach (int sx in new[] { -1, 1 })
                foreach (int sy in new[] { -1, 1 })
                {
                    p.Add(new Vec2(sx * (hl - cm), sy * hw)); // on the long rail
                    p.Add(new Vec2(sx * hl, sy * (hw - cm))); // on the short rail
                    p.Add(new Vec2(sx * sh, sy * hw));        // side pocket
                }
            return p;
        }
    }
}
