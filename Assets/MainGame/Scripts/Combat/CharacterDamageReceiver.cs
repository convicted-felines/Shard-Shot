using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 挂在角色碰撞体上，将武器命中转发给所属 <see cref="IDamageable"/>。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterDamageReceiver : MonoBehaviour
    {
        private IDamageable m_Damageable;

        public void Initialize(IDamageable damageable)
        {
            m_Damageable = damageable;
        }

        public int GetCharacterEntityId()
        {
            if (m_Damageable is CharacterEntity character)
            {
                return character.Id;
            }

            return 0;
        }

        public bool ApplyDamage(float damage, int attackerEntityId = 0)
        {
            if (m_Damageable == null || !m_Damageable.IsAlive || damage <= 0f)
            {
                return false;
            }

            m_Damageable.TakeDamage(damage, attackerEntityId);
            return true;
        }

        public bool ApplyHit(float damage, float stunDuration, int attackerEntityId = 0)
        {
            if (m_Damageable == null || !m_Damageable.IsAlive)
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

            if (hit && stunDuration > 0f && m_Damageable.IsAlive)
            {
                TryApplyStun(stunDuration);
            }

            return hit;
        }

        private void TryApplyStun(float stunDuration)
        {
            if (m_Damageable is not Component component)
            {
                return;
            }

            CharacterStunComponent stun = component.GetComponent<CharacterStunComponent>();
            stun?.ApplyStun(stunDuration);
        }

        public static bool TryApplyDamage(Collider collider, float damage, int attackerEntityId = 0)
        {
            if (collider == null)
            {
                return false;
            }

            CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
            return receiver != null && receiver.ApplyDamage(damage, attackerEntityId);
        }
    }
}
