using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 控制组件（玩法输入唯一入口）。
    /// 持有官方生成的 <see cref="GameControls"/>，经 bindingMask 做设备开关，玩法只读本组件查询 API。
    /// </summary>
    public class ControlComponent : GameFrameworkComponent
    {
        [SerializeField]
        [Tooltip("为 true 时禁用 Gameplay 地图（菜单/过场可用）。进入可操作玩法时调用 EnableAllGameControl。")]
        private bool m_blockAllInput = true;

        [Header("Input Source Enable/Disable")]
        [SerializeField]
        private bool m_enableKeyboardMouse = true;
        [SerializeField]
        private bool m_enableJoystick = true;
        [SerializeField]
        private bool m_enableTouchScreen = true;

        private GameControls m_Controls;
        private bool m_SuppressNavigate;
        private bool m_SuppressSubmit;
        private bool m_SuppressCancel;

        /// <summary>官方生成的输入包装；玩法可用 <c>Controls.Gameplay.Jump</c> 等。</summary>
        public GameControls Controls => m_Controls;

        protected override void Awake()
        {
            base.Awake();
            PointerInputUtility.EnsureInitialized();
            InputSystemUiBootstrap.EnsureInputSystemUiModule();

            m_Controls = new GameControls();
            RefreshBindingMask();
            ApplyGameplayMapEnabled();
        }

        private void OnDestroy()
        {
            if (m_Controls != null)
            {
                m_Controls.Gameplay.Disable();
                m_Controls.Dispose();
                m_Controls = null;
            }
        }

        private void Update()
        {
            m_SuppressNavigate = false;
            m_SuppressSubmit = false;
            m_SuppressCancel = false;

            UIGamepadNavigation.Tick();
        }

        /// <summary>
        /// UI 已消费手柄导航/确认/取消时调用：本帧读 Move/Confirm/Cancel 时临时排除 Gamepad。
        /// </summary>
        public void SuppressGamepadUiConsumedInput(bool navigate, bool submit, bool cancel)
        {
            if (navigate)
            {
                m_SuppressNavigate = true;
            }

            if (submit)
            {
                m_SuppressSubmit = true;
            }

            if (cancel)
            {
                m_SuppressCancel = true;
            }
        }

        #region 玩法查询 API

        public Vector2 GetMoveVector()
        {
            if (m_Controls == null)
            {
                return Vector2.zero;
            }

            return ReadVector(m_Controls.Gameplay.Move, m_SuppressNavigate);
        }

        public Vector2 GetLookVector()
        {
            if (m_Controls == null)
            {
                return Vector2.zero;
            }

            return ReadVector(m_Controls.Gameplay.Look, excludeGamepad: false);
        }

        public Vector2 GetVector(InputAction action)
        {
            return ReadVector(action, excludeGamepad: false);
        }

        public bool GetAction(InputAction action)
        {
            return QueryAction(action, a => a.IsPressed());
        }

        public bool GetActionDown(InputAction action)
        {
            return QueryAction(action, a => a.WasPressedThisFrame());
        }

        public bool GetActionUp(InputAction action)
        {
            return QueryAction(action, a => a.WasReleasedThisFrame());
        }

        #endregion

        public bool BlockAllInput
        {
            get { return m_blockAllInput; }
            set
            {
                if (m_blockAllInput == value)
                {
                    return;
                }

                m_blockAllInput = value;
                ApplyGameplayMapEnabled();
            }
        }

        public void BlockAllGameControl()
        {
            BlockAllInput = true;
        }

        public void EnableAllGameControl()
        {
            BlockAllInput = false;
        }

        public bool enableKeyboardMouse
        {
            get { return m_enableKeyboardMouse; }
            set
            {
                if (m_enableKeyboardMouse == value)
                {
                    return;
                }

                m_enableKeyboardMouse = value;
                RefreshBindingMask();
            }
        }

        public bool enableJoystick
        {
            get { return m_enableJoystick; }
            set
            {
                if (m_enableJoystick == value)
                {
                    return;
                }

                m_enableJoystick = value;
                RefreshBindingMask();
            }
        }

        public bool enableTouchScreen
        {
            get { return m_enableTouchScreen; }
            set
            {
                if (m_enableTouchScreen == value)
                {
                    return;
                }

                m_enableTouchScreen = value;
                RefreshBindingMask();
            }
        }

        private void ApplyGameplayMapEnabled()
        {
            if (m_Controls == null)
            {
                return;
            }

            if (m_blockAllInput)
            {
                m_Controls.Gameplay.Disable();
            }
            else
            {
                m_Controls.Gameplay.Enable();
            }
        }

        private void RefreshBindingMask()
        {
            if (m_Controls == null)
            {
                return;
            }

            m_Controls.bindingMask = BuildBindingMask(excludeGamepad: false);
        }

        private InputBinding? BuildBindingMask(bool excludeGamepad)
        {
            var groups = new List<string>(3);

            if (m_enableKeyboardMouse)
            {
                groups.Add(m_Controls.KeyboardMouseScheme.bindingGroup);
            }

            if (m_enableJoystick && !excludeGamepad)
            {
                groups.Add(m_Controls.GamepadScheme.bindingGroup);
            }

            if (m_enableTouchScreen)
            {
                groups.Add(m_Controls.TouchScheme.bindingGroup);
            }

            if (groups.Count == 0)
            {
                // 无一 scheme：匹配不到任何绑定
                return new InputBinding { groups = "__disabled__" };
            }

            return InputBinding.MaskByGroups(groups.ToArray());
        }

        private Vector2 ReadVector(InputAction action, bool excludeGamepad)
        {
            if (m_blockAllInput || m_Controls == null || action == null)
            {
                return Vector2.zero;
            }

            if (!excludeGamepad)
            {
                return action.ReadValue<Vector2>();
            }

            InputBinding? previous = m_Controls.bindingMask;
            m_Controls.bindingMask = BuildBindingMask(excludeGamepad: true);
            try
            {
                return action.ReadValue<Vector2>();
            }
            finally
            {
                m_Controls.bindingMask = previous;
            }
        }

        private bool QueryAction(InputAction action, Func<InputAction, bool> query)
        {
            if (m_blockAllInput || m_Controls == null || action == null)
            {
                return false;
            }

            bool excludeGamepad = ShouldExcludeGamepadForAction(action);
            if (!excludeGamepad)
            {
                return query(action);
            }

            InputBinding? previous = m_Controls.bindingMask;
            m_Controls.bindingMask = BuildBindingMask(excludeGamepad: true);
            try
            {
                return query(action);
            }
            finally
            {
                m_Controls.bindingMask = previous;
            }
        }

        private bool ShouldExcludeGamepadForAction(InputAction action)
        {
            if (m_SuppressSubmit && action == m_Controls.Gameplay.Confirm)
            {
                return true;
            }

            if (m_SuppressCancel && action == m_Controls.Gameplay.Cancel)
            {
                return true;
            }

            return false;
        }
    }
}
