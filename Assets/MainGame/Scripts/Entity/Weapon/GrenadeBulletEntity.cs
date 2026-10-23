using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 榴弹实体：刚体飞行、碰撞弹跳，引信结束后爆炸并对范围内敌对角色造成伤害与击飞布偶效果。
    /// </summary>
    public sealed class GrenadeBulletEntity : Entity
    {
        [SerializeField]
        private float m_DefaultSpeed = 8f;

        [SerializeField]
        private float m_ExplosionRadius = 2.5f;

        [SerializeField]
        private float m_ExplosionScatterForce = 12f;

        [SerializeField]
        private float m_MaxLifetime = 15f;

        [SerializeField]
        [Tooltip("引信时间随机偏移上限（秒），实际爆炸时间为 DamageCountdown ± 该值")]
        private float m_ExplodeDelayRandomOffset = 0.3f;

        private static readonly List<UnityGameFramework.Runtime.Entity> s_EntityBuffer =
            new List<UnityGameFramework.Runtime.Entity>(32);

        private BulletData m_BulletData;
        private Rigidbody m_Rigidbody;
        private Collider m_Collider;
        private float m_ElapsedLifetime;
        private float m_FuseElapsed;
        private float m_ActualExplodeDelay;
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
            m_ActualExplodeDelay = ResolveExplodeDelay(m_BulletData.ExplodeDelay);
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
            m_ActualExplodeDelay = 0f;
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
            if (m_FuseElapsed >= m_ActualExplodeDelay)
            {
                Explode();
            }
        }

        private float ResolveExplodeDelay(float baseDelay)
        {
            if (baseDelay <= 0f)
            {
                return 0f;
            }

            if (m_ExplodeDelayRandomOffset <= 0f)
            {
                return baseDelay;
            }

            float offset = Random.Range(-m_ExplodeDelayRandomOffset, m_ExplodeDelayRandomOffset);
            return Mathf.Max(0.1f, baseDelay + offset);
        }

        private void Explode()
        {
            if (m_HasExploded)
            {
                return;
            }

            m_HasExploded = true;

            GameEntry.Juice?.PlayEffectAtPrefabRotation(EffectEnum.榴弹爆炸, CachedTransform.position);
            ApplyExplosionEffects();
            GameEntry.Entity.HideEntity(this);
        }

        private void ApplyExplosionEffects()
        {
            if (m_BulletData == null || m_BulletData.Damage <= 0f || m_ExplosionRadius <= 0f)
            {
                return;
            }

            Vector3 center = CachedTransform.position;
            float radiusSqr = m_ExplosionRadius * m_ExplosionRadius;
            float scatterForce = m_ExplosionScatterForce > 0f ? m_ExplosionScatterForce : 12f;

            CharacterEntity owner = m_BulletData.OwnerEntityId != 0
                ? GameEntry.Entity.GetGameEntity(m_BulletData.OwnerEntityId) as CharacterEntity
                : null;
            CharacterFaction ownerFaction = owner != null ? owner.Faction : CharacterFaction.Player;

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
                if (character == null || !CharacterFactionUtility.IsHostile(ownerFaction, character.Faction))
                {
                    continue;
                }

                character.TryGetCombatWorldPosition(out Vector3 combatPosition);
                Vector3 offset = combatPosition - center;
                offset.y = 0f;
                if (offset.sqrMagnitude > radiusSqr)
                {
                    continue;
                }

                if (character is MonsterCharacterEntity monster)
                {
                    monster.ApplyExplosionHit(m_BulletData.Damage, m_BulletData.OwnerEntityId, center, scatterForce);
                    continue;
                }

                if (character is IDamageable damageable && damageable.IsAlive)
                {
                    damageable.TakeDamage(m_BulletData.Damage, m_BulletData.OwnerEntityId);
                }
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
