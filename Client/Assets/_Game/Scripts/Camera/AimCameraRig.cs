using UnityEngine;

namespace PoolGame.Client.CameraRig
{
    /// <summary>Two views: behind the cue ball looking along the aim, or a top-down table overview. Smoothly blended.</summary>
    public sealed class AimCameraRig : MonoBehaviour
    {
        public enum Mode { Aim, Overview }

        public Mode CurrentMode = Mode.Aim;
        public Transform Target;            // cue ball
        public float AimHeight = 0.55f;
        public float AimDistance = 0.9f;
        public float OverviewHeight = 2.4f;
        public float Smoothness = 6f;

        float _yaw;                         // radians, direction camera looks along (table plane)
        UnityEngine.Camera _cam;

        void Awake() { _cam = GetComponent<UnityEngine.Camera>(); }

        public void SetAimDirection(float yawRadians) { _yaw = yawRadians; }
        public void Toggle() { CurrentMode = CurrentMode == Mode.Aim ? Mode.Overview : Mode.Aim; }

        void LateUpdate()
        {
            Vector3 pos; Quaternion rot;
            if (CurrentMode == Mode.Aim && Target != null)
            {
                var dir = new Vector3(Mathf.Cos(_yaw), 0, Mathf.Sin(_yaw));
                pos = Target.position - dir * AimDistance + Vector3.up * AimHeight;
                rot = Quaternion.LookRotation((Target.position + dir * 0.5f) - pos, Vector3.up);
            }
            else
            {
                pos = new Vector3(0, OverviewHeight, 0);
                rot = Quaternion.Euler(90, 0, 0);
            }
            float k = 1f - Mathf.Exp(-Smoothness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, pos, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, rot, k);
        }

        /// <summary>Table-plane (y = 0) point under a screen position, or null if the ray misses.</summary>
        public Vector3? ScreenToTable(Vector2 screen)
        {
            if (_cam == null) return null;
            var ray = _cam.ScreenPointToRay(screen);
            var plane = new Plane(Vector3.up, Vector3.zero);
            return plane.Raycast(ray, out float t) ? ray.GetPoint(t) : (Vector3?)null;
        }
    }
}
