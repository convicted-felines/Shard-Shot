using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 近战武器实体。攻击判定碰撞体在 Prefab 上直接配置。
    /// </summary>
    public class MeleeWeaponEntity : WeaponEntity
    {
        [SerializeField]
        private Collider m_AttackCollider;

        public Collider AttackCollider => m_AttackCollider;

        protected override void OnEquipped()
        {
            base.OnEquipped();
            SetAttackColliderEnabled(false);
        }

        protected override void OnUnequipped()
        {
            SetAttackColliderEnabled(false);
            base.OnUnequipped();
        }

        private void SetAttackColliderEnabled(bool enabled)
        {
            if (m_AttackCollider != null)
            {
                m_AttackCollider.enabled = enabled;
            }
        }
    }
}
