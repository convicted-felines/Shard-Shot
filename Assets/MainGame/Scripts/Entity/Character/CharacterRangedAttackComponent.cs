using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 共享远程攻击周期：攻击间隔、开火时机、动画触发。由玩家输入组件与怪物 AI 组件共同驱动。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterRangedAttackComponent : MonoBehaviour
    {
        private static readonly int GunShootHash = Animator.StringToHash("GunShoot");
        private static readonly int HowitzerShootHash = Animator.StringToHash("HowitzerShoot");
        private static readonly int AttackEndHash = Animator.StringToHash("AttackEnd");

        private CharacterEntity m_Owner;
        private CharacterWeaponComponent m_WeaponComponent;
        private Animator m_Animator;

        private bool m_IsAttacking;
        private bool m_HasFiredThisCycle;
        private float m_AttackElapsed;
        private float m_AttackInterval;
        private float m_AttackFireDelay;

        public bool IsAttacking => m_IsAttacking;

        public void InterruptAttack()
        {
            ResetAttackState();
        }

        public void Initialize(CharacterEntity owner, CharacterWeaponComponent weaponComponent, Animator animator)
        {
            m_Owner = owner;
            m_WeaponComponent = weaponComponent;
            m_Animator = animator;
        }

        /// <param name="wantsAttack">是否希望开始或持续攻击。</param>
        /// <param name="interruptAttack">是否打断当前攻击（如移动、失去目标）。</param>
        /// <param name="facingTarget">攻击期间朝向的目标，可为 null。</param>
        public void Tick(float elapseSeconds, bool wantsAttack, bool interruptAttack, CharacterEntity facingTarget)
        {
            if (m_Owner == null || m_WeaponComponent == null)
            {
                return;
            }

            RangedWeaponEntity rangedWeapon = m_WeaponComponent.CurrentWeaponEntity as RangedWeaponEntity;
            DRWeapon weaponConfig = m_WeaponComponent.CurrentWeaponConfig;
            if (rangedWeapon == null || weaponConfig == null || string.IsNullOrEmpty(weaponConfig.BulletAssetName))
            {
                ResetAttackState();
                return;
            }

            if (interruptAttack)
            {
                ResetAttackState();
                return;
            }

            if (!m_IsAttacking)
            {
                if (!wantsAttack)
                {
                    return;
                }

                BeginAttackCycle(rangedWeapon, weaponConfig);
                if (!m_IsAttacking)
                {
                    return;
                }
            }

            UpdateFacing(facingTarget, elapseSeconds);
            TickGatlingRamp(rangedWeapon, elapseSeconds, wantsAttack, interruptAttack);

            m_AttackElapsed += elapseSeconds;
            if (!m_HasFiredThisCycle && m_AttackElapsed >= m_AttackFireDelay)
            {
                rangedWeapon.FireBullet();
                m_HasFiredThisCycle = true;
            }

            if (m_AttackElapsed < m_AttackInterval)
            {
                return;
            }

            if (wantsAttack)
            {
                BeginAttackCycle(rangedWeapon, weaponConfig);
            }
            else
            {
                ResetAttackState();
            }
        }

        private void BeginAttackCycle(RangedWeaponEntity rangedWeapon, DRWeapon weaponConfig)
        {
            float attackSpeed = GetEffectiveAttackSpeed(rangedWeapon, weaponConfig);
            m_AttackInterval = WeaponCombatUtility.GetAttackInterval(weaponConfig, attackSpeed);
            m_AttackFireDelay = WeaponCombatUtility.GetAttackFireDelay(weaponConfig, attackSpeed);
            if (m_AttackInterval <= 0f || float.IsInfinity(m_AttackInterval))
            {
                return;
            }

            m_IsAttacking = true;
            m_HasFiredThisCycle = false;
            m_AttackElapsed = 0f;
            m_Animator?.SetTrigger(GetAttackStartTriggerHash(weaponConfig));
        }

        private static int GetAttackStartTriggerHash(DRWeapon weaponConfig)
        {
            if (weaponConfig == null)
            {
                return GunShootHash;
            }

            switch ((WeaponEnum)weaponConfig.Id)
            {
                case WeaponEnum.榴弹炮:
                    return HowitzerShootHash;
                default:
                    return GunShootHash;
            }
        }

        private static float GetEffectiveAttackSpeed(RangedWeaponEntity rangedWeapon, DRWeapon weaponConfig)
        {
            if (rangedWeapon is GatlingWeaponEntity gatling)
            {
                float speed = gatling.GetEffectiveAttackSpeed();
                if (speed > 0f)
                {
                    return speed;
                }
            }

            return weaponConfig.AttackSpeed;
        }

        private void TickGatlingRamp(
            RangedWeaponEntity rangedWeapon,
            float elapseSeconds,
            bool wantsAttack,
            bool interruptAttack)
        {
            if (rangedWeapon is GatlingWeaponEntity gatling)
            {
                bool isStationaryAttacking = m_IsAttacking && wantsAttack && !interruptAttack;
                gatling.TickRamp(elapseSeconds, isStationaryAttacking);
            }
        }

        private void ResetGatlingRamp()
        {
            if (m_WeaponComponent?.CurrentWeaponEntity is GatlingWeaponEntity gatling)
            {
                gatling.ResetRamp();
            }
        }

        private void UpdateFacing(CharacterEntity facingTarget, float elapseSeconds)
        {
            if (facingTarget == null || m_Owner.CharacterConfig == null)
            {
                return;
            }

            CharacterTargetingUtility.RotateToward(
                m_Owner,
                facingTarget.CachedTransform.position,
                m_Owner.CharacterConfig.TurnSpeed,
                elapseSeconds);
        }

        private void ResetAttackState()
        {
            ResetGatlingRamp();

            bool wasAttacking = m_IsAttacking;
            m_IsAttacking = false;
            m_HasFiredThisCycle = false;
            m_AttackElapsed = 0f;
            m_AttackInterval = 0f;
            m_AttackFireDelay = 0f;

            if (wasAttacking && m_Animator != null)
            {
                m_Animator.ResetTrigger(GunShootHash);
                m_Animator.ResetTrigger(HowitzerShootHash);
                m_Animator.SetTrigger(AttackEndHash);
            }
        }

        private void OnDestroy()
        {
            ResetAttackState();
            m_Owner = null;
            m_WeaponComponent = null;
            m_Animator = null;
        }
    }
}
