using System;

namespace PoolGame.Core.Physics
{
    /// <summary>
    /// What a player submits: intent only. Never contains results.
    /// SpinX: -1 left English .. +1 right English. SpinY: -1 draw (back spin) .. +1 follow (top spin).
    /// (SpinX, SpinY) must lie inside the unit disc: it is the cue-tip contact point on the ball face.
    /// </summary>
    public struct ShotParameters
    {
        public double AngleRadians;
        public double Power;   // 0..1
        public double SpinX;
        public double SpinY;

        public ShotParameters(double angleRadians, double power, double spinX = 0, double spinY = 0)
        {
            AngleRadians = angleRadians; Power = power; SpinX = spinX; SpinY = spinY;
        }
    }

    public enum ShotRejection { None, NonFiniteValue, PowerOutOfRange, SpinOutsideDisc, CueBallNotOnTable }

    public static class ShotValidator
    {
        /// <summary>Server-side (and client-side) sanity check of a submitted shot.</summary>
        public static ShotRejection Validate(ShotParameters p, bool cueBallOnTable = true)
        {
            if (!cueBallOnTable) return ShotRejection.CueBallNotOnTable;
            if (!IsFinite(p.AngleRadians) || !IsFinite(p.Power) || !IsFinite(p.SpinX) || !IsFinite(p.SpinY))
                return ShotRejection.NonFiniteValue;
            if (p.Power < 0 || p.Power > 1) return ShotRejection.PowerOutOfRange;
            if (p.SpinX * p.SpinX + p.SpinY * p.SpinY > 1.0 + 1e-9) return ShotRejection.SpinOutsideDisc;
            return ShotRejection.None;
        }

        static bool IsFinite(double v) => !double.IsNaN(v) && !double.IsInfinity(v);
    }
}
