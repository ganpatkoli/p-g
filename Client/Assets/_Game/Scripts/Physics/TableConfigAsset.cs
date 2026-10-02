using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Client.Physics
{
    [CreateAssetMenu(menuName = "PoolGame/Table Config", fileName = "TableConfig")]
    public sealed class TableConfigAsset : ScriptableObject
    {
        public TableConfig Config = new TableConfig();
        public TableConfig ToConfig() => Config.Clone();
    }
}
