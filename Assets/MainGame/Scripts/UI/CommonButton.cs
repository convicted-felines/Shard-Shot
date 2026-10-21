//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public class CommonButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        private const float FadeTime = 0.3f;
        private const float DisabledAlpha = 0.4f;
        private static readonly Color NormalColor = Color.white;
        private static readonly Color HighlightColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        [Tooltip("最长点击时间，超过这个时间为长按")]
        public float clickInterval = 0.2f;
        [SerializeField]
        private bool responseOnceByPress = false;

        [SerializeField]
        [Tooltip("是否参与手柄左摇杆 UI 跳转选中")]
        private bool m_IncludeInGamepadNavigation = true;

        /// <summary>
        /// 点击抬起后触发
        /// </summary>
        [SerializeField]
        [Tooltip("点击抬起后触发")]
        public UnityEvent m_OnClick = null;
        /// <summary>
        /// 点击时触发
        /// </summary>
        [SerializeField]
        [Tooltip("点击时触发")]
        public UnityEvent m_OnPointerDown = null;
        /// <summary>
        /// 抬起时触发
        /// </summary>
        [SerializeField]
        [Tooltip("抬起时触发")]
        public UnityEvent m_OnPointerUp = null;
        /// <summary>
        /// 按下时触发
        /// </summary>
        [SerializeField]
        [Tooltip("按下时触发")]
        public UnityEvent m_OnPress = null;
        /// <summary>
        /// 长按时触发
        /// </summary>
        [SerializeField]
        [Tooltip("长按时触发")]
        public UnityEvent m_OnLongPress = null;
        /// <summary>
        /// 悬停时触发
        /// </summary>
        [SerializeField]
        [Tooltip("悬停时触发")]
        public UnityEvent m_OnHover = null;

        private CanvasGroup m_CanvasGroup = null;
        private Graphic[] m_Graphics;
        private Color[] m_OriginalColors;

        private bool isDown = false;
        private bool isPress = false;
        private float downTime = 0;
        private float clickIntervalTime = 0;
        private int clickCount = 0;
        private float pressDurationTime = 0.5f;
        private bool m_GamepadSelected;
        private bool m_PointerHovered;
        private Color m_CurrentTint = NormalColor;
        private Coroutine m_HighlightCoroutine;
        private int m_ActivePointerId = int.MinValue;
        /// <summary>父节点 IDragHandler 抢走拖拽时 EventSystem 会发一次假 PointerUp；此时改由物理指针状态决定抬起。</summary>
        private bool m_AwaitingPhysicalRelease;

        /// <summary>是否可作为手柄导航目标。</summary>
        public bool IsGamepadNavigable
        {
            get
            {
                if (!m_IncludeInGamepadNavigation || !isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    return false;
                }

                if (m_CanvasGroup != null && (!m_CanvasGroup.interactable || !m_CanvasGroup.blocksRaycasts || m_CanvasGroup.alpha <= 0.01f))
                {
                    return false;
                }

                return true;
            }
        }

        private void Awake()
        {
            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            CacheGraphics();
            ApplyHighlightVisualImmediate();
        }

        private void OnEnable()
        {
            ApplyHighlightVisualImmediate();
        }

        private void OnDisable()
        {
            m_GamepadSelected = false;
            m_PointerHovered = false;
            isDown = false;
            m_ActivePointerId = int.MinValue;
            m_AwaitingPhysicalRelease = false;
            StopHighlightCoroutine();
            if (m_CanvasGroup != null)
            {
                m_CanvasGroup.alpha = 1f;
            }

            ApplyTintImmediate(NormalColor);
            m_CurrentTint = NormalColor;
        }

        private void Update()
        {
            if (m_AwaitingPhysicalRelease)
            {
                UpdatePhysicalHoldRelease();
            }

            if (isDown)
            {
                downTime += Time.deltaTime;

                if(downTime >= clickInterval)
                {
                    isPress = true;
                    m_OnLongPress.Invoke();
                }
            }

        }

        /// <summary>手柄选中：与鼠标悬停共用高亮（白色 → 浅灰）。</summary>
        public void SetGamepadSelected(bool selected)
        {
            if (m_GamepadSelected == selected)
            {
                return;
            }

            m_GamepadSelected = selected;
            RefreshHighlightVisual();
            if (selected)
            {
                m_OnHover?.Invoke();
            }
        }

        /// <summary>手柄 A 触发点击（等同 OnPointerClick）。</summary>
        public void InvokeClickFromGamepad()
        {
            if (!IsGamepadNavigable)
            {
                return;
            }

            m_OnClick?.Invoke();
        }

        /// <summary>外部修改 interactable 后刷新明暗。</summary>
        public void RefreshVisualState()
        {
            RefreshHighlightVisual();
        }

        private void CacheGraphics()
        {
            m_Graphics = GetComponentsInChildren<Graphic>(true);
            m_OriginalColors = new Color[m_Graphics.Length];
            for (int i = 0; i < m_Graphics.Length; i++)
            {
                m_OriginalColors[i] = m_Graphics[i] != null ? m_Graphics[i].color : Color.white;
            }
        }

        private void RefreshHighlightVisual()
        {
            if (m_CanvasGroup == null)
            {
                return;
            }

            StopHighlightCoroutine();
            m_HighlightCoroutine = StartCoroutine(AnimateHighlight());
        }

        private void ApplyHighlightVisualImmediate()
        {
            if (m_CanvasGroup == null)
            {
                return;
            }

            StopHighlightCoroutine();
            GetHighlightTargets(out float targetAlpha, out Color targetTint);
            m_CanvasGroup.alpha = targetAlpha;
            m_CurrentTint = targetTint;
            ApplyTintImmediate(targetTint);
        }

        private void GetHighlightTargets(out float targetAlpha, out Color targetTint)
        {
            bool canHighlight = m_CanvasGroup.interactable;
            if (!canHighlight)
            {
                targetAlpha = DisabledAlpha;
                targetTint = NormalColor;
                return;
            }

            bool highlighted = m_GamepadSelected || m_PointerHovered;
            targetAlpha = 1f;
            targetTint = highlighted ? HighlightColor : NormalColor;
        }

        private void ApplyTintImmediate(Color tint)
        {
            if (m_Graphics == null)
            {
                return;
            }

            for (int i = 0; i < m_Graphics.Length; i++)
            {
                Graphic graphic = m_Graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                graphic.color = m_OriginalColors[i] * tint;
            }
        }

        private IEnumerator AnimateHighlight()
        {
            GetHighlightTargets(out float targetAlpha, out Color targetTint);
            float time = 0f;
            float startAlpha = m_CanvasGroup.alpha;
            Color startTint = m_CurrentTint;
            while (time < FadeTime)
            {
                time += Time.unscaledDeltaTime;
                float t = time / FadeTime;
                m_CanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                m_CurrentTint = Color.Lerp(startTint, targetTint, t);
                ApplyTintImmediate(m_CurrentTint);
                yield return null;
            }

            m_CanvasGroup.alpha = targetAlpha;
            m_CurrentTint = targetTint;
            ApplyTintImmediate(targetTint);
            m_HighlightCoroutine = null;
        }

        private void StopHighlightCoroutine()
        {
            if (m_HighlightCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_HighlightCoroutine);
            m_HighlightCoroutine = null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_PointerHovered = true;
            RefreshHighlightVisual();
            m_OnHover.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_PointerHovered = false;
            RefreshHighlightVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_ActivePointerId = eventData.pointerId;
            m_AwaitingPhysicalRelease = false;
            isDown = true;

            m_OnPointerDown.Invoke();

            StartCoroutine(CoutinuousPressRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            if (m_ActivePointerId != int.MinValue && eventData.pointerId != m_ActivePointerId)
            {
                return;
            }

            // 父级接管拖拽时 EventSystem 会对 pointerPress 发 PointerUp，但手指仍按着；
            // 只要触点仍在本按钮范围内，就继续视为按住，真正抬起由 Update 检测。
            if (isDown && IsPointerPhysicallyHeld(eventData.pointerId))
            {
                if (IsScreenPointInsideButton(eventData.position))
                {
                    m_AwaitingPhysicalRelease = true;
                    return;
                }
            }

            ReleasePointerHold();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            m_OnClick.Invoke();
        }

        private void UpdatePhysicalHoldRelease()
        {
            if (m_ActivePointerId == int.MinValue)
            {
                ReleasePointerHold();
                return;
            }

            if (!IsPointerPhysicallyHeld(m_ActivePointerId))
            {
                ReleasePointerHold();
                return;
            }

            if (!TryGetPointerScreenPosition(m_ActivePointerId, out Vector2 screenPos)
                || !IsScreenPointInsideButton(screenPos))
            {
                ReleasePointerHold();
            }
        }

        private void ReleasePointerHold()
        {
            if (!isDown && !m_AwaitingPhysicalRelease)
            {
                m_ActivePointerId = int.MinValue;
                return;
            }

            bool wasDown = isDown;
            isDown = false;
            downTime = 0;
            m_AwaitingPhysicalRelease = false;
            m_ActivePointerId = int.MinValue;

            if (wasDown)
            {
                m_OnPointerUp.Invoke();
            }
        }

        private static bool IsPointerPhysicallyHeld(int pointerId)
        {
            if (pointerId < 0)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                return PointerInputUtility.GetMouseButton(0);
#else
                return false;
#endif
            }

            int touchCount = PointerInputUtility.TouchCount;
            for (int i = 0; i < touchCount; i++)
            {
                if (!PointerInputUtility.TryGetTouch(i, out int fingerId, out _, out TouchPhase phase))
                {
                    continue;
                }

                if (fingerId != pointerId)
                {
                    continue;
                }

                return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
            }

            return false;
        }

        private static bool TryGetPointerScreenPosition(int pointerId, out Vector2 screenPos)
        {
            if (pointerId < 0)
            {
#if UNITY_EDITOR || UNITY_STANDALONE
                screenPos = PointerInputUtility.MousePosition;
                return PointerInputUtility.GetMouseButton(0);
#else
                screenPos = default;
                return false;
#endif
            }

            int touchCount = PointerInputUtility.TouchCount;
            for (int i = 0; i < touchCount; i++)
            {
                if (!PointerInputUtility.TryGetTouch(i, out int fingerId, out Vector2 pos, out TouchPhase phase))
                {
                    continue;
                }

                if (fingerId != pointerId)
                {
                    continue;
                }

                screenPos = pos;
                return phase != TouchPhase.Ended && phase != TouchPhase.Canceled;
            }

            screenPos = default;
            return false;
        }

        private bool IsScreenPointInsideButton(Vector2 screenPos)
        {
            RectTransform rectTransform = transform as RectTransform;
            if (rectTransform == null)
            {
                return false;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera eventCamera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                eventCamera = canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(rectTransform, screenPos, eventCamera);
        }

        private IEnumerator CoutinuousPressRoutine()
        {
            while (isDown)
            {
                m_OnPress.Invoke();
                yield return null;
            }
        }
    }
}
