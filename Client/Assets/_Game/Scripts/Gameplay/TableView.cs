using PoolGame.Core.Geometry;
using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Client.Gameplay
{
    /// <summary>Builds a simple procedural table (cloth, rails, pockets) from TableConfig until real art exists.</summary>
    public static class TableView
    {
        const float RailWidth = 0.12f, RailHeight = 0.06f;

        public static Transform Build(TableConfig t)
        {
            var root = new GameObject("Table").transform;
            var cloth = Block(root, "Cloth", Vector3.zero, new Vector3((float)t.Length + 0.02f, 0.02f, (float)t.Width + 0.02f), new Color(0.1f, 0.42f, 0.28f));
            cloth.localPosition = new Vector3(0, -0.01f, 0);

            float hl = (float)t.HalfLength, hw = (float)t.HalfWidth, y = RailHeight / 2;
            var wood = new Color(0.30f, 0.17f, 0.09f);
            // long rails (split by the side pockets), short rails
            float segLen = hl - (float)t.SideMouthHalf - (float)t.CornerMouth;
            foreach (int sy in new[] { -1, 1 })
                foreach (int sx in new[] { -1, 1 })
                    Block(root, "RailLong", new Vector3(sx * ((float)t.SideMouthHalf + segLen / 2), y, sy * (hw + RailWidth / 2)),
                          new Vector3(segLen, RailHeight, RailWidth), wood);
            float shortLen = 2 * (hw - (float)t.CornerMouth);
            foreach (int sx in new[] { -1, 1 })
                Block(root, "RailShort", new Vector3(sx * (hl + RailWidth / 2), y, 0), new Vector3(RailWidth, RailHeight, shortLen), wood);

            foreach (var p in t.Pockets())
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = p.IsSide ? "PocketSide" : "PocketCorner";
                go.transform.SetParent(root, false);
                go.transform.localPosition = new Vector3((float)p.Center.X, -0.011f, (float)p.Center.Y);
                go.transform.localScale = new Vector3((float)p.Radius * 1.5f, 0.005f, (float)p.Radius * 1.5f);
                Object.Destroy(go.GetComponent<Collider>());
                go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Create(Color.black, 0f);
            }
            return root;
        }

        static Transform Block(Transform parent, string name, Vector3 pos, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Create(c, 0.2f);
            return go.transform;
        }
    }
}
