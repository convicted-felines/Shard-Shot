using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    /// <summary>
    /// 游戏主流程：进入游戏场景后创建玩家角色。
    /// </summary>
    public class ProcedureGame : ProcedureFlowBase
    {
        private static readonly Vector3 PlayerSpawnPosition = Vector3.zero;
        private static readonly Vector3 TargetSpawnPosition = new Vector3(10f, 0f, 0f);
        private static readonly Vector3 CameraSpawnPosition = new Vector3(0f, 12f, -8f);

        private int m_PlayerEntityId;
        private int m_TargetEntityId;
        private int m_CameraEntityId;

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            GameEntry.ProgressObserver?.OnProcedureEnter(typeof(ProcedureGame));
            GameEntry.Control?.EnableAllGameControl();
            OpenUIFromData(procedureOwner);
            SpawnPlayer();
            SpawnTarget();
            SpawnCamera();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Control?.BlockAllGameControl();

            if (m_CameraEntityId != 0)
            {
                GameEntry.Entity.HideEntity(m_CameraEntityId);
                m_CameraEntityId = 0;
            }

            if (m_PlayerEntityId != 0)
            {
                GameEntry.Entity.HideEntity(m_PlayerEntityId);
                m_PlayerEntityId = 0;
            }

            if (m_TargetEntityId != 0)
            {
                GameEntry.Entity.HideEntity(m_TargetEntityId);
                m_TargetEntityId = 0;
            }

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

        private void SpawnTarget()
        {
            CharacterData data = CharacterData.Create(CharacterEnum.骑士机器人, TargetSpawnPosition, Quaternion.identity);
            m_TargetEntityId = data.Id;
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
