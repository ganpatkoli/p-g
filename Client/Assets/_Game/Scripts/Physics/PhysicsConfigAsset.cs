using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Client.Physics
{
    /// <summary>Designer-editable physics tuning. The server loads the same values from its own config.</summary>
    [CreateAssetMenu(menuName = "PoolGame/Physics Config", fileName = "PhysicsConfig")]
    public sealed class PhysicsConfigAsset : ScriptableObject
    {
        public PhysicsConfig Config = new PhysicsConfig();

        public PhysicsConfig ToConfig()
        {
            var c = Config.Clone();
            c.Validate();
            return c;
        }
    }
}
