using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 主界面：菜单流程占位；游戏流程中作为 HUD，暂停按钮打开设置界面。
    /// </summary>
    public class MenuForm : UGuiForm
    {
        [SerializeField]
        private CommonButton m_PauseButton = null;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            ConfigureHudRaycasts();
            BindPauseButton();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            UnbindPauseButton();
            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// HUD 根节点不挡射线，但暂停按钮需忽略父级 CanvasGroup 才能单独接收点击。
        /// </summary>
        private void ConfigureHudRaycasts()
        {
            CanvasGroup rootGroup = GetComponent<CanvasGroup>();
            if (rootGroup != null)
            {
                rootGroup.blocksRaycasts = false;
            }

            EnsurePauseButtonResolved();
            if (m_PauseButton == null)
            {
                return;
            }

            Graphic graphic = m_PauseButton.GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
            }

            CanvasGroup pauseGroup = m_PauseButton.GetComponent<CanvasGroup>();
            if (pauseGroup != null)
            {
                pauseGroup.ignoreParentGroups = true;
                pauseGroup.blocksRaycasts = true;
                pauseGroup.interactable = true;
            }
        }

        private void EnsurePauseButtonResolved()
        {
            if (m_PauseButton != null)
            {
                return;
            }

            Transform pauseTransform = transform.Find("PauseButton");
            if (pauseTransform != null)
            {
                m_PauseButton = pauseTransform.GetComponent<CommonButton>();
            }
        }

        private void BindPauseButton()
        {
            EnsurePauseButtonResolved();
            if (m_PauseButton == null)
            {
                return;
            }

            m_PauseButton.m_OnClick.RemoveListener(OnClickPause);
            m_PauseButton.m_OnClick.AddListener(OnClickPause);
        }

        private void UnbindPauseButton()
        {
            if (m_PauseButton == null)
            {
                return;
            }

            m_PauseButton.m_OnClick.RemoveListener(OnClickPause);
        }

        public void OnClickPause()
        {
            if (!(GameEntry.Procedure.CurrentProcedure is ProcedureGame))
            {
                return;
            }

            if (GameEntry.Base.IsGamePaused || GameEntry.UI.HasUIForm(UIFormEnum.设置界面))
            {
                return;
            }

            GameEntry.Base.PauseGame();
            GameEntry.Control?.BlockAllGameControl();
            GameEntry.UI.OpenUIForm(UIFormEnum.设置界面, this);
        }
    }
}
