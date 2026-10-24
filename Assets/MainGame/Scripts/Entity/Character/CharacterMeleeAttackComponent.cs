using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 共享近战攻击周期：动画播放速度对齐攻击间隔，伤害由动画事件 DamageApply 触发。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterMeleeAttackComponent : MonoBehaviour
    {
        private const string DamageApplyEventName = "DamageApply";
        private static readonly int SwingTheKnifeHash = Animator.StringToHash("SwingTheKnife");
        private static readonly int AttackEndHash = Animator.StringToHash("AttackEnd");

        private CharacterEntity m_Owner;
        private CharacterWeaponComponent m_WeaponComponent;
        private Animator m_Animator;

        private bool m_IsAttacking;
        private bool m_HasAppliedDamageThisCycle;
        private float m_AttackElapsed;
        private float m_AttackInterval;
        private float m_CachedMeleeClipLength;
        private float m_DefaultAnimatorSpeed = 1f;

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
            m_CachedMeleeClipLength = 0f;

            if (m_Animator != null)
            {
                m_DefaultAnimatorSpeed = m_Animator.speed > 0f ? m_Animator.speed : 1f;
                EnsureAnimationEventRelay();
            }
        }

        public void Tick(float elapseSeconds, bool wantsAttack, bool interruptAttack, CharacterEntity facingTarget)
        {
            if (m_Owner == null || m_WeaponComponent == null)
            {
                return;
            }

            MeleeWeaponEntity meleeWeapon = m_WeaponComponent.CurrentWeaponEntity as MeleeWeaponEntity;
            DRWeapon weaponConfig = m_WeaponComponent.CurrentWeaponConfig;
            if (meleeWeapon == null || weaponConfig == null || weaponConfig.WeaponType != Constant.WeaponType.Melee)
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

                BeginAttackCycle(weaponConfig);
                if (!m_IsAttacking)
                {
                    return;
                }
            }

            UpdateFacing(facingTarget, elapseSeconds);

            m_AttackElapsed += elapseSeconds;
            if (m_AttackElapsed < m_AttackInterval)
            {
                return;
            }

            if (wantsAttack)
            {
                BeginAttackCycle(weaponConfig);
            }
            else
            {
                ResetAttackState();
            }
        }

        /// <summary>由动画事件 DamageApply 调用，在攻击距离内对最近敌对目标造成伤害。</summary>
        public void OnDamageApply()
        {
            if (!m_IsAttacking || m_HasAppliedDamageThisCycle || m_Owner == null || m_WeaponComponent == null)
            {
                return;
            }

            DRWeapon weaponConfig = m_WeaponComponent.CurrentWeaponConfig;
            if (weaponConfig == null || weaponConfig.WeaponType != Constant.WeaponType.Melee)
            {
                return;
            }

            float attackRange = m_Owner.CharacterConfig != null ? m_Owner.CharacterConfig.AttackRange : 0f;
            if (attackRange <= 0f)
            {
                return;
            }

            if (!CharacterTargetingUtility.TryFindNearestHostile(m_Owner, attackRange, out CharacterEntity target))
            {
                return;
            }

            IDamageable damageable = target as IDamageable;
            if (damageable == null || !damageable.IsAlive)
            {
                return;
            }

            PlayerCharacterEntity playerTarget = target as PlayerCharacterEntity;
            WeaponEnum weaponEnum = (WeaponEnum)weaponConfig.Id;
            bool shouldPlayHitFeedback = playerTarget != null
                && playerTarget.IsAlive
                && !playerTarget.IsInvincible;

            m_HasAppliedDamageThisCycle = true;
            damageable.TakeDamage(weaponConfig.Damage, m_Owner.Id);

            if (shouldPlayHitFeedback
                && playerTarget.TryGetCombatWorldPosition(out Vector3 hitPosition))
            {
                PlayMeleeHitFeedback(weaponEnum, hitPosition);
            }
        }

        private static void PlayMeleeHitFeedback(WeaponEnum weaponEnum, Vector3 hitPosition)
        {
            switch (weaponEnum)
            {
                case WeaponEnum.骑士剑:
                    GameEntry.Juice?.PlayEffectAtPrefabRotation(EffectEnum.骑士剑击中, hitPosition);
                    GameEntry.Sound.PlaySound(SoundEnum.骑士剑击中, hitPosition);
                    break;
                case WeaponEnum.大斧头:
                    GameEntry.Juice?.PlayEffectAtPrefabRotation(EffectEnum.大斧头击中, hitPosition);
                    GameEntry.Sound.PlaySound(SoundEnum.大斧头击中, hitPosition);
                    break;
            }
        }

        private void BeginAttackCycle(DRWeapon weaponConfig)
        {
            m_AttackInterval = WeaponCombatUtility.GetAttackInterval(weaponConfig);
            if (m_AttackInterval <= 0f || float.IsInfinity(m_AttackInterval))
            {
                return;
            }

            m_IsAttacking = true;
            m_HasAppliedDamageThisCycle = false;
            m_AttackElapsed = 0f;
            PlayMeleeSwingSound((WeaponEnum)weaponConfig.Id);
            ApplyAttackAnimatorSpeed();
            if (m_Animator != null)
            {
                m_Animator.ResetTrigger(AttackEndHash);
                m_Animator.SetTrigger(SwingTheKnifeHash);
            }
        }

        private void PlayMeleeSwingSound(WeaponEnum weaponEnum)
        {
            if (m_Owner == null)
            {
                return;
            }

            switch (weaponEnum)
            {
                case WeaponEnum.骑士剑:
                    GameEntry.Sound.PlaySound(SoundEnum.挥动骑士剑, m_Owner);
                    break;
                case WeaponEnum.大斧头:
                    GameEntry.Sound.PlaySound(SoundEnum.挥动斧头, m_Owner);
                    break;
            }
        }

        private void ApplyAttackAnimatorSpeed()
        {
            if (m_Animator == null)
            {
                return;
            }

            float clipLength = GetMeleeClipLength();
            m_Animator.speed = clipLength / m_AttackInterval;
        }

        private void RestoreAnimatorSpeed()
        {
            if (m_Animator != null)
            {
                m_Animator.speed = m_DefaultAnimatorSpeed;
            }
        }

        private float GetMeleeClipLength()
        {
            if (m_CachedMeleeClipLength > 0f)
            {
                return m_CachedMeleeClipLength;
            }

            if (m_Animator != null && m_Animator.runtimeAnimatorController != null)
            {
                AnimationClip[] clips = m_Animator.runtimeAnimatorController.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    AnimationClip clip = clips[i];
                    if (clip == null || clip.events == null)
                    {
                        continue;
                    }

                    for (int e = 0; e < clip.events.Length; e++)
                    {
                        if (clip.events[e].functionName == DamageApplyEventName)
                        {
                            m_CachedMeleeClipLength = Mathf.Max(0.01f, clip.length);
                            return m_CachedMeleeClipLength;
                        }
                    }
                }
            }

            m_CachedMeleeClipLength = 1f;
            return m_CachedMeleeClipLength;
        }

        private void EnsureAnimationEventRelay()
        {
            if (m_Animator == null)
            {
                return;
            }

            CharacterAnimationEventRelay relay = m_Animator.GetComponent<CharacterAnimationEventRelay>();
            if (relay == null)
            {
                relay = m_Animator.gameObject.AddComponent<CharacterAnimationEventRelay>();
            }

            relay.Initialize(this);
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
            bool wasAttacking = m_IsAttacking;
            m_IsAttacking = false;
            m_HasAppliedDamageThisCycle = false;
            m_AttackElapsed = 0f;
            m_AttackInterval = 0f;

            RestoreAnimatorSpeed();

            if (wasAttacking && m_Animator != null)
            {
                m_Animator.ResetTrigger(SwingTheKnifeHash);
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
