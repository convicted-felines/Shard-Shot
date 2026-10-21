using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 主菜单界面（占位）：进入菜单流程时打开。
    /// </summary>
    public class MenuForm : UGuiForm
    {
        [SerializeField]
        private CommonButton m_PlaceholderButton = null;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            if (m_PlaceholderButton != null)
            {
                m_PlaceholderButton.m_OnClick.RemoveListener(OnClickPlaceholder);
                m_PlaceholderButton.m_OnClick.AddListener(OnClickPlaceholder);
            }
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            if (m_PlaceholderButton != null)
            {
                m_PlaceholderButton.m_OnClick.RemoveListener(OnClickPlaceholder);
            }

            base.OnClose(isShutdown, userData);
        }

        /// <summary>
        /// 占位按钮点击：后续可改为开始游戏等逻辑。
        /// </summary>
        public void OnClickPlaceholder()
        {
            Log.Info("MenuForm placeholder button clicked.");
        }
    }
}
