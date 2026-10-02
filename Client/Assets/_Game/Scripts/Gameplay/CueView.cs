using UnityEngine;

namespace PoolGame.Client.Gameplay
{
    /// <summary>The cue stick. Sits behind the cue ball along the aim and pulls back with power.</summary>
    public sealed class CueView : MonoBehaviour
    {
        const float Length = 1.45f;
        const float MaxPull = 0.25f;
        const float Gap = 0.02f;
        const float Thickness = 0.014f;

        public static CueView Create(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "Cue";
            go.transform.SetParent(parent, false);
            go.transform.localScale = new Vector3(Thickness, Length / 2f, Thickness);
            Object.Destroy(go.GetComponent<Collider>());
            go.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Create(new Color(0.8f, 0.6f, 0.35f), 0.4f);
            return go.AddComponent<CueView>();
        }

        public void Show(bool visible) { gameObject.SetActive(visible); }

        /// <summary>
        /// cuePos: cue ball centre (world). aimYaw: radians in the table plane. spin: cue-tip offset on the ball face (-1..1).
        /// </summary>
        public void Place(Vector3 cuePos, float aimYaw, float power, Vector2 spin, float ballRadius)
        {
            var dir = new Vector3(Mathf.Cos(aimYaw), 0f, Mathf.Sin(aimYaw));
            var right = new Vector3(dir.z, 0f, -dir.x);                       // right of the aim direction
            Vector3 tip = cuePos + right * (spin.x * ballRadius * 0.5f) + Vector3.up * (spin.y * ballRadius * 0.5f);
            Vector3 tipBack = tip - dir * (ballRadius + Gap + power * MaxPull);
            // cylinder axis is local Y: put the tip at the front end
            transform.position = tipBack - dir * (Length / 2f);
            transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
        }
    }
}
