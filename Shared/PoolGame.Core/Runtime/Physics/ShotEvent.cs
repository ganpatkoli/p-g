namespace PoolGame.Core.Physics
{
    public enum ShotEventType { BallBall, BallCushion, BallJaw, Pocketed }

    /// <summary>Something that happened during a simulated shot. Used by rules, audio, analytics and audit.</summary>
    public struct ShotEvent
    {
        public ShotEventType Type;
        public double Time;
        public int BallA;        // always set
        public int BallB;        // BallBall: other ball, otherwise -1
        public int PocketIndex;  // Pocketed only, otherwise -1
        public double Speed;     // impact speed along the contact normal (0 for Pocketed)
    }
}
