using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 子弹实体：沿发射方向飞行，命中角色后造成伤害并销毁。
    /// </summary>
    public sealed class BulletEntity : Entity
    {
        [SerializeField]
        private float m_DefaultSpeed = 30f;

        [SerializeField]
        private float m_MaxLifetime = 5f;

        private BulletData m_BulletData;
        private Vector3 m_Velocity;
        private float m_ElapsedLifetime;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_BulletData = userData as BulletData;
            if (m_BulletData == null)
            {
                Log.Error("BulletEntity data is invalid.");
                return;
            }

            Vector3 direction = m_BulletData.Direction;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }
            else
            {
                direction.Normalize();
            }

            float speed = m_BulletData.Speed > 0f ? m_BulletData.Speed : m_DefaultSpeed;
            m_Velocity = direction * speed;
            m_ElapsedLifetime = 0f;

            // 异步加载完成后再次对齐世界坐标，避免对象池复用时位置/朝向残留。
            CachedTransform.SetPositionAndRotation(
                m_BulletData.Position,
                Quaternion.LookRotation(direction, Vector3.up));

            Rigidbody rigidbody = GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;

            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                {
                    collider.isTrigger = true;
                }
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_BulletData = null;
            m_Velocity = Vector3.zero;
            m_ElapsedLifetime = 0f;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_BulletData == null)
            {
                return;
            }

            CachedTransform.position += m_Velocity * elapseSeconds;
            m_ElapsedLifetime += elapseSeconds;
            if (m_ElapsedLifetime >= m_MaxLifetime)
            {
                GameEntry.Entity.HideEntity(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (m_BulletData == null || other == null)
            {
                return;
            }

            if (IsOwnerCollider(other))
            {
                return;
            }

            if (CharacterDamageReceiver.TryApplyDamage(other, m_BulletData.Damage, m_BulletData.OwnerEntityId))
            {
                GameEntry.Entity.HideEntity(this);
            }
        }

        private bool IsOwnerCollider(Collider other)
        {
            if (m_BulletData.OwnerEntityId == 0)
            {
                return false;
            }

            CharacterEntity owner = GameEntry.Entity.GetGameEntity(m_BulletData.OwnerEntityId) as CharacterEntity;
            if (owner == null)
            {
                return false;
            }

            return other.transform.IsChildOf(owner.CachedTransform);
        }
    }
}
