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

        private CharacterStunComponent m_StunComponent;

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
            m_StunComponent = owner.GetComponent<CharacterStunComponent>();
        }

        public void Tick(float elapseSeconds)
        {
            if (m_Owner == null || m_RangedAttack == null || GameEntry.Control == null)
            {
                return;
            }

            if (m_StunComponent != null && m_StunComponent.IsStunned)
            {
                return;
            }

            bool interruptAttack = IsMoveInputActive();

            bool fireHeld = GameEntry.Control.GetAction(GameEntry.Control.Controls.Gameplay.Fire);

            bool fireDown = GameEntry.Control.GetActionDown(GameEntry.Control.Controls.Gameplay.Fire);

            // 仅按住/按下时继续射击；单击完成当前一轮后停止，不因 IsAttacking 自动连射。
            bool wantsAttack = fireHeld || fireDown;



            Vector3? facingWorldPosition = null;
            if (m_Owner.CharacterConfig != null
                && GameEntry.Control.TryGetGameplayAimWorldPoint(
                    m_Owner.CachedTransform.position.y,
                    m_Owner.CachedTransform.position,
                    out Vector3 aimWorldPoint))
            {
                facingWorldPosition = aimWorldPoint;
            }

            m_RangedAttack.Tick(elapseSeconds, wantsAttack, interruptAttack, null, facingWorldPosition);

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
            m_StunComponent = null;
        }

    }

}


