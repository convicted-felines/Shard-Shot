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
        private MonsterMovementComponent m_MovementComponent;
        private CharacterStunComponent m_StunComponent;
        private CharacterKnockbackComponent m_KnockbackComponent;
        private CharacterKnockdownComponent m_KnockdownComponent;
        private bool m_UseExplosionDeathForce;
        private Vector3 m_ExplosionDeathForceOrigin;
        private float m_ExplosionDeathScatterForce;

        public override CharacterFaction Faction => CharacterFaction.Enemy;

        public bool IsKnockedDown => m_KnockdownComponent != null && m_KnockdownComponent.IsKnockedDown;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            InitializeHealth();

            if (Rigidbody != null)
            {
                CharacterRigidbodyUtility.SetupTopDownMovement(Rigidbody);
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
            m_MovementComponent = null;
            if (m_StunComponent != null)
            {
                m_StunComponent.Clear();
                m_StunComponent = null;
            }

            if (m_KnockbackComponent != null)
            {
                m_KnockbackComponent.Clear();
                m_KnockbackComponent = null;
            }

            if (m_KnockdownComponent != null)
            {
                m_KnockdownComponent.Clear();
                m_KnockdownComponent = null;
            }

            ClearHealthState();
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (!IsAlive)
            {
                return;
            }

            m_KnockdownComponent?.Tick(elapseSeconds);
            if (IsKnockedDown)
            {
                return;
            }

            m_KnockbackComponent?.Tick(elapseSeconds);
            m_StunComponent?.Tick(elapseSeconds);
            if (m_StunComponent != null && m_StunComponent.IsStunned)
            {
                return;
            }

            m_MovementComponent?.Tick(elapseSeconds);
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
            MarkDead();

            if (TypeId == (int)CharacterEnum.骑士机器人)
            {
                PlayCharacterDeathEffect();
            }

            DeathRagdollSettings settings = GetDeathRagdollSettings();
            Vector3 forceOrigin = m_UseExplosionDeathForce
                ? m_ExplosionDeathForceOrigin
                : ResolveDeathForceOrigin(LastAttackerEntityId);
            float scatterForce = m_UseExplosionDeathForce
                ? m_ExplosionDeathScatterForce
                : settings.ScatterForce;
            m_UseExplosionDeathForce = false;

            if (m_KnockdownComponent != null
                && m_KnockdownComponent.TryConvertActiveRagdollToDeathScatter(forceOrigin, scatterForce, settings.HideDelay))
            {
                m_KnockdownComponent.ClearAfterDeathRagdollConversion();
                SetKnockdownPresentationActive(true);
                GameEntry.Entity.HideEntity(this);
                Log.Info("Monster character '{0}' destroyed (knockdown ragdoll scattered).", Name);
                return;
            }

            m_KnockdownComponent?.Clear();
            SetKnockdownPresentationActive(true);
            SpawnDeathRagdollAndHide(LastAttackerEntityId, forceOrigin, scatterForce);
            Log.Info("Monster character '{0}' destroyed.", Name);
        }

        /// <summary>榴弹爆炸：扣血；未击飞则进入布偶，布偶中则再次冲量，起身中则从当前姿态重新击飞；致死则死亡散架。</summary>
        public void ApplyExplosionHit(float damage, int attackerEntityId, Vector3 explosionCenter, float scatterForce)
        {
            if (!IsAlive || damage <= 0f)
            {
                return;
            }

            CharacterPrefabConfig config = PrefabConfig;
            if (scatterForce <= 0f)
            {
                scatterForce = config != null ? config.KnockdownScatterForce : 12f;
            }

            float deathScatterForce = scatterForce > 0f
                ? scatterForce
                : config != null ? config.DeathScatterForce : 12f;
            bool wasInRagdoll = m_KnockdownComponent != null && m_KnockdownComponent.IsInRagdollPhase;
            bool wasInRecovery = m_KnockdownComponent != null && m_KnockdownComponent.IsInRecoveryPhase;
            if (wasInRagdoll)
            {
                m_KnockdownComponent.SyncOwnerPositionWithActiveRagdoll();
            }

            m_UseExplosionDeathForce = true;
            m_ExplosionDeathForceOrigin = explosionCenter;
            m_ExplosionDeathScatterForce = deathScatterForce;
            TakeDamage(damage, attackerEntityId);

            if (!IsAlive)
            {
                m_UseExplosionDeathForce = false;
                return;
            }

            m_UseExplosionDeathForce = false;

            if (wasInRagdoll)
            {
                m_KnockdownComponent?.ApplyRagdollExplosionForce(explosionCenter, scatterForce);
                return;
            }

            if (wasInRecovery)
            {
                m_KnockdownComponent?.TryReenterKnockdownFromRecovery(attackerEntityId, scatterForce, explosionCenter);
                return;
            }

            TryEnterKnockdown(attackerEntityId, scatterForce, explosionCenter);
        }

        /// <summary>非致命击飞布偶（榴弹等调用）。</summary>
        public bool TryEnterKnockdown(int attackerEntityId, float scatterForce = 0f, Vector3? forceOriginWorld = null)
        {
            return IsAlive && m_KnockdownComponent != null
                && m_KnockdownComponent.TryEnterKnockdown(attackerEntityId, scatterForce, forceOriginWorld);
        }

        private void SetupCombat()
        {
            m_Animator = GetComponentInChildren<Animator>();
            m_StunComponent = CharacterStunComponent.Ensure(this, m_Animator);
            m_KnockbackComponent = CharacterKnockbackComponent.Ensure(this);
            m_KnockdownComponent = CharacterKnockdownComponent.Ensure(this, BuildMonsterKnockdownOptions());

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

            m_MovementComponent = GetComponent<MonsterMovementComponent>();
            if (m_MovementComponent == null)
            {
                m_MovementComponent = gameObject.AddComponent<MonsterMovementComponent>();
            }

            m_CombatComponent = GetComponent<MonsterCombatComponent>();
            if (m_CombatComponent == null)
            {
                m_CombatComponent = gameObject.AddComponent<MonsterCombatComponent>();
            }

            m_CombatComponent.Initialize(
                this,
                m_WeaponComponent,
                m_RangedAttackComponent,
                m_MeleeAttackComponent,
                m_MovementComponent);

            m_MovementComponent.Initialize(this, m_CombatComponent, m_Animator);
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
            if (CharacterTargetingUtility.TryFindNearestHostile(this, out CharacterEntity target))
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

        private static CharacterKnockdownComponent.Options BuildMonsterKnockdownOptions()
        {
            return new CharacterKnockdownComponent.Options
            {
                FollowCameraWithRagdoll = false,
                TransferWeaponToRagdoll = true,
                PostGetUpInvincibleDuration = 0f,
            };
        }
    }
}
