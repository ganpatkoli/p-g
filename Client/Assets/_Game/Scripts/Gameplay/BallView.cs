using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Client.Gameplay
{
    /// <summary>Visual for one ball. Pure presentation: position and spin come from the replayed core state.</summary>
    public sealed class BallView : MonoBehaviour
    {
        static readonly Color[] Palette =
        {
            new Color(0.95f, 0.82f, 0.15f), new Color(0.15f, 0.35f, 0.85f), new Color(0.88f, 0.2f, 0.15f),
            new Color(0.42f, 0.25f, 0.62f), new Color(0.94f, 0.54f, 0.14f), new Color(0.1f, 0.6f, 0.33f),
            new Color(0.55f, 0.17f, 0.17f), new Color(0.07f, 0.07f, 0.08f),
        };

        public int BallId { get; private set; }

        public static BallView Create(Transform parent, int id, float radius)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = id == 0 ? "CueBall" : "Ball" + id;
            go.transform.SetParent(parent, false);
            go.transform.localScale = Vector3.one * radius * 2f;
            Object.Destroy(go.GetComponent<Collider>());

            var view = go.AddComponent<BallView>();
            view.BallId = id;
            var rend = go.GetComponent<Renderer>();

            if (id == 0) rend.sharedMaterial = MaterialFactory.Create(new Color(0.96f, 0.96f, 0.93f), 0.8f);
            else
            {
                var colour = Palette[(id - 1) % 8];
                bool stripe = id > 8;
                rend.sharedMaterial = MaterialFactory.Create(stripe ? new Color(0.96f, 0.96f, 0.93f) : colour, 0.8f);
                if (stripe)
                {
                    var band = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    band.name = "Stripe";
                    band.transform.SetParent(go.transform, false);
                    band.transform.localScale = new Vector3(1.01f, 0.18f, 1.01f);
                    Object.Destroy(band.GetComponent<Collider>());
                    band.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Create(colour, 0.8f);
                }
            }
            return view;
        }

        /// <summary>Apply a core ball state. dt is the time since the previous apply, used to integrate rolling rotation.</summary>
        public void Apply(BallState s, float radius, float dt)
        {
            gameObject.SetActive(s.OnTable);
            if (!s.OnTable) return;
            transform.position = ViewMath.ToWorld(s.Position, radius);

            // Core (right-handed, Z up) -> Unity (left-handed, Y up): swap Y/Z and flip sign.
            var w = new Vector3(-(float)s.Wx, -(float)s.Wz, -(float)s.Wy);
            float speed = w.magnitude;
            if (speed > 1e-4f && dt > 0f)
                transform.rotation = Quaternion.AngleAxis(speed * dt * Mathf.Rad2Deg, w / speed) * transform.rotation;
        }
    }
}
