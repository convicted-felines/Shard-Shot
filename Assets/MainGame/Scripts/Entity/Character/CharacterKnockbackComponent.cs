using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色平面击退：与移动速度分离存储，避免被每帧移动逻辑覆盖。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CharacterKnockbackComponent : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("击退速度衰减速率（越大停得越快）")]
        private float m_Decay = 10f;

        private Vector3 m_Velocity;

        public bool HasActiveKnockback => m_Velocity.sqrMagnitude > 0.01f;

        public static CharacterKnockbackComponent Ensure(CharacterEntity owner)
        {
            if (owner == null)
            {
                return null;
            }

            CharacterKnockbackComponent knockback = owner.GetComponent<CharacterKnockbackComponent>();
            if (knockback == null)
            {
                knockback = owner.gameObject.AddComponent<CharacterKnockbackComponent>();
            }

            return knockback;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (force <= 0f)
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            m_Velocity += direction.normalized * force;
        }

        public void Tick(float elapseSeconds)
        {
            if (!HasActiveKnockback || elapseSeconds <= 0f)
            {
                return;
            }

            float damping = Mathf.Exp(-m_Decay * elapseSeconds);
            m_Velocity *= damping;
            if (m_Velocity.sqrMagnitude <= 0.01f)
            {
                m_Velocity = Vector3.zero;
            }
        }

        public void ApplyToRigidbody(Rigidbody rigidbody, Vector3 intendedPlanarVelocity)
        {
            if (rigidbody == null)
            {
                return;
            }

            intendedPlanarVelocity.y = 0f;
            Vector3 velocity = intendedPlanarVelocity + m_Velocity;
            velocity.y = rigidbody.velocity.y;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.velocity = velocity;
        }

        public void Clear()
        {
            m_Velocity = Vector3.zero;
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
