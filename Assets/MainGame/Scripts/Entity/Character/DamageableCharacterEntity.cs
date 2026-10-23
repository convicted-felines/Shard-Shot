using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 可受伤、可死亡的角色基类。
    /// </summary>
    public abstract class DamageableCharacterEntity : CharacterEntity, IDamageable
    {
        private float m_CurrentHealth;
        private bool m_IsAlive;
        private int m_LastAttackerEntityId;

        public bool IsAlive => m_IsAlive;

        public float CurrentHealth => m_CurrentHealth;

        public float MaxHealth => CharacterConfig != null ? CharacterConfig.Health : 0f;

        protected int LastAttackerEntityId => m_LastAttackerEntityId;

        protected void InitializeHealth()
        {
            m_CurrentHealth = MaxHealth;
            m_IsAlive = m_CurrentHealth > 0f;
            m_LastAttackerEntityId = 0;
        }

        protected void ClearHealthState()
        {
            m_IsAlive = false;
            m_CurrentHealth = 0f;
            m_LastAttackerEntityId = 0;
        }

        public virtual void TakeDamage(float damage, int attackerEntityId = 0)
        {
            if (!m_IsAlive || damage <= 0f)
            {
                return;
            }

            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
            if (attackerEntityId != 0)
            {
                m_LastAttackerEntityId = attackerEntityId;
            }

            GameEntry.Entity.ShowWorldUIFloatingText(Id, $"-{damage:0}");

            if (m_CurrentHealth <= 0f)
            {
                OnDead();
            }
        }

        protected virtual void OnDead()
        {
            m_IsAlive = false;
            SpawnDeathRagdollAndHide(m_LastAttackerEntityId);
        }

        public static void SetupDamageReceivers(CharacterEntity owner, IDamageable damageable)
        {
            if (owner == null || damageable == null)
            {
                return;
            }

            Collider[] colliders = owner.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
                if (receiver == null)
                {
                    receiver = collider.gameObject.AddComponent<CharacterDamageReceiver>();
                }

                receiver.Initialize(damageable);

                if (Constant.Layer.CharacterLayerId >= 0)
                {
                    collider.gameObject.layer = Constant.Layer.CharacterLayerId;
                }
            }
        }
    }
}
