using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 榴弹实体：刚体飞行、碰撞弹跳，引信结束后爆炸并对范围内目标造成伤害。
    /// </summary>
    public sealed class GrenadeBulletEntity : Entity
    {
        [SerializeField]
        private float m_DefaultSpeed = 8f;

        [SerializeField]
        private float m_ExplosionRadius = 2.5f;

        [SerializeField]
        private float m_MaxLifetime = 15f;

        private static readonly List<UnityGameFramework.Runtime.Entity> s_EntityBuffer =
            new List<UnityGameFramework.Runtime.Entity>(32);

        private BulletData m_BulletData;
        private Rigidbody m_Rigidbody;
        private Collider m_Collider;
        private float m_ElapsedLifetime;
        private float m_FuseElapsed;
        private bool m_HasExploded;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_BulletData = userData as BulletData;
            if (m_BulletData == null)
            {
                Log.Error("GrenadeBulletEntity data is invalid.");
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

            CachedTransform.SetPositionAndRotation(
                m_BulletData.Position,
                Quaternion.LookRotation(direction, Vector3.up));

            m_Rigidbody = GetComponent<Rigidbody>();
            if (m_Rigidbody == null)
            {
                m_Rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            m_Rigidbody.isKinematic = false;
            m_Rigidbody.useGravity = true;
            m_Rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            m_Rigidbody.angularVelocity = Vector3.zero;

            float speed = m_BulletData.Speed > 0f ? m_BulletData.Speed : m_DefaultSpeed;
            m_Rigidbody.velocity = direction * speed;

            m_Collider = GetComponent<Collider>();
            if (m_Collider != null)
            {
                m_Collider.isTrigger = false;
            }

            IgnoreOwnerCollisions();
            m_ElapsedLifetime = 0f;
            m_FuseElapsed = 0f;
            m_HasExploded = false;
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (m_Rigidbody != null)
            {
                m_Rigidbody.velocity = Vector3.zero;
                m_Rigidbody.angularVelocity = Vector3.zero;
            }

            m_BulletData = null;
            m_Rigidbody = null;
            m_Collider = null;
            m_ElapsedLifetime = 0f;
            m_FuseElapsed = 0f;
            m_HasExploded = false;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_BulletData == null || m_HasExploded)
            {
                return;
            }

            m_ElapsedLifetime += elapseSeconds;
            if (m_ElapsedLifetime >= m_MaxLifetime)
            {
                Explode();
                return;
            }

            m_FuseElapsed += elapseSeconds;
            if (m_FuseElapsed >= m_BulletData.ExplodeDelay)
            {
                Explode();
            }
        }

        private void Explode()
        {
            if (m_HasExploded)
            {
                return;
            }

            m_HasExploded = true;
            ApplyExplosionDamage();
            GameEntry.Entity.HideEntity(this);
        }

        private void ApplyExplosionDamage()
        {
            if (m_BulletData == null || m_BulletData.Damage <= 0f || m_ExplosionRadius <= 0f)
            {
                return;
            }

            Vector3 center = CachedTransform.position;
            float radiusSqr = m_ExplosionRadius * m_ExplosionRadius;

            s_EntityBuffer.Clear();
            GameEntry.Entity.GetAllLoadedEntities(s_EntityBuffer);

            for (int i = 0; i < s_EntityBuffer.Count; i++)
            {
                UnityGameFramework.Runtime.Entity ugfEntity = s_EntityBuffer[i];
                if (ugfEntity == null || ugfEntity.Id == m_BulletData.OwnerEntityId)
                {
                    continue;
                }

                CharacterEntity character = ugfEntity.Logic as CharacterEntity;
                if (character == null)
                {
                    continue;
                }

                IDamageable damageable = character as IDamageable;
                if (damageable == null || !damageable.IsAlive)
                {
                    continue;
                }

                Vector3 offset = character.CachedTransform.position - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                damageable.TakeDamage(m_BulletData.Damage, m_BulletData.OwnerEntityId);
            }
        }

        private void IgnoreOwnerCollisions()
        {
            if (m_BulletData == null || m_BulletData.OwnerEntityId == 0 || m_Collider == null)
            {
                return;
            }

            CharacterEntity owner = GameEntry.Entity.GetGameEntity(m_BulletData.OwnerEntityId) as CharacterEntity;
            if (owner == null)
            {
                return;
            }

            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < ownerColliders.Length; i++)
            {
                Collider ownerCollider = ownerColliders[i];
                if (ownerCollider != null)
                {
                    Physics.IgnoreCollision(m_Collider, ownerCollider, true);
                }
            }
        }
    }
}
