using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    /// <summary>Full dynamic state of one ball. Id 0 is the cue ball.</summary>
    public sealed class BallState
    {
        public const int CueBallId = 0;

        public int Id;
        public Vec2 Position;
        public Vec2 Velocity;
        /// <summary>Angular velocity (rad/s). X/Y = forward/back roll axes, Z = side spin.</summary>
        public double Wx, Wy, Wz;
        public bool OnTable = true;

        public BallState() { }
        public BallState(int id, Vec2 position) { Id = id; Position = position; }

        public bool IsMoving => Velocity.X != 0 || Velocity.Y != 0 || Wx != 0 || Wy != 0 || Wz != 0;

        public BallState Clone() => (BallState)MemberwiseClone();
    }
}
