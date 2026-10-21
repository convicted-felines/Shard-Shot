using System;
using System.Collections.Generic;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    /// <summary>
    /// 流程基类：发起「切场景 + 切流程」请求（仅经 ProcedureChangeScene）。
    /// </summary>
    public abstract class ProcedureFlowBase : ProcedureBase
    {
        protected bool m_LeaveGame = false;
        protected ProcedureEnum m_toProcedureId = ProcedureEnum.菜单流程;
        protected string m_TargetSceneName = string.Empty;
        protected bool m_HasPendingFlowTransition = false;

        public const string WillOpenUIKey = "WillOpenUIKey";
        protected List<UIFormEnum> m_OpenUINextProcedure = new List<UIFormEnum>();

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_HasPendingFlowTransition = false;
            m_LeaveGame = false;
            m_OpenUINextProcedure.Clear();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_LeaveGame)
            {
                return;
            }

            if (!m_HasPendingFlowTransition)
            {
                return;
            }

            int openUiCount = m_OpenUINextProcedure.Count;
            int[] uiFormId = new int[openUiCount];
            for (int i = 0; i < openUiCount; i++)
            {
                uiFormId[i] = (int)m_OpenUINextProcedure[i];
            }

            procedureOwner.SetData<VarInt32Array>(WillOpenUIKey, uiFormId);
            procedureOwner.SetData<VarInt32>(ProcedureChangeScene.NextProcedureIdKey, (int)m_toProcedureId);
            procedureOwner.SetData<VarString>(ProcedureChangeScene.NextSceneNameKey, m_TargetSceneName);
            ChangeState<ProcedureChangeScene>(procedureOwner);
            m_LeaveGame = true;
        }

        public void StartGame(ProcedureEnum procedureEnum)
        {
            if (procedureEnum == ProcedureEnum.Unknown)
            {
                return;
            }

            m_toProcedureId = procedureEnum;
            m_HasPendingFlowTransition = true;
        }

        public void StartGame(ProcedureEnum procedureEnum, string sceneName)
        {
            if (procedureEnum == ProcedureEnum.Unknown)
            {
                return;
            }

            if (sceneName == null)
            {
                Log.Warning("Scene name is invalid.");
                return;
            }

            m_toProcedureId = procedureEnum;
            m_TargetSceneName = sceneName;
            m_HasPendingFlowTransition = true;
        }

        public void StartGame(ProcedureEnum procedureEnum, SceneEnum sceneEnum)
        {
            if (procedureEnum == ProcedureEnum.Unknown)
            {
                return;
            }

            DRScene scene = GameEntry.DataTable.GetDataRow<DRScene>((int)sceneEnum);
            if (scene == null)
            {
                Log.Warning("Scene '{0}' not found.", sceneEnum);
                return;
            }

            if (string.IsNullOrEmpty(scene.AssetName))
            {
                Log.Warning("Scene '{0}' can not resolve asset name.", sceneEnum);
                return;
            }

            m_toProcedureId = procedureEnum;
            m_TargetSceneName = scene.AssetName;
            m_HasPendingFlowTransition = true;
        }

        public void BackToMenu()
        {
            StartGame(ProcedureEnum.菜单流程, SceneEnum.菜单场景);
        }

        public void BackToMenu_andOpenUI(UIFormEnum[] ids)
        {
            m_OpenUINextProcedure.AddRange(ids);
            BackToMenu();
        }

        protected virtual void OpenUIFromData(ProcedureOwner procedureOwner)
        {
            VarInt32Array varData = procedureOwner.GetData<VarInt32Array>(WillOpenUIKey);
            if (varData == null)
            {
                return;
            }

            int[] uiFormId = varData;
            foreach (var id in uiFormId)
            {
                UIFormEnum uiId = (UIFormEnum)id;
                if (Enum.IsDefined(typeof(UIFormEnum), uiId))
                {
                    GameEntry.UI.OpenUIForm(uiId, this);
                }
            }

            procedureOwner.RemoveData(WillOpenUIKey);
        }
    }
}
