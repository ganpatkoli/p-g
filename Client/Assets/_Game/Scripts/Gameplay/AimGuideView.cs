using PoolGame.Core.Physics;
using UnityEngine;

namespace PoolGame.Client.Gameplay
{
    /// <summary>Ghost-ball aim line, built from the core AimPredictor (geometry only, no spin).</summary>
    public sealed class AimGuideView : MonoBehaviour
    {
        LineRenderer _line, _objectLine;
        Transform _ghost;

        public static AimGuideView Create(Transform parent, float ballRadius)
        {
            var go = new GameObject("AimGuide");
            go.transform.SetParent(parent, false);
            var v = go.AddComponent<AimGuideView>();
            v._line = v.MakeLine("CueLine", new Color(1, 1, 1, 0.85f));
            v._objectLine = v.MakeLine("ObjectLine", new Color(1f, 0.8f, 0.3f, 0.9f));
            var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            g.name = "GhostBall";
            g.transform.SetParent(go.transform, false);
            g.transform.localScale = Vector3.one * ballRadius * 2f;
            Object.Destroy(g.GetComponent<Collider>());
            g.GetComponent<Renderer>().sharedMaterial = MaterialFactory.Create(new Color(1, 1, 1, 0.35f), 0f);
            v._ghost = g.transform;
            return v;
        }

        LineRenderer MakeLine(string name, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<LineRenderer>();
            l.positionCount = 2; l.startWidth = l.endWidth = 0.006f; l.useWorldSpace = true;
            l.sharedMaterial = MaterialFactory.Create(c, 0f);
            l.startColor = l.endColor = c;
            return l;
        }

        public void Show(bool visible) { gameObject.SetActive(visible); }

        public void UpdateGuide(AimPrediction p, Vector3 cueWorld, float ballRadius)
        {
            float y = ballRadius;
            Vector3 ghost = ViewMath.ToWorld(p.GhostBallCenter, y);
            _line.SetPosition(0, cueWorld); _line.SetPosition(1, ghost);
            _ghost.position = ghost;
            _objectLine.enabled = p.HitsBall;
            if (p.HitsBall)
            {
                var d = new Vector3((float)p.ObjectDirection.X, 0, (float)p.ObjectDirection.Y);
                var objCentre = ghost + d * (ballRadius * 2f);
                _objectLine.SetPosition(0, objCentre);
                _objectLine.SetPosition(1, objCentre + d * 0.5f);
            }
        }
    }
}
