//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System.Collections;
using GameFramework;
using TMPro;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public class DialogForm : UGuiForm
    {
        [SerializeField]
        private TMP_Text m_TitleText = null;

        [SerializeField]
        private TMP_Text m_MessageText = null;

        [Header("正文逐字显示（可选）")]
        [SerializeField]
        private bool m_MessageRevealByCharacter = false;

        [SerializeField]
        [Min(0f)]
        private float m_MessageRevealSecondsPerChar = 0.03f;

        [Tooltip("可选；不指定时从 m_MessageText 同节点获取 CommonButton。点击时跳过逐字、一次显示全文。")]
        [SerializeField]
        private CommonButton m_MessageRevealSkipButton = null;

        [SerializeField]
        private GameObject[] m_ModeObjects = null;

        [SerializeField]
        private TMP_Text[] m_ConfirmTexts = null;

        [SerializeField]
        private TMP_Text[] m_CancelTexts = null;

        [SerializeField]
        private TMP_Text[] m_OtherTexts = null;

        private int m_DialogMode = 1;
        private bool m_PauseGame = false;
        private object m_UserData = null;
        private GameFrameworkAction<object> m_OnClickConfirm = null;
        private GameFrameworkAction<object> m_OnClickCancel = null;
        private GameFrameworkAction<object> m_OnClickOther = null;

        private Coroutine m_MessageRevealCoroutine = null;

        private CommonButton m_MessageRevealSkipButtonSubscribed = null;

        private string m_MessageRevealFullTextPending = string.Empty;

        public int DialogMode
        {
            get
            {
                return m_DialogMode;
            }
        }

        public bool PauseGame
        {
            get
            {
                return m_PauseGame;
            }
        }

        public object UserData
        {
            get
            {
                return m_UserData;
            }
        }

        public void OnConfirmButtonClick()
        {
            Close();

            if (m_OnClickConfirm != null)
            {
                m_OnClickConfirm(m_UserData);
            }
        }

        public void OnCancelButtonClick()
        {
            Close();

            if (m_OnClickCancel != null)
            {
                m_OnClickCancel(m_UserData);
            }
        }

        public void OnOtherButtonClick()
        {
            Close();

            if (m_OnClickOther != null)
            {
                m_OnClickOther(m_UserData);
            }
        }


        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            DialogParams dialogParams = (DialogParams)userData;
            if (dialogParams == null)
            {
                Log.Warning("DialogParams is invalid.");
                return;
            }

            m_DialogMode = dialogParams.Mode;
            RefreshDialogMode();

            m_TitleText.text = dialogParams.Title;

            StopMessageReveal();
            string message = dialogParams.Message ?? string.Empty;
            bool reveal = m_MessageRevealByCharacter;
            if (dialogParams.MessageRevealByCharacter.HasValue)
            {
                reveal = dialogParams.MessageRevealByCharacter.Value;
            }

            m_MessageRevealFullTextPending = string.Empty;
            if (reveal && message.Length > 0 && m_MessageRevealSecondsPerChar > 0f)
            {
                m_MessageRevealFullTextPending = message;
                m_MessageRevealCoroutine = StartCoroutine(RevealMessageCo(message));
                SubscribeMessageRevealSkip();
            }
            else
            {
                m_MessageText.text = message;
                UnsubscribeMessageRevealSkip();
            }

            m_PauseGame = dialogParams.PauseGame;
            RefreshPauseGame();

            m_UserData = dialogParams.UserData;

            RefreshConfirmText(dialogParams.ConfirmText);
            m_OnClickConfirm = dialogParams.OnClickConfirm;

            RefreshCancelText(dialogParams.CancelText);
            m_OnClickCancel = dialogParams.OnClickCancel;

            RefreshOtherText(dialogParams.OtherText);
            m_OnClickOther = dialogParams.OnClickOther;
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            StopMessageReveal();
            UnsubscribeMessageRevealSkip();
            m_MessageRevealFullTextPending = string.Empty;

            if (m_PauseGame)
            {
                GameEntry.Base.ResumeGame();
            }

            m_DialogMode = 1;
            m_TitleText.text = string.Empty;
            m_MessageText.text = string.Empty;
            m_PauseGame = false;
            m_UserData = null;

            RefreshConfirmText(string.Empty);
            m_OnClickConfirm = null;

            RefreshCancelText(string.Empty);
            m_OnClickCancel = null;

            RefreshOtherText(string.Empty);
            m_OnClickOther = null;

            base.OnClose(isShutdown, userData);
        }

        private void RefreshDialogMode()
        {
            for (int i = 1; i <= m_ModeObjects.Length; i++)
            {
                m_ModeObjects[i - 1].SetActive(i == m_DialogMode);
            }
        }

        private void RefreshPauseGame()
        {
            if (m_PauseGame)
            {
                GameEntry.Base.PauseGame();
            }
        }

        private void RefreshConfirmText(string confirmText)
        {
            if (string.IsNullOrEmpty(confirmText))
            {
                confirmText = GameEntry.Localization.GetString("Dialog.ConfirmButton");
            }

            for (int i = 0; i < m_ConfirmTexts.Length; i++)
            {
                m_ConfirmTexts[i].text = confirmText;
            }
        }

        private void RefreshCancelText(string cancelText)
        {
            if (string.IsNullOrEmpty(cancelText))
            {
                cancelText = GameEntry.Localization.GetString("Dialog.CancelButton");
            }

            for (int i = 0; i < m_CancelTexts.Length; i++)
            {
                m_CancelTexts[i].text = cancelText;
            }
        }

        private void RefreshOtherText(string otherText)
        {
            if (string.IsNullOrEmpty(otherText))
            {
                otherText = GameEntry.Localization.GetString("Dialog.OtherButton");
            }

            for (int i = 0; i < m_OtherTexts.Length; i++)
            {
                m_OtherTexts[i].text = otherText;
            }
        }

        private void StopMessageReveal()
        {
            if (m_MessageRevealCoroutine != null)
            {
                StopCoroutine(m_MessageRevealCoroutine);
                m_MessageRevealCoroutine = null;
            }
        }

        private void SubscribeMessageRevealSkip()
        {
            UnsubscribeMessageRevealSkip();

            CommonButton btn = m_MessageRevealSkipButton;
            if (btn == null && m_MessageText != null)
            {
                btn = m_MessageText.GetComponent<CommonButton>();
            }

            if (btn == null)
            {
                return;
            }

            btn.m_OnClick.AddListener(OnMessageRevealSkipClick);
            m_MessageRevealSkipButtonSubscribed = btn;
        }

        private void UnsubscribeMessageRevealSkip()
        {
            if (m_MessageRevealSkipButtonSubscribed != null)
            {
                m_MessageRevealSkipButtonSubscribed.m_OnClick.RemoveListener(OnMessageRevealSkipClick);
                m_MessageRevealSkipButtonSubscribed = null;
            }
        }

        private void OnMessageRevealSkipClick()
        {
            if (m_MessageRevealCoroutine == null || string.IsNullOrEmpty(m_MessageRevealFullTextPending))
            {
                return;
            }

            StopMessageReveal();
            m_MessageText.text = m_MessageRevealFullTextPending;
            m_MessageRevealFullTextPending = string.Empty;
            UnsubscribeMessageRevealSkip();
        }

        private IEnumerator RevealMessageCo(string fullText)
        {
            m_MessageText.text = string.Empty;
            int len = fullText.Length;
            for (int i = 1; i <= len; i++)
            {
                m_MessageText.text = fullText.Substring(0, i);
                yield return new WaitForSecondsRealtime(m_MessageRevealSecondsPerChar);
            }

            m_MessageRevealCoroutine = null;
            m_MessageRevealFullTextPending = string.Empty;
            UnsubscribeMessageRevealSkip();
        }
    }

    /// <summary>
    /// 对话框显示数据。
    /// </summary>
    public class DialogParams
    {
        /// <summary>
        /// 模式，即按钮数量。取值 1、2、3。
        /// </summary>
        public int Mode
        {
            get;
            set;
        }

        /// <summary>
        /// 标题。
        /// </summary>
        public string Title
        {
            get;
            set;
        }

        /// <summary>
        /// 消息内容。
        /// </summary>
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// 是否为正文启用逐字显示。为 null 时使用对话框预制体上的默认设置。
        /// </summary>
        public bool? MessageRevealByCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// 弹出窗口时是否暂停游戏。
        /// </summary>
        public bool PauseGame
        {
            get;
            set;
        }

        /// <summary>
        /// 确认按钮文本。
        /// </summary>
        public string ConfirmText
        {
            get;
            set;
        }

        /// <summary>
        /// 确定按钮回调。
        /// </summary>
        public GameFrameworkAction<object> OnClickConfirm
        {
            get;
            set;
        }

        /// <summary>
        /// 取消按钮文本。
        /// </summary>
        public string CancelText
        {
            get;
            set;
        }

        /// <summary>
        /// 取消按钮回调。
        /// </summary>
        public GameFrameworkAction<object> OnClickCancel
        {
            get;
            set;
        }

        /// <summary>
        /// 中立按钮文本。
        /// </summary>
        public string OtherText
        {
            get;
            set;
        }

        /// <summary>
        /// 其它按钮回调。
        /// </summary>
        public GameFrameworkAction<object> OnClickOther
        {
            get;
            set;
        }

        /// <summary>
        /// 用户自定义数据。
        /// </summary>
        public string UserData
        {
            get;
            set;
        }
    }
}
