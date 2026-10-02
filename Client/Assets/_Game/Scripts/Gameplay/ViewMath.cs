using PoolGame.Core.Geometry;
using UnityEngine;

namespace PoolGame.Client.Gameplay
{
    /// <summary>Core space (X along table, Y across, Z up) to Unity space (X, Y up, Z). Table surface is y = 0.</summary>
    public static class ViewMath
    {
        public static Vector3 ToWorld(Vec2 p, float height = 0f) => new Vector3((float)p.X, height, (float)p.Y);
        public static Vec2 ToCore(Vector3 w) => new Vec2(w.x, w.z);
    }
}
