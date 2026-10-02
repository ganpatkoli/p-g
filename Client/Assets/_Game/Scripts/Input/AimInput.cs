using UnityEngine;
using UnityEngine.InputSystem;

namespace PoolGame.Client.Input
{
    /// <summary>
    /// Reads pointer/keyboard and exposes raw player intent. It knows nothing about rules or physics.
    /// Pointer (mouse or touch) aims, wheel or Up/Down page keys set power, arrow keys move the cue-tip contact point,
    /// click/tap or Space shoots.
    /// </summary>
    public sealed class AimInput
    {
        public float Power { get; private set; } = 0.5f;
        public Vector2 Spin { get; private set; }
        public bool ShootPressed { get; private set; }
        public bool PointerPressed { get; private set; }
        public Vector2 PointerScreen { get; private set; }
        public bool HasPointer { get; private set; }

        const float PowerPerScroll = 0.0005f;
        const float PowerPerSecond = 0.6f;
        const float SpinPerSecond = 1.2f;

        public void Tick(float dt)
        {
            var pointer = Pointer.current;
            HasPointer = pointer != null;
            PointerPressed = HasPointer && pointer.press.wasPressedThisFrame;
            if (HasPointer) PointerScreen = pointer.position.ReadValue();

            var mouse = Mouse.current;
            if (mouse != null) Power = Mathf.Clamp01(Power + mouse.scroll.ReadValue().y * PowerPerScroll);

            var kb = Keyboard.current;
            ShootPressed = PointerPressed;
            if (kb == null) return;

            if (kb.pageUpKey.isPressed || kb.equalsKey.isPressed) Power = Mathf.Clamp01(Power + PowerPerSecond * dt);
            if (kb.pageDownKey.isPressed || kb.minusKey.isPressed) Power = Mathf.Clamp01(Power - PowerPerSecond * dt);

            var s = Spin;
            if (kb.leftArrowKey.isPressed) s.x -= SpinPerSecond * dt;
            if (kb.rightArrowKey.isPressed) s.x += SpinPerSecond * dt;
            if (kb.upArrowKey.isPressed) s.y += SpinPerSecond * dt;
            if (kb.downArrowKey.isPressed) s.y -= SpinPerSecond * dt;
            if (kb.rKey.wasPressedThisFrame) s = Vector2.zero;
            Spin = Vector2.ClampMagnitude(s, 1f);

            if (kb.spaceKey.wasPressedThisFrame) ShootPressed = true;
        }
    }
}
