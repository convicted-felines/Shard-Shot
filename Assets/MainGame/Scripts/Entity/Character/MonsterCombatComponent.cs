using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 怪物战斗 AI：在攻击距离内自动锁定最近敌对目标并开火。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterCombatComponent : MonoBehaviour
    {
        private CharacterEntity m_Owner;
        private CharacterWeaponComponent m_WeaponComponent;
        private CharacterRangedAttackComponent m_RangedAttack;
        private CharacterMeleeAttackComponent m_MeleeAttack;

        public bool IsAttacking => IsMeleeWeapon()
            ? m_MeleeAttack != null && m_MeleeAttack.IsAttacking
            : m_RangedAttack != null && m_RangedAttack.IsAttacking;

        public void Initialize(
            CharacterEntity owner,
            CharacterWeaponComponent weaponComponent,
            CharacterRangedAttackComponent rangedAttack,
            CharacterMeleeAttackComponent meleeAttack)
        {
            m_Owner = owner;
            m_WeaponComponent = weaponComponent;
            m_RangedAttack = rangedAttack;
            m_MeleeAttack = meleeAttack;
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Owner == null || m_Owner.CharacterConfig == null)
            {
                return;
            }

            float attackRange = m_Owner.CharacterConfig.AttackRange;
            bool hasTarget = CharacterTargetingUtility.TryFindNearestHostile(
                m_Owner,
                attackRange,
                out CharacterEntity target);

            CharacterEntity facingTarget = hasTarget ? target : null;
            if (IsMeleeWeapon())
            {
                if (m_MeleeAttack == null)
                {
                    return;
                }

                m_MeleeAttack.Tick(
                    elapseSeconds,
                    wantsAttack: hasTarget,
                    interruptAttack: false,
                    facingTarget: facingTarget);
            }
            else
            {
                if (m_RangedAttack == null)
                {
                    return;
                }

                m_RangedAttack.Tick(
                    elapseSeconds,
                    wantsAttack: hasTarget,
                    interruptAttack: false,
                    facingTarget: facingTarget);
            }

            if (!hasTarget || IsAttacking)
            {
                return;
            }

            CharacterTargetingUtility.RotateToward(
                m_Owner,
                target.CachedTransform.position,
                m_Owner.CharacterConfig.TurnSpeed,
                elapseSeconds);
        }

        private bool IsMeleeWeapon()
        {
            DRWeapon weaponConfig = m_WeaponComponent != null ? m_WeaponComponent.CurrentWeaponConfig : null;
            return weaponConfig != null && weaponConfig.WeaponType == Constant.WeaponType.Melee;
        }

        private void OnDestroy()
        {
            m_Owner = null;
            m_WeaponComponent = null;
            m_RangedAttack = null;
            m_MeleeAttack = null;
        }
    }
}
