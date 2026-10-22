using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 怪物角色：自动索敌、远程攻击、可死亡散架。
    /// </summary>
    public class MonsterCharacterEntity : DamageableCharacterEntity
    {
        [SerializeField]
        private WeaponEnum m_DefaultWeapon = WeaponEnum.加特林;

        private Animator m_Animator;
        private CharacterWeaponComponent m_WeaponComponent;
        private CharacterRangedAttackComponent m_RangedAttackComponent;
        private CharacterMeleeAttackComponent m_MeleeAttackComponent;
        private MonsterCombatComponent m_CombatComponent;

        public override CharacterFaction Faction => CharacterFaction.Enemy;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            InitializeHealth();

            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = true;
                Rigidbody.useGravity = false;
                Rigidbody.velocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }

            SetupDamageReceivers(this, this);
            SetupCombat();
            EquipDefaultWeapon();
            FaceNearestHostileOrOrigin();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (m_WeaponComponent != null)
            {
                m_WeaponComponent.ClearCurrentWeapon();
            }

            m_Animator = null;
            m_WeaponComponent = null;
            m_RangedAttackComponent = null;
            m_MeleeAttackComponent = null;
            m_CombatComponent = null;
            ClearHealthState();
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            m_CombatComponent?.Tick(elapseSeconds);
        }

        protected override DeathRagdollSettings GetDeathRagdollSettings()
        {
            DeathRagdollSettings settings = base.GetDeathRagdollSettings();
            settings.DetachLimbs = true;
            return settings;
        }

        protected override void OnDead()
        {
            Log.Info("Monster character '{0}' destroyed.", Name);
            base.OnDead();
        }

        private void SetupCombat()
        {
            m_Animator = GetComponentInChildren<Animator>();

            m_WeaponComponent = GetComponent<CharacterWeaponComponent>();
            if (m_WeaponComponent == null)
            {
                m_WeaponComponent = gameObject.AddComponent<CharacterWeaponComponent>();
            }

            m_WeaponComponent.Initialize(this, GetComponent<CharacterPrefabConfig>());

            m_RangedAttackComponent = GetComponent<CharacterRangedAttackComponent>();
            if (m_RangedAttackComponent == null)
            {
                m_RangedAttackComponent = gameObject.AddComponent<CharacterRangedAttackComponent>();
            }

            m_RangedAttackComponent.Initialize(this, m_WeaponComponent, m_Animator);

            m_MeleeAttackComponent = GetComponent<CharacterMeleeAttackComponent>();
            if (m_MeleeAttackComponent == null)
            {
                m_MeleeAttackComponent = gameObject.AddComponent<CharacterMeleeAttackComponent>();
            }

            m_MeleeAttackComponent.Initialize(this, m_WeaponComponent, m_Animator);

            m_CombatComponent = GetComponent<MonsterCombatComponent>();
            if (m_CombatComponent == null)
            {
                m_CombatComponent = gameObject.AddComponent<MonsterCombatComponent>();
            }

            m_CombatComponent.Initialize(this, m_WeaponComponent, m_RangedAttackComponent, m_MeleeAttackComponent);
        }

        private void EquipDefaultWeapon()
        {
            WeaponEnum weapon = m_DefaultWeapon;
            CharacterPrefabConfig config = GetComponent<CharacterPrefabConfig>();
            if (config != null && config.DefaultWeapon != WeaponEnum.Unknown)
            {
                weapon = config.DefaultWeapon;
            }

            m_WeaponComponent.EquipWeapon(weapon);
        }

        private void FaceNearestHostileOrOrigin()
        {
            if (CharacterConfig != null
                && CharacterTargetingUtility.TryFindNearestHostile(this, CharacterConfig.AttackRange, out CharacterEntity target))
            {
                FacePosition(target.CachedTransform.position);
                return;
            }

            FacePosition(Vector3.zero);
        }

        private void FacePosition(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - CachedTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            CachedTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
