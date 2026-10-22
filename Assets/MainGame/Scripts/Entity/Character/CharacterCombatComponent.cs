using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色战斗组件：处理 Fire 输入、攻击间隔、攻击时机与自动转向。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCombatComponent : MonoBehaviour
    {
        private static readonly int GunShootHash = Animator.StringToHash("GunShoot");
        private static readonly int HowitzerShootHash = Animator.StringToHash("HowitzerShoot");
        private static readonly int SwingTheKnifeHash = Animator.StringToHash("SwingTheKnife");
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

        public void Initialize(CharacterEntity owner, CharacterWeaponComponent weaponComponent, Animator animator)
        {
            m_Owner = owner;
            m_WeaponComponent = weaponComponent;
            m_Animator = animator;
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Owner == null || m_WeaponComponent == null || GameEntry.Control == null)
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

            // 移动中不能开火；移动会立刻打断当前攻击并回到待机动画。
            if (IsMoveInputActive())
            {
                ResetAttackState();
                return;
            }

            bool fireHeld = GameEntry.Control.GetAction(GameEntry.Control.Controls.Gameplay.Fire);
            bool fireDown = GameEntry.Control.GetActionDown(GameEntry.Control.Controls.Gameplay.Fire);

            // 已进入攻击周期则继续跑完本轮（含 AttackTiming 出弹），松手不取消。
            if (!m_IsAttacking)
            {
                if (!fireHeld && !fireDown)
                {
                    return;
                }

                BeginAttackCycle(weaponConfig);
                if (!m_IsAttacking)
                {
                    return;
                }
            }

            // 仅开火攻击周期内自动朝向敌方；移动已在上方打断攻击，不会走到这里。
            UpdateFacing(elapseSeconds);

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

            // 本轮结束：仍按住则立即进入下一轮，否则停火。
            if (fireHeld)
            {
                BeginAttackCycle(weaponConfig);
            }
            else
            {
                ResetAttackState();
            }
        }

        private void BeginAttackCycle(DRWeapon weaponConfig)
        {
            m_AttackInterval = WeaponCombatUtility.GetAttackInterval(weaponConfig);
            m_AttackFireDelay = WeaponCombatUtility.GetAttackFireDelay(weaponConfig);
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
                case WeaponEnum.大刀:
                    return SwingTheKnifeHash;
                default:
                    return GunShootHash;
            }
        }

        private static bool IsMoveInputActive()
        {
            Vector2 moveInput = GameEntry.Control.GetMoveVector();
            return moveInput.sqrMagnitude > 0.01f;
        }

        private void UpdateFacing(float elapseSeconds)
        {
            if (m_Owner.CharacterConfig == null)
            {
                return;
            }

            float attackRange = m_Owner.CharacterConfig.AttackRange;
            if (!CharacterTargetingUtility.TryFindNearestHostile(m_Owner, attackRange, out CharacterEntity target))
            {
                return;
            }

            CharacterTargetingUtility.RotateToward(
                m_Owner,
                target.CachedTransform.position,
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
                ResetAttackStartTriggers();
                m_Animator.SetTrigger(AttackEndHash);
            }
        }

        private void ResetAttackStartTriggers()
        {
            if (m_Animator == null)
            {
                return;
            }

            m_Animator.ResetTrigger(GunShootHash);
            m_Animator.ResetTrigger(HowitzerShootHash);
            m_Animator.ResetTrigger(SwingTheKnifeHash);
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
