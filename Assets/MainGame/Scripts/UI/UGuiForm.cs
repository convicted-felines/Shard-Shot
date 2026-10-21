//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------


using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public abstract class UGuiForm : UIFormLogic
    {
        public const int DepthFactor = 100;
        private const float FadeTime = 0.1f;

        private static Font s_MainFont = null;
        private static TMP_FontAsset s_MainTMP_FontAsset = null;
        private Canvas m_CachedCanvas = null;
        private CanvasGroup m_CanvasGroup = null;
        private List<Canvas> m_CachedCanvasContainer = new List<Canvas>();

        private static SpriteAtlas s_MainSpriteAtlas = null;

        public int OriginalDepth
        {
            get;
            private set;
        }

        public int Depth
        {
            get
            {
                return m_CachedCanvas.sortingOrder;
            }
        }

        #region 实用方法

        public void Close()
        {
            Close(false);
        }

        public void Close(bool ignoreFade)
        {
            // 主动关闭：停掉本界面上所有协程，避免与淡出 CloseCo、子类计时器等叠加冲突。
            StopAllCoroutines();

            if (ignoreFade)
            {
                GameEntry.UI.CloseUIForm(this);
            }
            else
            {
                StartCoroutine(CloseCo(FadeTime));
            }
        }

        /// <summary>
        /// 是否捕获手柄 UI 导航（左摇杆选按钮、A 确认）。默认 Default/Popup 组捕获；Game 组 HUD 不捕获。
        /// </summary>
        public virtual bool CaptureGamepadNavigation
        {
            get
            {
                if (UIForm == null || UIForm.UIGroup == null)
                {
                    return false;
                }

                string groupName = UIForm.UIGroup.Name;
                return groupName == "Default" || groupName == "Popup";
            }
        }

        /// <summary>
        /// 手柄 B 是否可关闭本界面。默认仅 Popup 组可关。
        /// </summary>
        public virtual bool CanCloseByGamepadB
        {
            get
            {
                return UIForm != null && UIForm.UIGroup != null && UIForm.UIGroup.Name == "Popup";
            }
        }

        /// <summary>手柄 B 关闭入口；子类可覆写以走带清理逻辑的关闭方法。</summary>
        public virtual void CloseByGamepadB()
        {
            Close();
        }

        public void PlayUISound(int uiSoundId)
        {
            GameEntry.Sound.PlayUISound(uiSoundId);
        }

        public static void SetMainFont(Font mainFont)
        {
            if (mainFont == null)
            {
                Log.Error("Main font is invalid.");
                return;
            }

            s_MainFont = mainFont;
        }

        public static void SetMainTMPFontAsset(TMP_FontAsset mainTMPFontAsset)
        {
            if (mainTMPFontAsset == null)
            {
                Log.Error("Main TMP font asset is invalid");
                return;
            }
            s_MainTMP_FontAsset = mainTMPFontAsset;
        }
        public static void SetMainSpriteAtlas(SpriteAtlas spriteAtlas)
        {
            if (spriteAtlas == null)
            {
                Log.Error("Main sprite atlas is invalid");
                return;
            }
            s_MainSpriteAtlas = spriteAtlas;
        }

        public SpriteAtlas MainSpriteAtals
        {
            get { return s_MainSpriteAtlas; }
            //set { s_MainSpriteAtlas = value; }
        }


        private void UITextTranslationForAll()
        {
            TMP_Text[] text = GetComponentsInChildren<TMP_Text>(true);

            for (int i = 0; i < text.Length; i++)
            {
                TMP_Text t = text[i];
                if (t == null || !t.IsActive())
                {
                    continue;
                }

                string textName = t.gameObject.name;
                if (string.IsNullOrEmpty(textName))
                {
                    continue;
                }

                if (!GameEntry.Localization.TryGetUiTranslationTableId(textName, out int textId))
                {
                    continue;
                }

                string translated = GameEntry.Localization.GetUITranslationStringByTableId(textId, textName);
                if (translated != null)
                {
                    t.text = translated;
                }
            }
        }

        public string UITextTranslation(string TranslationKey)
        {
            return GameEntry.Localization.GetUITranslationString(TranslationKey);
        }


        #endregion

        #region 生命周期

#if UNITY_2017_3_OR_NEWER
        protected override void OnInit(object userData)
#else
        protected internal override void OnInit(object userData)
#endif
        {
            base.OnInit(userData);

            m_CachedCanvas = gameObject.GetOrAddComponent<Canvas>();
            m_CachedCanvas.overrideSorting = true;
            OriginalDepth = m_CachedCanvas.sortingOrder;

            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();

            RectTransform transform = GetComponent<RectTransform>();
            transform.anchorMin = Vector2.zero;
            transform.anchorMax = Vector2.one;
            transform.anchoredPosition = Vector2.zero;
            transform.sizeDelta = Vector2.zero;

            gameObject.GetOrAddComponent<GraphicRaycaster>();

            //根据字符在预制体中的名称来找到UI翻译表进行翻译
            UITextTranslationForAll();

        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnRecycle()
#else
        protected internal override void OnRecycle()
#endif
        {
            base.OnRecycle();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);

            m_CanvasGroup.alpha = 1f;
            // 不再在此处 StopAllCoroutines：打开/恢复流程里 OnOpen 后常会立刻 OnResume，
            // 若在子类 OnOpen 里 StartCoroutine 并 yield，会被误杀。界面进池时由 OnRecycle 统一清理。
            //StartCoroutine(m_CanvasGroup.FadeToAlpha(1f, FadeTime));
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnClose(bool isShutdown, object userData)
#else
        protected internal override void OnClose(bool isShutdown, object userData)
#endif
        {
            base.OnClose(isShutdown, userData);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnPause()
#else
        protected internal override void OnPause()
#endif
        {
            base.OnPause();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnResume()
#else
        protected internal override void OnResume()
#endif
        {
            base.OnResume();

            m_CanvasGroup.alpha = 1f;
            // 与 OnOpen 相同：避免打断子类在刚打开/恢复时启动的协程。被遮挡时一般会 SetActive(false)，Unity 会停掉协程。
            //StartCoroutine(m_CanvasGroup.FadeToAlpha(1f, FadeTime));
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnCover()
#else
        protected internal override void OnCover()
#endif
        {
            base.OnCover();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnReveal()
#else
        protected internal override void OnReveal()
#endif
        {
            base.OnReveal();
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnRefocus(object userData)
#else
        protected internal override void OnRefocus(object userData)
#endif
        {
            base.OnRefocus(userData);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#else
        protected internal override void OnUpdate(float elapseSeconds, float realElapseSeconds)
#endif
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
#else
        protected internal override void OnDepthChanged(int uiGroupDepth, int depthInUIGroup)
#endif
        {
            int oldDepth = Depth;
            base.OnDepthChanged(uiGroupDepth, depthInUIGroup);
            int deltaDepth = UGuiGroupHelper.DepthFactor * uiGroupDepth + DepthFactor * depthInUIGroup - oldDepth + OriginalDepth;
            GetComponentsInChildren(true, m_CachedCanvasContainer);
            for (int i = 0; i < m_CachedCanvasContainer.Count; i++)
            {
                m_CachedCanvasContainer[i].sortingOrder += deltaDepth;
            }

            m_CachedCanvasContainer.Clear();
        }

        private IEnumerator CloseCo(float duration)
        {
            yield return m_CanvasGroup.FadeToAlpha(0f, duration);
            GameEntry.UI.CloseUIForm(this);
        }

        #endregion
    }
}
