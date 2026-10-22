using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 站桩靶子角色：固定位置、可承受伤害，供武器测试。
    /// </summary>
    public class TargetCharacterEntity : CharacterEntity, IDamageable
    {
        private float m_CurrentHealth;
        private bool m_IsAlive;

        public bool IsAlive => m_IsAlive;

        public float CurrentHealth => m_CurrentHealth;

        public float MaxHealth => CharacterConfig != null ? CharacterConfig.Health : 0f;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_CurrentHealth = MaxHealth;
            m_IsAlive = m_CurrentHealth > 0f;

            if (Rigidbody != null)
            {
                Rigidbody.isKinematic = true;
                Rigidbody.useGravity = false;
                Rigidbody.velocity = Vector3.zero;
                Rigidbody.angularVelocity = Vector3.zero;
            }

            SetupDamageReceivers();
            FacePosition(Vector3.zero);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_IsAlive = false;
            m_CurrentHealth = 0f;
            base.OnHide(isShutdown, userData);
        }

        public void TakeDamage(float damage, int attackerEntityId = 0)
        {
            if (!m_IsAlive || damage <= 0f)
            {
                return;
            }

            m_CurrentHealth = Mathf.Max(0f, m_CurrentHealth - damage);
            GameEntry.Entity.ShowWorldUIFloatingText(Id, $"-{damage:0}");

            if (m_CurrentHealth <= 0f)
            {
                OnDead();
            }
        }

        private void SetupDamageReceivers()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
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

                receiver.Initialize(this);

                if (Constant.Layer.CharacterLayerId >= 0)
                {
                    collider.gameObject.layer = Constant.Layer.CharacterLayerId;
                }
            }
        }

        private void FacePosition(Vector3 worldPosition)
        {
            Vector3 direction = worldPosition - CachedTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            CachedTransform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        private void OnDead()
        {
            m_IsAlive = false;
            Log.Info("Target character '{0}' destroyed.", Name);
            DelayHide(1.5f);
        }
    }
}
