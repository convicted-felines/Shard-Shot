using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    /// <summary>
    /// 游戏主流程：进入游戏场景后创建玩家角色，并由 <see cref="SpawnManager"/> 驱动刷怪。
    /// </summary>
    public class ProcedureGame : ProcedureFlowBase
    {
        private static readonly Vector3 PlayerSpawnPosition = Vector3.zero;
        private static readonly Vector3 CameraSpawnPosition = new Vector3(0f, 12f, -8f);
        private static readonly SpawnMonsterEntry[] s_SpawnMonsterEntries =
        {
            new SpawnMonsterEntry(CharacterEnum.骑士机器人, 6f),
            new SpawnMonsterEntry(CharacterEnum.熊怪, 1f),
        };

        private int m_PlayerEntityId;
        private int m_CameraEntityId;
        private SpawnManager m_SpawnManager;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GameEntry.ProgressObserver?.OnProcedureEnter(typeof(ProcedureGame));
            GameEntry.Control?.EnableAllGameControl();
            OpenUIFromData(procedureOwner);

            if (!GameEntry.UI.HasUIForm(UIFormEnum.主界面))
            {
                GameEntry.UI.OpenUIForm(UIFormEnum.主界面, this);
            }

            SpawnPlayer();
            SpawnCamera();
            m_SpawnManager = new SpawnManager(m_PlayerEntityId, s_SpawnMonsterEntries);
            m_SpawnManager.Reset();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            m_SpawnManager?.Update(elapseSeconds);
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Control?.BlockAllGameControl();
            GameEntry.Base.ResetNormalGameSpeed();

            UGuiForm settingForm = GameEntry.UI.GetUIForm(UIFormEnum.设置界面);
            if (settingForm != null)
            {
                settingForm.Close(true);
            }

            UGuiForm menuForm = GameEntry.UI.GetUIForm(UIFormEnum.主界面);
            if (menuForm != null)
            {
                menuForm.Close(true);
            }

            if (m_CameraEntityId != 0 && GameEntry.Entity.HasEntity(m_CameraEntityId))
            {
                GameEntry.Entity.HideEntity(m_CameraEntityId);
            }

            m_CameraEntityId = 0;

            if (m_PlayerEntityId != 0 && GameEntry.Entity.HasEntity(m_PlayerEntityId))
            {
                GameEntry.Entity.HideEntity(m_PlayerEntityId);
            }

            m_PlayerEntityId = 0;

            m_SpawnManager?.ClearAll();
            m_SpawnManager = null;
            GameEntry.Entity.HideAllLoadedRagdolls();

            GameEntry.Sound.RefreshBuiltInAudioListener();
            GameEntry.ProgressObserver?.OnProcedureLeave(typeof(ProcedureGame));
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void SpawnPlayer()
        {
            CharacterData data = CharacterData.Create(CharacterEnum.玩家角色, PlayerSpawnPosition, Quaternion.identity);
            m_PlayerEntityId = data.Id;
            GameEntry.Entity.ShowCharacter(data);
        }

        private void SpawnCamera()
        {
            AudioListener builtInListener = GameEntry.Sound.GetComponent<AudioListener>();
            if (builtInListener != null)
            {
                builtInListener.enabled = false;
            }

            TopDownFollowCameraData data = TopDownFollowCameraData.Create(CameraSpawnPosition, m_PlayerEntityId);
            m_CameraEntityId = data.Id;
            GameEntry.Entity.ShowTopDownFollowCamera(data);
        }
    }
}
