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
        /// <param name="facingWorldPosition">攻击期间朝向的世界坐标；优先于 facingTarget。</param>
        public void Tick(
            float elapseSeconds,
            bool wantsAttack,
            bool interruptAttack,
            CharacterEntity facingTarget,
            Vector3? facingWorldPosition = null)
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

            TickGatlingDecay(rangedWeapon, elapseSeconds);

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

            UpdateFacing(facingTarget, facingWorldPosition, elapseSeconds);

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

        private static void TickGatlingDecay(RangedWeaponEntity rangedWeapon, float elapseSeconds)
        {
            if (rangedWeapon is GatlingWeaponEntity gatling)
            {
                gatling.TickDecay(elapseSeconds);
            }
        }

        private void UpdateFacing(CharacterEntity facingTarget, Vector3? facingWorldPosition, float elapseSeconds)
        {
            if (m_Owner.CharacterConfig == null)
            {
                return;
            }

            Vector3 targetPosition;
            if (facingWorldPosition.HasValue)
            {
                targetPosition = facingWorldPosition.Value;
            }
            else if (facingTarget != null)
            {
                targetPosition = facingTarget.CachedTransform.position;
            }
            else
            {
                return;
            }

            CharacterTargetingUtility.RotateToward(
                m_Owner,
                targetPosition,
                m_Owner.CharacterConfig.TurnSpeed,
                elapseSeconds);
        }

        private void ResetAttackState()
        {
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
