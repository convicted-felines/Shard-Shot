using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    /// <summary>
    /// 启动完成后的默认流程：打开主菜单 UI，供新项目扩展。
    /// </summary>
    public class ProcedureMenu : ProcedureFlowBase
    {
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GameEntry.ProgressObserver?.OnProcedureEnter(typeof(ProcedureMenu));
            OpenUIFromData(procedureOwner);

            if (!GameEntry.UI.HasUIForm(UIFormEnum.主界面))
            {
                GameEntry.UI.OpenUIForm(UIFormEnum.主界面, this);
            }
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            UGuiForm menuForm = GameEntry.UI.GetUIForm(UIFormEnum.主界面);
            if (menuForm != null)
            {
                menuForm.Close(true);
            }

            GameEntry.ProgressObserver?.OnProcedureLeave(typeof(ProcedureMenu));
            base.OnLeave(procedureOwner, isShutdown);
        }
    }
}
