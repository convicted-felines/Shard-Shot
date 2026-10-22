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

        private CharacterData m_CharacterData;
        private DRCharacter m_DRCharacter;
        private Rigidbody m_Rigidbody;
        private CharacterPrefabConfig m_PrefabConfig;

        public CharacterData CharacterData => m_CharacterData;

        public DRCharacter CharacterConfig => m_DRCharacter;

        public Rigidbody Rigidbody => m_Rigidbody;

        public virtual CharacterFaction Faction => CharacterFaction.Neutral;

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

            Name = m_DRCharacter != null ? m_DRCharacter.Name : Utility.Text.Format("Character_{0}", Id);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_CharacterData = null;
            m_DRCharacter = null;
            m_Rigidbody = null;
            m_PrefabConfig = null;
            base.OnHide(isShutdown, userData);
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
        protected void SpawnDeathRagdollAndHide(int attackerEntityId = 0)
        {
            if (CharacterConfig == null || string.IsNullOrEmpty(CharacterConfig.RagdollName))
            {
                GameEntry.Entity.HideEntity(this);
                return;
            }

            DeathRagdollSettings settings = GetDeathRagdollSettings();
            string boneRootName = ResolveRagBoneRootName();
            RagdollBonePoseSnapshot bonePoseSnapshot = RagdollBonePoseSnapshot.CaptureFromHierarchy(CachedTransform, boneRootName);
            Vector3 velocity = Rigidbody != null ? Rigidbody.velocity : Vector3.zero;
            Vector3 forceOriginWorld = ResolveForceOrigin(attackerEntityId);
            RagdollEntityData ragdollData = RagdollEntityData.Create(
                CommonUtility.TemporaryId,
                CachedTransform.position,
                CachedTransform.rotation,
                velocity,
                boneRootName,
                bonePoseSnapshot,
                CharacterConfig.RagdollName,
                settings.DetachLimbs,
                forceOriginWorld,
                settings.ScatterForce,
                settings.HideDelay);
            GameEntry.Entity.ShowRagdoll(ragdollData);
            GameEntry.Entity.HideEntity(this);
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

        private string ResolveRagBoneRootName()
        {
            if (m_PrefabConfig != null && m_PrefabConfig.RagBoneRoot != null)
            {
                return m_PrefabConfig.RagBoneRoot.name;
            }

            return DefaultRagBoneRootName;
        }
    }
}
