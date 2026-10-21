using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.TouchPhase;

namespace MainGame
{
    /// <summary>
    /// 指针/触摸读取：替代 UnityEngine.Input.mousePosition / GetTouch（纯 Input System 模式下旧 API 会抛异常）。
    /// </summary>
    public static class PointerInputUtility
    {
        private static bool s_EnhancedTouchEnabled;

        public static void EnsureInitialized()
        {
            if (s_EnhancedTouchEnabled)
            {
                return;
            }

            EnhancedTouchSupport.Enable();
            s_EnhancedTouchEnabled = true;
        }

        public static Vector2 MousePosition
        {
            get
            {
                Mouse mouse = Mouse.current;
                return mouse != null ? mouse.position.ReadValue() : Vector2.zero;
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

        public static bool GetMouseButtonDown(int button)
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            switch (button)
            {
                case 0: return mouse.leftButton.wasPressedThisFrame;
                case 1: return mouse.rightButton.wasPressedThisFrame;
                case 2: return mouse.middleButton.wasPressedThisFrame;
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

        public static int TouchCount
        {
            get
            {
                EnsureInitialized();
                return Touch.activeTouches.Count;
            }
        }

        public static bool TryGetTouch(int index, out int fingerId, out Vector2 position, out TouchPhase phase)
        {
            EnsureInitialized();
            var touches = Touch.activeTouches;
            if (index < 0 || index >= touches.Count)
            {
                fingerId = 0;
                position = default;
                phase = TouchPhase.Canceled;
                return false;
            }

            Touch touch = touches[index];
            fingerId = touch.finger.index;
            position = touch.screenPosition;
            phase = ToUnityTouchPhase(touch.phase);
            return true;
        }

        private static TouchPhase ToUnityTouchPhase(UnityEngine.InputSystem.TouchPhase phase)
        {
            switch (phase)
            {
                case UnityEngine.InputSystem.TouchPhase.Began:
                    return TouchPhase.Began;
                case UnityEngine.InputSystem.TouchPhase.Moved:
                    return TouchPhase.Moved;
                case UnityEngine.InputSystem.TouchPhase.Stationary:
                    return TouchPhase.Stationary;
                case UnityEngine.InputSystem.TouchPhase.Ended:
                    return TouchPhase.Ended;
                case UnityEngine.InputSystem.TouchPhase.Canceled:
                    return TouchPhase.Canceled;
                default:
                    return TouchPhase.Canceled;
            }
        }
    }
}
