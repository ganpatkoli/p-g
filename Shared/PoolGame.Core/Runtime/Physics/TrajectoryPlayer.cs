using System;
using PoolGame.Core.Geometry;

namespace PoolGame.Core.Physics
{
    /// <summary>Interpolates a recorded shot so a view can replay it at any frame rate.</summary>
    public sealed class TrajectoryPlayer
    {
        readonly TrajectoryRecorder _rec;

        public TrajectoryPlayer(TrajectoryRecorder recorder)
        {
            if (recorder == null || recorder.Frames.Count == 0) throw new ArgumentException("empty trajectory");
            _rec = recorder;
        }

        public double Duration => _rec.Times[_rec.Times.Count - 1];
        public int BallCount => _rec.Frames[0].Length;

        /// <summary>Writes the interpolated state at time t into <paramref name="into"/> (length = BallCount). Clamps to [0, Duration].</summary>
        public void Sample(double t, BallState[] into)
        {
            var times = _rec.Times;
            if (t <= 0) { Copy(_rec.Frames[0], into); return; }
            if (t >= Duration) { Copy(_rec.Frames[_rec.Frames.Count - 1], into); return; }

            int hi = 1;
            while (times[hi] < t) hi++;           // frames are few thousand at most; shots are short
            int lo = hi - 1;
            double k = (t - times[lo]) / (times[hi] - times[lo]);
            var a = _rec.Frames[lo]; var b = _rec.Frames[hi];
            for (int i = 0; i < into.Length; i++)
            {
                var o = into[i] ?? (into[i] = new BallState());
                o.Id = a[i].Id;
                o.OnTable = b[i].OnTable;   // pocketed balls vanish at the frame where they were pocketed
                o.Position = a[i].Position + (b[i].Position - a[i].Position) * k;
                o.Velocity = a[i].Velocity + (b[i].Velocity - a[i].Velocity) * k;
                o.Wx = a[i].Wx + (b[i].Wx - a[i].Wx) * k;
                o.Wy = a[i].Wy + (b[i].Wy - a[i].Wy) * k;
                o.Wz = a[i].Wz + (b[i].Wz - a[i].Wz) * k;
            }
        }

        static void Copy(BallState[] src, BallState[] dst)
        {
            for (int i = 0; i < dst.Length; i++) dst[i] = src[i].Clone();
        }
    }
}
