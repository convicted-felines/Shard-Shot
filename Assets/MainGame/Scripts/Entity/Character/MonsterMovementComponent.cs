using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 怪物移动 AI：朝最近敌对目标径直移动，进入攻击距离后停止由战斗组件接管。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MonsterMovementComponent : MonoBehaviour
    {
        private static readonly int SpeedParamHash = Animator.StringToHash("Speed");

        private DamageableCharacterEntity m_Owner;
        private MonsterCombatComponent m_CombatComponent;
        private Animator m_Animator;
        private float m_AnimatorSpeed;

        public void Initialize(DamageableCharacterEntity owner, MonsterCombatComponent combatComponent, Animator animator)
        {
            m_Owner = owner;
            m_CombatComponent = combatComponent;
            m_Animator = animator;
            m_AnimatorSpeed = 0f;
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Owner == null
                || !m_Owner.IsAlive
                || m_Owner.CharacterConfig == null
                || m_Owner.Rigidbody == null)
            {
                return;
            }

            Rigidbody rigidbody = m_Owner.Rigidbody;

            if (m_CombatComponent != null && m_CombatComponent.IsAttacking)
            {
                CharacterRigidbodyUtility.StopPlanarMovement(rigidbody);
                UpdateAnimator(elapseSeconds);
                return;
            }

            if (!CharacterTargetingUtility.TryFindNearestHostile(m_Owner, out CharacterEntity target))
            {
                CharacterRigidbodyUtility.StopPlanarMovement(rigidbody);
                UpdateAnimator(elapseSeconds);
                return;
            }

            Vector3 selfPosition = m_Owner.CachedTransform.position;
            Vector3 targetPosition = target.CachedTransform.position;
            Vector3 toTarget = targetPosition - selfPosition;
            toTarget.y = 0f;

            float attackRange = m_Owner.CharacterConfig.AttackRange;
            float attackRangeSqr = attackRange * attackRange;
            if (toTarget.sqrMagnitude <= attackRangeSqr)
            {
                CharacterRigidbodyUtility.StopPlanarMovement(rigidbody);
                CharacterTargetingUtility.RotateToward(
                    m_Owner,
                    targetPosition,
                    m_Owner.CharacterConfig.TurnSpeed,
                    elapseSeconds);
                UpdateAnimator(elapseSeconds);
                return;
            }

            Vector3 moveDirection = toTarget.normalized;
            float moveSpeed = m_Owner.CharacterConfig.MoveSpeed;
            CharacterRigidbodyUtility.ApplyPlanarVelocity(rigidbody, moveDirection * moveSpeed);

            CharacterTargetingUtility.RotateToward(
                m_Owner,
                targetPosition,
                m_Owner.CharacterConfig.TurnSpeed,
                elapseSeconds);
            UpdateAnimator(elapseSeconds);
        }

        private void UpdateAnimator(float elapseSeconds)
        {
            if (m_Animator == null || m_Owner == null || m_Owner.CharacterConfig == null)
            {
                return;
            }

            float moveSpeed = m_Owner.CharacterConfig.MoveSpeed;
            float horizontalSpeed = CharacterRigidbodyUtility.GetPlanarSpeed(m_Owner.Rigidbody);
            float targetSpeed = moveSpeed > 0.01f ? Mathf.Clamp01(horizontalSpeed / moveSpeed) : 0f;
            m_AnimatorSpeed = Mathf.Lerp(m_AnimatorSpeed, targetSpeed, elapseSeconds * 10f);
            m_Animator.SetFloat(SpeedParamHash, m_AnimatorSpeed);
        }

        private void OnDestroy()
        {
            m_Owner = null;
            m_CombatComponent = null;
            m_Animator = null;
        }
    }
}
