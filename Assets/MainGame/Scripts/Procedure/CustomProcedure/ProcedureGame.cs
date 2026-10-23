using System.Collections.Generic;
using UnityEngine;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace MainGame
{
    /// <summary>
    /// 游戏主流程：进入游戏场景后创建玩家角色，并间歇生成骑士敌人。
    /// </summary>
    public class ProcedureGame : ProcedureFlowBase
    {
        private const int MaxKnightCount = 15;
        private const float KnightSpawnMinRadius = 5f;
        private const float KnightSpawnRadius = 30f;
        private const float KnightSpawnIntervalMin = 3f;
        private const float KnightSpawnIntervalMax = 8f;

        private static readonly Vector3 PlayerSpawnPosition = Vector3.zero;
        private static readonly Vector3 CameraSpawnPosition = new Vector3(0f, 12f, -8f);

        private readonly List<int> m_KnightEntityIds = new List<int>(MaxKnightCount);

        private int m_PlayerEntityId;
        private int m_CameraEntityId;
        private float m_KnightSpawnTimer;

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

            m_KnightEntityIds.Clear();
            m_KnightSpawnTimer = KnightSpawnIntervalMin;
            SpawnPlayer();
            SpawnCamera();
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);
            UpdateKnightSpawner(elapseSeconds);
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

            HideAllKnights();

            GameEntry.Sound.RefreshBuiltInAudioListener();
            GameEntry.ProgressObserver?.OnProcedureLeave(typeof(ProcedureGame));
            base.OnLeave(procedureOwner, isShutdown);
        }

        private void UpdateKnightSpawner(float elapseSeconds)
        {
            m_KnightSpawnTimer -= elapseSeconds;
            if (m_KnightSpawnTimer > 0f)
            {
                return;
            }

            PruneInactiveKnights();
            if (m_KnightEntityIds.Count < MaxKnightCount)
            {
                SpawnKnight();
            }

            ScheduleNextKnightSpawn();
        }

        private void ScheduleNextKnightSpawn()
        {
            m_KnightSpawnTimer = Random.Range(KnightSpawnIntervalMin, KnightSpawnIntervalMax);
        }

        private void PruneInactiveKnights()
        {
            for (int i = m_KnightEntityIds.Count - 1; i >= 0; i--)
            {
                int entityId = m_KnightEntityIds[i];
                if (GameEntry.Entity.GetGameEntity(entityId) == null)
                {
                    m_KnightEntityIds.RemoveAt(i);
                }
            }
        }

        private void SpawnKnight()
        {
            Vector3 spawnCenter = PlayerSpawnPosition;
            CharacterEntity player = GameEntry.Entity.GetGameEntity(m_PlayerEntityId) as CharacterEntity;
            if (player != null)
            {
                spawnCenter = player.CachedTransform.position;
            }

            Vector2 randomDirection = Random.insideUnitCircle;
            if (randomDirection.sqrMagnitude <= 0.0001f)
            {
                randomDirection = Vector2.right;
            }
            else
            {
                randomDirection.Normalize();
            }

            float distance = Random.Range(KnightSpawnMinRadius, KnightSpawnRadius);
            Vector3 offset = new Vector3(randomDirection.x, 0f, randomDirection.y) * distance;
            Vector3 position = spawnCenter + offset;
            position.y = spawnCenter.y;
            CharacterData data = CharacterData.Create(CharacterEnum.骑士机器人, position, Quaternion.identity);
            m_KnightEntityIds.Add(data.Id);
            GameEntry.Entity.ShowCharacter(data);
        }

        private void HideAllKnights()
        {
            for (int i = 0; i < m_KnightEntityIds.Count; i++)
            {
                int entityId = m_KnightEntityIds[i];
                if (entityId != 0)
                {
                    GameEntry.Entity.HideEntity(entityId);
                }
            }

            m_KnightEntityIds.Clear();
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
