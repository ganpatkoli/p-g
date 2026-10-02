using System;

namespace PoolGame.Core.Physics
{
    /// <summary>
    /// All tunable physics values (SI units: metres, kilograms, seconds, radians).
    /// Defaults approximate a regulation 9ft table with 57.15mm balls. Nothing here is hardcoded elsewhere.
    /// </summary>
    [Serializable]
    public sealed class PhysicsConfig
    {
        // Ball
        public double BallRadius = 0.028575;
        public double BallMass = 0.17;
        public double Gravity = 9.81;

        // Cloth
        public double SlidingFriction = 0.2;      // ball-cloth, while slipping
        public double RollingResistance = 0.01;   // ball-cloth, while rolling (deceleration = value * g)

        // Ball-ball
        public double BallRestitution = 0.95;
        public double BallFriction = 0.05;        // transfers side spin (throw)

        // Cushion
        public double CushionRestitution = 0.75;
        public double CushionFriction = 0.2;      // turns side spin into rebound angle change

        // Pocket jaws (rim)
        public double JawRadius = 0.008;
        public double JawRestitution = 0.5;

        // Cue strike
        public double MaxShotSpeed = 9.0;         // m/s at Power = 1
        public double MaxStrikeOffset = 0.5;      // fraction of ball radius reachable by the cue tip before a miscue
        public double SquirtCoefficient = 0.02;   // radians of deflection per unit of side offset

        // Integration
        public double FixedTimestep = 1.0 / 480.0;
        public double MaxSimSeconds = 60.0;
        public double StopSpeed = 0.004;          // m/s: below this a rolling ball is at rest
        public double SlideEpsilon = 1e-3;        // m/s: contact-point slip below this counts as rolling

        public double MomentOfInertia => 0.4 * BallMass * BallRadius * BallRadius;

        public PhysicsConfig Clone() => (PhysicsConfig)MemberwiseClone();

        public void Validate()
        {
            Positive(BallRadius, nameof(BallRadius)); Positive(BallMass, nameof(BallMass));
            Positive(Gravity, nameof(Gravity)); Positive(FixedTimestep, nameof(FixedTimestep));
            Positive(MaxShotSpeed, nameof(MaxShotSpeed)); Positive(MaxSimSeconds, nameof(MaxSimSeconds));
            NonNegative(SlidingFriction, nameof(SlidingFriction)); NonNegative(RollingResistance, nameof(RollingResistance));
            NonNegative(BallFriction, nameof(BallFriction)); NonNegative(CushionFriction, nameof(CushionFriction));
            Unit(BallRestitution, nameof(BallRestitution)); Unit(CushionRestitution, nameof(CushionRestitution));
            Unit(JawRestitution, nameof(JawRestitution));
            if (MaxStrikeOffset < 0 || MaxStrikeOffset > 1) throw new ArgumentException(nameof(MaxStrikeOffset) + " must be in [0,1]");
            // Anti-tunnelling: two balls closing at 2x max speed must not cross more than 1.5 radii in one step.
            if (MaxShotSpeed * FixedTimestep > 0.75 * BallRadius)
                throw new ArgumentException("FixedTimestep too large for MaxShotSpeed (tunnelling risk)");
        }

        static void Positive(double v, string n) { if (!(v > 0)) throw new ArgumentException(n + " must be > 0"); }
        static void NonNegative(double v, string n) { if (!(v >= 0)) throw new ArgumentException(n + " must be >= 0"); }
        static void Unit(double v, string n) { if (!(v >= 0 && v <= 1)) throw new ArgumentException(n + " must be in [0,1]"); }
    }
}
