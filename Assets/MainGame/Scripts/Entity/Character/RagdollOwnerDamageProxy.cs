using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 击飞布偶碰撞体：将命中伤害转发给绑定的角色实体。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RagdollOwnerDamageProxy : MonoBehaviour
    {
        private int m_OwnerEntityId;

        public int OwnerEntityId => m_OwnerEntityId;

        public void Initialize(int ownerEntityId)
        {
            m_OwnerEntityId = ownerEntityId;
        }

        public void Clear()
        {
            m_OwnerEntityId = 0;
        }

        public bool ApplyDamage(float damage, int attackerEntityId = 0)
        {
            IDamageable damageable = ResolveOwnerDamageable();
            if (damageable == null || !damageable.IsAlive || damage <= 0f)
            {
                return false;
            }

            damageable.TakeDamage(damage, attackerEntityId);
            return true;
        }

        public bool ApplyHit(float damage, float stunDuration, int attackerEntityId = 0)
        {
            IDamageable damageable = ResolveOwnerDamageable();
            if (damageable == null || !damageable.IsAlive)
            {
                return false;
            }

            bool hit = false;
            if (damage > 0f)
            {
                hit = ApplyDamage(damage, attackerEntityId);
            }
            else
            {
                hit = true;
            }

            if (hit && stunDuration > 0f && damageable.IsAlive && damageable is Component component)
            {
                component.GetComponent<CharacterStunComponent>()?.ApplyStun(stunDuration);
            }

            return hit;
        }

        private IDamageable ResolveOwnerDamageable()
        {
            if (m_OwnerEntityId == 0 || GameEntry.Entity == null)
            {
                return null;
            }

            return GameEntry.Entity.GetGameEntity(m_OwnerEntityId) as IDamageable;
        }
    }
}
