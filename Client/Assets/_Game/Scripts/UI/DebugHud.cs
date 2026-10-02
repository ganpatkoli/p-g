using PoolGame.Client.Gameplay;
using PoolGame.Core.Rules;
using UnityEngine;

namespace PoolGame.Client.UI
{
    /// <summary>
    /// Placeholder gameplay HUD (IMGUI): turn, groups, message, power bar, spin ball.
    /// Shows state only. No economy or rules logic lives here. Replaced by uGUI/UI Toolkit in a later phase.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        public ShotController Controller;

        void OnGUI()
        {
            if (Controller == null || Controller.State == null) return;
            var s = Controller.State;
            float w = Screen.width, h = Screen.height;

            for (int i = 0; i < 2; i++)
            {
                var rect = new Rect(10 + i * (w - 230), 10, 220, 52);
                GUI.Box(rect, "");
                string label = $"Player {i + 1}" + (s.CurrentPlayer == i && !s.IsOver ? "  <  turn" : "");
                GUI.Label(new Rect(rect.x + 8, rect.y + 4, 210, 22), label);
                GUI.Label(new Rect(rect.x + 8, rect.y + 26, 210, 22), s.PlayerGroups[i] == BallGroup.None ? "Open table" : s.PlayerGroups[i].ToString());
            }

            GUI.Label(new Rect(w / 2 - 300, 12, 600, 30), Controller.Message, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });

            // power bar
            var pr = new Rect(20, h - 190, 24, 160);
            GUI.Box(pr, "");
            float p = Controller.Power;
            GUI.Box(new Rect(pr.x + 2, pr.y + pr.height - 2 - (pr.height - 4) * p, pr.width - 4, (pr.height - 4) * p), "");
            GUI.Label(new Rect(pr.x - 4, pr.y - 22, 80, 20), $"Power {Mathf.RoundToInt(p * 100)}%");

            // spin ball
            var sb = new Rect(70, h - 150, 110, 110);
            GUI.Box(sb, "");
            var dot = new Rect(sb.center.x + Controller.Spin.x * 45 - 5, sb.center.y - Controller.Spin.y * 45 - 5, 10, 10);
            GUI.Box(dot, "");
            GUI.Label(new Rect(sb.x, sb.y - 22, 140, 20), "Spin (arrows, R reset)");

            GUI.Label(new Rect(w - 360, h - 30, 350, 24), "Wheel/+/- power · Click or Space shoot · C camera");
        }
    }
}
