using System.Collections.Generic;
using PoolGame.Core.Physics;
using PoolGame.Core.Rules;

namespace PoolGame.Client.Core
{
    public readonly struct AimChanged { public readonly double Angle; public AimChanged(double a) { Angle = a; } }
    public readonly struct PowerChanged { public readonly double Power; public PowerChanged(double p) { Power = p; } }
    public readonly struct SpinChanged { public readonly double X, Y; public SpinChanged(double x, double y) { X = x; Y = y; } }
    public readonly struct ShotFired { public readonly ShotParameters Shot; public readonly int Player; public ShotFired(ShotParameters s, int p) { Shot = s; Player = p; } }
    public readonly struct ShotSettled { public readonly ShotOutcome Outcome; public ShotSettled(ShotOutcome o) { Outcome = o; } }
    public readonly struct BallEvent { public readonly ShotEvent Event; public BallEvent(ShotEvent e) { Event = e; } }
    public readonly struct GameFinished { public readonly int Winner; public readonly WinReason Reason; public GameFinished(int w, WinReason r) { Winner = w; Reason = r; } }
}
