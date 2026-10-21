using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 手柄 UI 导航：仅在当前最上层可捕获的界面内，用左摇杆在 <see cref="CommonButton"/> 间跳转，A 确认、Popup 的 B 关闭。
    /// </summary>
    public static class UIGamepadNavigation
    {
        private const float StickDeadZone = 0.5f;
        private const float NavigateRepeatSeconds = 0.22f;
        private const float MaxAngleDot = 0.35f;

        private static readonly List<UIForm> s_LoadedForms = new List<UIForm>();
        private static readonly List<CommonButton> s_Buttons = new List<CommonButton>();

        private static UGuiForm s_CurrentForm;
        private static CommonButton s_Selected;
        private static bool s_StickLatched;
        private static float s_NextNavigateTime;

        public static void Tick()
        {
            if (GameEntry.UI == null)
            {
                ClearSelection();
                return;
            }

            Gamepad pad = Gamepad.current;
            bool aDown = pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool bDown = pad != null && pad.buttonEast.wasPressedThisFrame;
            Vector2 stick = pad != null ? pad.leftStick.ReadValue() : Vector2.zero;
            float stickMag = stick.magnitude;

            UGuiForm closeablePopup = FindTopmostCloseablePopup();
            UGuiForm navigableForm = FindTopmostNavigableForm();

            if (navigableForm != s_CurrentForm)
            {
                ClearSelection();
                s_CurrentForm = navigableForm;
                if (s_CurrentForm != null)
                {
                    CollectButtons(s_CurrentForm, s_Buttons);
                    if (s_Buttons.Count > 0)
                    {
                        SetSelected(PickDefaultButton(s_Buttons));
                    }
                }
            }
            else if (s_CurrentForm != null)
            {
                CollectButtons(s_CurrentForm, s_Buttons);
                if (s_Selected != null && !s_Buttons.Contains(s_Selected))
                {
                    SetSelected(s_Buttons.Count > 0 ? PickDefaultButton(s_Buttons) : null);
                }
                else if (s_Selected == null && s_Buttons.Count > 0)
                {
                    SetSelected(PickDefaultButton(s_Buttons));
                }
            }

            bool consumedNavigate = false;
            bool consumedSubmit = false;
            bool consumedCancel = false;

            if (bDown && closeablePopup != null)
            {
                closeablePopup.CloseByGamepadB();
                consumedCancel = true;
            }

            if (navigableForm != null && aDown && s_Selected != null)
            {
                s_Selected.InvokeClickFromGamepad();
                consumedSubmit = true;
            }

            if (navigableForm != null)
            {
                if (stickMag >= StickDeadZone)
                {
                    consumedNavigate = true;
                    if (!s_StickLatched || Time.unscaledTime >= s_NextNavigateTime)
                    {
                        CommonButton next = FindButtonInDirection(s_Selected, stick.normalized, s_Buttons);
                        if (next != null)
                        {
                            SetSelected(next);
                        }

                        s_StickLatched = true;
                        s_NextNavigateTime = Time.unscaledTime + NavigateRepeatSeconds;
                    }
                }
                else
                {
                    s_StickLatched = false;
                    s_NextNavigateTime = 0f;
                }
            }
            else
            {
                s_StickLatched = stickMag >= StickDeadZone;
            }

            if ((consumedNavigate || consumedSubmit || consumedCancel) && GameEntry.Control != null)
            {
                GameEntry.Control.SuppressGamepadUiConsumedInput(consumedNavigate, consumedSubmit, consumedCancel);
            }
        }

        private static UGuiForm FindTopmostNavigableForm()
        {
            return FindTopmostForm(form => form.CaptureGamepadNavigation && HasNavigableButtons(form));
        }

        private static UGuiForm FindTopmostCloseablePopup()
        {
            return FindTopmostForm(form => form.CanCloseByGamepadB);
        }

        private static UGuiForm FindTopmostForm(System.Func<UGuiForm, bool> predicate)
        {
            s_LoadedForms.Clear();
            GameEntry.UI.GetAllLoadedUIForms(s_LoadedForms);

            UGuiForm best = null;
            int bestDepth = int.MinValue;
            for (int i = 0; i < s_LoadedForms.Count; i++)
            {
                UIForm uiForm = s_LoadedForms[i];
                if (uiForm == null || uiForm.Logic == null)
                {
                    continue;
                }

                if (!(uiForm.Logic is UGuiForm form) || !form.isActiveAndEnabled || !form.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!predicate(form))
                {
                    continue;
                }

                if (form.Depth >= bestDepth)
                {
                    bestDepth = form.Depth;
                    best = form;
                }
            }

            return best;
        }

        private static bool HasNavigableButtons(UGuiForm form)
        {
            CollectButtons(form, s_Buttons);
            return s_Buttons.Count > 0;
        }

        private static void CollectButtons(UGuiForm form, List<CommonButton> results)
        {
            results.Clear();
            if (form == null)
            {
                return;
            }

            CommonButton[] buttons = form.GetComponentsInChildren<CommonButton>(false);
            for (int i = 0; i < buttons.Length; i++)
            {
                CommonButton button = buttons[i];
                if (button != null && button.IsGamepadNavigable)
                {
                    results.Add(button);
                }
            }
        }

        private static CommonButton PickDefaultButton(List<CommonButton> buttons)
        {
            CommonButton best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < buttons.Count; i++)
            {
                Vector3 pos = buttons[i].transform.position;
                float score = pos.y * -1000f + pos.x;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = buttons[i];
                }
            }

            return best;
        }

        private static CommonButton FindButtonInDirection(CommonButton from, Vector2 direction, List<CommonButton> buttons)
        {
            if (buttons.Count == 0)
            {
                return null;
            }

            Vector2 origin = from != null
                ? (Vector2)from.transform.position
                : (Vector2)PickDefaultButton(buttons).transform.position;

            CommonButton best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < buttons.Count; i++)
            {
                CommonButton candidate = buttons[i];
                if (candidate == from)
                {
                    continue;
                }

                Vector2 delta = (Vector2)candidate.transform.position - origin;
                float dist = delta.magnitude;
                if (dist < 0.01f)
                {
                    continue;
                }

                float dot = Vector2.Dot(delta.normalized, direction);
                if (dot < MaxAngleDot)
                {
                    continue;
                }

                float score = dist / Mathf.Max(dot, 0.01f);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static void SetSelected(CommonButton button)
        {
            if (s_Selected == button)
            {
                return;
            }

            if (s_Selected != null)
            {
                s_Selected.SetGamepadSelected(false);
            }

            s_Selected = button;
            if (s_Selected != null)
            {
                s_Selected.SetGamepadSelected(true);
            }
        }

        private static void ClearSelection()
        {
            SetSelected(null);
            s_CurrentForm = null;
            s_Buttons.Clear();
        }
    }
}
