using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色定身（眩晕）：阻止移动与攻击，驱动眩晕/结束动画触发器。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterStunComponent : MonoBehaviour
    {
        private static readonly int StunHash = Animator.StringToHash("Stun");
        private static readonly int StunEndHash = Animator.StringToHash("StunEnd");
        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        private CharacterEntity m_Owner;
        private Animator m_Animator;
        private CharacterRangedAttackComponent m_RangedAttack;
        private CharacterMeleeAttackComponent m_MeleeAttack;
        private float m_RemainingSeconds;

        public bool IsStunned => m_RemainingSeconds > 0f;

        public static CharacterStunComponent Ensure(CharacterEntity owner, Animator animator)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterStunComponent stun = owner.GetComponent<CharacterStunComponent>();
            if (stun == null)
            {
                stun = owner.gameObject.AddComponent<CharacterStunComponent>();
            }

            stun.Initialize(owner, animator);
            return stun;
        }

        public void Initialize(CharacterEntity owner, Animator animator)
        {
            m_Owner = owner;
            m_Animator = animator;
            m_RangedAttack = owner.GetComponent<CharacterRangedAttackComponent>();
            m_MeleeAttack = owner.GetComponent<CharacterMeleeAttackComponent>();
        }

        public void ApplyStun(float duration)
        {
            if (duration <= 0f || m_Owner == null)
            {
                return;
            }

            if (m_Owner is IDamageable damageable && !damageable.IsAlive)
            {
                return;
            }

            CharacterKnockdownComponent knockdown = m_Owner.GetComponent<CharacterKnockdownComponent>();
            if (knockdown != null && knockdown.IsKnockedDown)
            {
                return;
            }

            bool wasStunned = IsStunned;
            m_RemainingSeconds = Mathf.Max(m_RemainingSeconds, duration);
            if (!wasStunned)
            {
                OnStunStarted();
            }
        }

        public void Tick(float elapseSeconds)
        {
            if (!IsStunned)
            {
                return;
            }

            HoldStunPresentation();

            m_RemainingSeconds -= elapseSeconds;
            if (m_RemainingSeconds > 0f)
            {
                return;
            }

            m_RemainingSeconds = 0f;
            OnStunEnded();
        }

        public void Clear()
        {
            if (IsStunned)
            {
                m_RemainingSeconds = 0f;
                OnStunEnded();
            }

            m_Owner = null;
            m_Animator = null;
            m_RangedAttack = null;
            m_MeleeAttack = null;
        }

        private void OnStunStarted()
        {
            m_RangedAttack?.InterruptAttack();
            m_MeleeAttack?.InterruptAttack();
            HoldStunPresentation();

            if (m_Animator != null)
            {
                m_Animator.ResetTrigger(StunEndHash);
                m_Animator.SetTrigger(StunHash);
            }
        }

        private void OnStunEnded()
        {
            m_Animator?.SetTrigger(StunEndHash);
        }

        private void HoldStunPresentation()
        {
            if (m_Owner?.Rigidbody != null)
            {
                CharacterKnockbackComponent knockback = m_Owner.GetComponent<CharacterKnockbackComponent>();
                if (knockback != null)
                {
                    knockback.ApplyToRigidbody(m_Owner.Rigidbody, Vector3.zero);
                }
                else
                {
                    CharacterRigidbodyUtility.StopPlanarMovement(m_Owner.Rigidbody);
                }
            }

            m_Animator?.SetFloat(SpeedParamHash, 0f);
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
