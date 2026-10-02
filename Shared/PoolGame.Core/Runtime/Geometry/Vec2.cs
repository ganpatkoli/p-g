using System;

namespace PoolGame.Core.Geometry
{
    /// <summary>Plain double-precision 2D vector. Doubles keep client and server results identical.</summary>
    public struct Vec2 : IEquatable<Vec2>
    {
        public double X;
        public double Y;

        public Vec2(double x, double y) { X = x; Y = y; }

        public static readonly Vec2 Zero = new Vec2(0, 0);

        public double Length => Math.Sqrt(X * X + Y * Y);
        public double SqrLength => X * X + Y * Y;

        public Vec2 Normalized()
        {
            double l = Length;
            return l > 1e-12 ? new Vec2(X / l, Y / l) : Zero;
        }

        public static Vec2 FromAngle(double radians) => new Vec2(Math.Cos(radians), Math.Sin(radians));
        public static double Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static double Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, double s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator *(double s, Vec2 a) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, double s) => new Vec2(a.X / s, a.Y / s);

        public bool Equals(Vec2 o) => X == o.X && Y == o.Y;
        public override bool Equals(object obj) => obj is Vec2 v && Equals(v);
        public override int GetHashCode() => (X.GetHashCode() * 397) ^ Y.GetHashCode();
        public override string ToString() => $"({X:0.####}, {Y:0.####})";
    }
}
