using UnityEngine;
using UnityEngine.InputSystem;

namespace EpicToonFX
{
    /// <summary>
    /// Demo-only input shim for projects that use Input System exclusively.
    /// </summary>
    internal static class ETFXInput
    {
        public static Vector3 mousePosition
        {
            get
            {
                Mouse mouse = Mouse.current;
                if (mouse == null)
                {
                    return Vector3.zero;
                }

                Vector2 position = mouse.position.ReadValue();
                return new Vector3(position.x, position.y, 0f);
            }
        }

        public static bool GetMouseButton(int button)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case 0: return mouse.leftButton.isPressed;
                case 1: return mouse.rightButton.isPressed;
                case 2: return mouse.middleButton.isPressed;
                default: return false;
            }
        }

        public static bool GetMouseButtonUp(int button)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case 0: return mouse.leftButton.wasReleasedThisFrame;
                case 1: return mouse.rightButton.wasReleasedThisFrame;
                case 2: return mouse.middleButton.wasReleasedThisFrame;
                default: return false;
            }
        }

        public static bool GetKeyDown(KeyCode keyCode)
        {
            if (TryGetMouseButtonDown(keyCode, out bool mousePressed))
            {
                return mousePressed;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[(Key)keyCode].wasPressedThisFrame;
        }

        public static bool GetKeyUp(KeyCode keyCode)
        {
            if (TryGetMouseButtonUp(keyCode, out bool mousePressed))
            {
                return mousePressed;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return keyboard[(Key)keyCode].wasReleasedThisFrame;
        }

        public static float GetAxis(string axisName)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return 0f;
            }

            switch (axisName)
            {
                case "Mouse X":
                    return mouse.delta.ReadValue().x;
                case "Mouse Y":
                    return mouse.delta.ReadValue().y;
                case "Mouse ScrollWheel":
                    return mouse.scroll.ReadValue().y / 1200f;
                default:
                    return 0f;
            }
        }

        private static bool TryGetMouseButtonDown(KeyCode keyCode, out bool pressed)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                pressed = false;
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Mouse0:
                    pressed = mouse.leftButton.wasPressedThisFrame;
                    return true;
                case KeyCode.Mouse1:
                    pressed = mouse.rightButton.wasPressedThisFrame;
                    return true;
                case KeyCode.Mouse2:
                    pressed = mouse.middleButton.wasPressedThisFrame;
                    return true;
                default:
                    pressed = false;
                    return false;
            }
        }

        private static bool TryGetMouseButtonUp(KeyCode keyCode, out bool pressed)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                pressed = false;
                return false;
            }

            switch (keyCode)
            {
                case KeyCode.Mouse0:
                    pressed = mouse.leftButton.wasReleasedThisFrame;
                    return true;
                case KeyCode.Mouse1:
                    pressed = mouse.rightButton.wasReleasedThisFrame;
                    return true;
                case KeyCode.Mouse2:
                    pressed = mouse.middleButton.wasReleasedThisFrame;
                    return true;
                default:
                    pressed = false;
                    return false;
            }
        }
    }
}
