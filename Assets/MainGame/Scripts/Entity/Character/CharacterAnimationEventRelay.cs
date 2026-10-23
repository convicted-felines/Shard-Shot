using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 挂在 Animator 所在物体上，转发动画事件到战斗组件。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterAnimationEventRelay : MonoBehaviour
    {
        private CharacterMeleeAttackComponent m_MeleeAttack;

        public void Initialize(CharacterMeleeAttackComponent meleeAttack)
        {
            m_MeleeAttack = meleeAttack;
        }

        /// <summary>动画片段事件名须与此方法一致。</summary>
        public void DamageApply()
        {
            m_MeleeAttack?.OnDamageApply();
        }

        private void OnDestroy()
        {
            m_MeleeAttack = null;
        }
    }
}
