using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 玩家战斗组件：读取 Fire 输入并驱动共享远程攻击周期。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterCombatComponent : MonoBehaviour
    {
        private CharacterEntity m_Owner;
        private CharacterRangedAttackComponent m_RangedAttack;

        public bool IsAttacking => m_RangedAttack != null && m_RangedAttack.IsAttacking;

        public void Initialize(CharacterEntity owner, CharacterWeaponComponent weaponComponent, Animator animator)
        {
            m_Owner = owner;

            m_RangedAttack = owner.GetComponent<CharacterRangedAttackComponent>();
            if (m_RangedAttack == null)
            {
                m_RangedAttack = owner.gameObject.AddComponent<CharacterRangedAttackComponent>();
            }

            m_RangedAttack.Initialize(owner, weaponComponent, animator);
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Owner == null || m_RangedAttack == null || GameEntry.Control == null)
            {
                return;
            }

            bool interruptAttack = IsMoveInputActive();
            bool fireHeld = GameEntry.Control.GetAction(GameEntry.Control.Controls.Gameplay.Fire);
            bool fireDown = GameEntry.Control.GetActionDown(GameEntry.Control.Controls.Gameplay.Fire);
            bool wantsAttack = m_RangedAttack.IsAttacking || fireHeld || fireDown;

            CharacterEntity facingTarget = null;
            if (m_Owner.CharacterConfig != null)
            {
                CharacterTargetingUtility.TryFindNearestHostile(
                    m_Owner,
                    m_Owner.CharacterConfig.AttackRange,
                    out facingTarget);
            }

            m_RangedAttack.Tick(elapseSeconds, wantsAttack, interruptAttack, facingTarget);
        }

        private static bool IsMoveInputActive()
        {
            Vector2 moveInput = GameEntry.Control.GetMoveVector();
            return moveInput.sqrMagnitude > 0.01f;
        }

        private void OnDestroy()
        {
            m_Owner = null;
            m_RangedAttack = null;
        }
    }
}
