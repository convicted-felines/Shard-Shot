using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色实体逻辑。移动等玩法基于刚体，后续与物理碎片交互。
    /// </summary>
    public class CharacterEntity : Entity
    {
        private const string DefaultRagBoneRootName = "hips";

        private static readonly List<UnityGameFramework.Runtime.Entity> s_EntityBuffer =
            new List<UnityGameFramework.Runtime.Entity>(32);

        private CharacterData m_CharacterData;
        private DRCharacter m_DRCharacter;
        private Rigidbody m_Rigidbody;
        private CharacterPrefabConfig m_PrefabConfig;
        private Collider[] m_CachedColliders;
        private Animator m_CachedAnimator;
        private Renderer[] m_CachedRenderers;
        private bool[] m_CachedRendererEnabled;

        public CharacterData CharacterData => m_CharacterData;

        public DRCharacter CharacterConfig => m_DRCharacter;

        public Rigidbody Rigidbody => m_Rigidbody;

        public CharacterPrefabConfig PrefabConfig => m_PrefabConfig;

        public virtual CharacterFaction Faction => CharacterFaction.Neutral;

        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_CharacterData = userData as CharacterData;
            if (m_CharacterData == null)
            {
                Log.Error("CharacterEntity data is invalid.");
                return;
            }

            m_DRCharacter = GameEntry.DataTable.GetDataRow<DRCharacter>(TypeId);
            m_PrefabConfig = GetComponent<CharacterPrefabConfig>();
            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                m_Rigidbody = GetComponentInChildren<Rigidbody>();
            }

            m_CachedColliders = GetComponentsInChildren<Collider>(true);
            m_CachedAnimator = GetComponentInChildren<Animator>();
            if (m_CachedAnimator != null)
            {
                m_CachedAnimator.keepAnimatorStateOnDisable = true;
            }

            CacheRenderers();
            CharacterRigidbodyUtility.SyncRigidbodyToTransform(m_Rigidbody);

            Name = m_DRCharacter != null ? m_DRCharacter.Name : Utility.Text.Format("Character_{0}", Id);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            RestoreRenderersIfSuppressed();
            m_CharacterData = null;
            m_DRCharacter = null;
            m_Rigidbody = null;
            m_PrefabConfig = null;
            m_CachedColliders = null;
            m_CachedAnimator = null;
            m_CachedRenderers = null;
            m_CachedRendererEnabled = null;
            base.OnHide(isShutdown, userData);
        }

        /// <summary>
        /// 在已加载实体中查找与指定角色绑定的击飞/死亡布偶。
        /// </summary>
        public static RagdollEntity FindActiveRagdollEntityFor(CharacterEntity character)
        {
            if (character == null || GameEntry.Entity == null)
            {
                return null;
            }

            s_EntityBuffer.Clear();
            GameEntry.Entity.GetAllLoadedEntities(s_EntityBuffer);
            for (int i = 0; i < s_EntityBuffer.Count; i++)
            {
                UnityGameFramework.Runtime.Entity ugfEntity = s_EntityBuffer[i];
                if (ugfEntity == null)
                {
                    continue;
                }

                RagdollEntity ragdoll = ugfEntity.Logic as RagdollEntity;
                if (ragdoll != null && ragdoll.IsBoundToOwner(character.Id))
                {
                    return ragdoll;
                }
            }

            return null;
        }

        /// <summary>
        /// 世界血条位置：固定世界 Y，XZ 来自角色根；击飞布偶阶段取臀部关节。
        /// </summary>
        public bool TryGetWorldHealthBarPosition(float fixedWorldY, out Vector3 worldPosition)
        {
            Vector3 xzSource = CachedTransform.position;

            RagdollEntity ragdoll = FindActiveRagdollEntityFor(this);
            if (ragdoll != null && ragdoll.TryGetHipsWorldPosition(out Vector3 hipsWorld))
            {
                xzSource = hipsWorld;
            }

            worldPosition = new Vector3(xzSource.x, fixedWorldY, xzSource.z);
            return true;
        }

        /// <summary>
        /// 战斗判定与飘字用的世界坐标；击飞布偶阶段跟随布偶臀部推算的根节点位置。
        /// </summary>
        public bool TryGetCombatWorldPosition(out Vector3 worldPosition)
        {
            worldPosition = CachedTransform.position;

            RagdollEntity ragdoll = FindActiveRagdollEntityFor(this);
            if (ragdoll == null || !ragdoll.TryGetHipsWorldPosition(out Vector3 hipsWorld))
            {
                return true;
            }

            Vector3 hipLocalOffset = CharacterRagdollRecoveryUtility.ResolveHipLocalOffsetFromRoot(
                CachedTransform,
                m_PrefabConfig);
            worldPosition = hipsWorld - CachedTransform.rotation * hipLocalOffset;
            return true;
        }

        /// <summary>
        /// 击飞/起身用：设置世界位姿并清零刚体速度。
        /// </summary>
        public void SetCharacterWorldPose(Vector3 worldPosition, Quaternion worldRotation)
        {
            bool wasKinematic = m_Rigidbody != null && m_Rigidbody.isKinematic;
            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = true;
            }

            CachedTransform.SetPositionAndRotation(worldPosition, worldRotation);
            if (m_Rigidbody != null)
            {
                m_Rigidbody.position = worldPosition;
                m_Rigidbody.rotation = worldRotation;
                m_Rigidbody.isKinematic = wasKinematic;
                if (!wasKinematic)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                    m_Rigidbody.angularVelocity = Vector3.zero;
                }
            }
        }

        /// <summary>
        /// 击飞时隐藏角色渲染与物理，起身后恢复。
        /// 不走 HideEntity / Visible（避免 SetActive 打断实体轮询），以便倒计时后原地起身。
        /// </summary>
        public void SetKnockdownPresentationActive(bool active)
        {
            if (m_CachedAnimator != null)
            {
                m_CachedAnimator.enabled = active;
            }

            if (active)
            {
                RestoreRenderersIfSuppressed();
                EnableCharacterPhysics();
            }
            else
            {
                SuppressRenderers();
                DisableCharacterPhysics();
            }
        }

        private void CacheRenderers()
        {
            m_CachedRenderers = GetComponentsInChildren<Renderer>(true);
            m_CachedRendererEnabled = null;
        }

        private void SuppressRenderers()
        {
            if (m_CachedRenderers == null)
            {
                CacheRenderers();
            }

            if (m_CachedRenderers == null)
            {
                return;
            }

            if (m_CachedRendererEnabled == null || m_CachedRendererEnabled.Length != m_CachedRenderers.Length)
            {
                m_CachedRendererEnabled = new bool[m_CachedRenderers.Length];
            }

            for (int i = 0; i < m_CachedRenderers.Length; i++)
            {
                Renderer renderer = m_CachedRenderers[i];
                if (renderer == null)
                {
                    m_CachedRendererEnabled[i] = false;
                    continue;
                }

                m_CachedRendererEnabled[i] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        private void RestoreRenderersIfSuppressed()
        {
            if (m_CachedRenderers == null || m_CachedRendererEnabled == null)
            {
                return;
            }

            for (int i = 0; i < m_CachedRenderers.Length; i++)
            {
                Renderer renderer = m_CachedRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                renderer.enabled = m_CachedRendererEnabled[i];
            }

            m_CachedRendererEnabled = null;
        }

        protected void DisableCharacterPhysics()
        {
            if (m_Rigidbody != null)
            {
                if (!m_Rigidbody.isKinematic)
                {
                    m_Rigidbody.velocity = Vector3.zero;
                    m_Rigidbody.angularVelocity = Vector3.zero;
                }

                m_Rigidbody.isKinematic = true;
            }

            SetCollidersEnabled(false);
        }

        protected void EnableCharacterPhysics()
        {
            SetCollidersEnabled(true);

            if (m_Rigidbody != null)
            {
                m_Rigidbody.isKinematic = false;
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }
        }

        private void SetCollidersEnabled(bool enabled)
        {
            if (m_CachedColliders == null)
            {
                return;
            }

            for (int i = 0; i < m_CachedColliders.Length; i++)
            {
                Collider collider = m_CachedColliders[i];
                if (collider != null)
                {
                    collider.enabled = enabled;
                }
            }
        }

        protected struct DeathRagdollSettings
        {
            public bool DetachLimbs;
            public float HideDelay;
            public float ScatterForce;
        }

        /// <summary>
        /// 死亡时生成布偶并隐藏角色。若未配置布偶资源则直接隐藏。
        /// </summary>
        /// <returns>布偶实体 Id；未生成布偶时返回 0。</returns>
        protected int SpawnDeathRagdollAndHide(int attackerEntityId = 0)
        {
            DeathRagdollSettings settings = GetDeathRagdollSettings();
            return SpawnDeathRagdollAndHide(attackerEntityId, ResolveForceOrigin(attackerEntityId), settings.ScatterForce);
        }

        protected int SpawnDeathRagdollAndHide(int attackerEntityId, Vector3 forceOriginWorld, float scatterForce)
        {
            if (CharacterConfig == null || string.IsNullOrEmpty(CharacterConfig.RagdollName))
            {
                GameEntry.Entity.HideEntity(this);
                return 0;
            }

            DeathRagdollSettings settings = GetDeathRagdollSettings();
            int ragdollEntityId = SpawnRagdollInternal(
                scatterForce,
                settings.DetachLimbs,
                settings.HideDelay,
                ownerEntityId: 0,
                forceOriginWorld);
            GameEntry.Entity.HideEntity(this);
            return ragdollEntityId;
        }

        /// <summary>
        /// 击飞时生成可恢复布偶（不隐藏角色实体，由表现层 Visible 控制）。
        /// </summary>
        public int SpawnKnockdownRagdoll(int attackerEntityId, float scatterForce, Vector3? forceOriginWorld = null)
        {
            if (CharacterConfig == null || string.IsNullOrEmpty(CharacterConfig.RagdollName))
            {
                return 0;
            }

            return SpawnRagdollInternal(
                scatterForce,
                detachLimbs: false,
                hideDelay: 0f,
                ownerEntityId: Id,
                forceOriginWorld ?? ResolveForceOrigin(attackerEntityId));
        }

        private int SpawnRagdollInternal(
            float scatterForce,
            bool detachLimbs,
            float hideDelay,
            int ownerEntityId,
            Vector3 forceOriginWorld)
        {
            string boneRootName = ResolveRagBoneRootName();
            RagdollBonePoseSnapshot bonePoseSnapshot = RagdollBonePoseSnapshot.CaptureFromHierarchy(CachedTransform, boneRootName);
            Vector3 velocity = Rigidbody != null ? Rigidbody.velocity : Vector3.zero;
            RagdollEntityData ragdollData = RagdollEntityData.Create(
                CommonUtility.TemporaryId,
                CachedTransform.position,
                CachedTransform.rotation,
                velocity,
                boneRootName,
                bonePoseSnapshot,
                CharacterConfig.RagdollName,
                detachLimbs,
                forceOriginWorld,
                scatterForce,
                hideDelay,
                ownerEntityId);
            int ragdollEntityId = ragdollData.Id;
            GameEntry.Entity.ShowRagdoll(ragdollData);
            return ragdollEntityId;
        }

        protected virtual DeathRagdollSettings GetDeathRagdollSettings()
        {
            return new DeathRagdollSettings
            {
                DetachLimbs = m_PrefabConfig != null && m_PrefabConfig.DetachLimbsOnDeath,
                HideDelay = m_PrefabConfig != null ? m_PrefabConfig.DeathRagdollHideDelay : 3f,
                ScatterForce = m_PrefabConfig != null ? m_PrefabConfig.DeathScatterForce : 12f,
            };
        }

        protected Vector3 ResolveDeathForceOrigin(int attackerEntityId)
        {
            return ResolveForceOrigin(attackerEntityId);
        }

        private Vector3 ResolveForceOrigin(int attackerEntityId)
        {
            if (attackerEntityId != 0)
            {
                Entity attacker = GameEntry.Entity.GetGameEntity(attackerEntityId);
                if (attacker != null)
                {
                    return attacker.CachedTransform.position;
                }
            }

            return CachedTransform.position - CachedTransform.forward;
        }

        public string GetRagBoneRootName()
        {
            return ResolveRagBoneRootName();
        }

        /// <summary>采样起身动画第一帧骨骼姿态（用于布偶→动画的拟合目标）。</summary>
        public virtual RagdollBonePoseSnapshot SampleGetUpAnimationFirstFrame(string getUpAnimatorStateName, string boneRootName)
        {
            if (m_CachedAnimator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return new RagdollBonePoseSnapshot();
            }

            m_CachedAnimator.SetFloat(SpeedParamHash, 0f);
            m_CachedAnimator.speed = 1f;
            m_CachedAnimator.enabled = true;
            m_CachedAnimator.Play(getUpAnimatorStateName, 0, 0f);
            m_CachedAnimator.Update(0f);
            return RagdollBonePoseSnapshot.CaptureFromHierarchy(CachedTransform, boneRootName);
        }

        /// <summary>骨骼拟合期间关闭 Animator，避免与手动写骨骼冲突。</summary>
        public virtual void SetAnimatorEnabledForPoseBlend(bool enabled)
        {
            if (m_CachedAnimator != null)
            {
                m_CachedAnimator.enabled = enabled;
            }
        }

        /// <summary>布偶起身后播放趴起身/坐起身状态（骨骼已对齐第 0 帧时仅接管 Animator）。</summary>
        public virtual void BeginGetUpAnimationPlayback(string getUpAnimatorStateName)
        {
            if (m_CachedAnimator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return;
            }

            m_CachedAnimator.SetFloat(SpeedParamHash, 0f);
            m_CachedAnimator.speed = 1f;
            m_CachedAnimator.enabled = true;
            m_CachedAnimator.Play(getUpAnimatorStateName, 0, 0f);
            m_CachedAnimator.Update(0f);
        }

        /// <summary>起身动画是否已结束（离开起身状态或 normalizedTime ≥ 1）。</summary>
        public virtual bool IsGetUpAnimationComplete(bool playbackStarted, string getUpAnimatorStateName)
        {
            if (!playbackStarted || m_CachedAnimator == null || string.IsNullOrEmpty(getUpAnimatorStateName))
            {
                return false;
            }

            int getUpStateHash = Animator.StringToHash(getUpAnimatorStateName);
            AnimatorStateInfo state = m_CachedAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == getUpStateHash)
            {
                return state.normalizedTime >= 1f;
            }

            return true;
        }

        protected string ResolveRagBoneRootName()
        {
            if (m_PrefabConfig != null && m_PrefabConfig.RagBoneRoot != null)
            {
                return m_PrefabConfig.RagBoneRoot.name;
            }

            return DefaultRagBoneRootName;
        }
    }
}
