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

            if (IsColliderBelongingToEntity(other, m_BulletData.OwnerEntityId)
                || IsColliderBelongingToEntity(other, m_BulletData.IgnoreCharacterEntityId))
            {
                return;
            }

            if (CombatEffectUtility.TryApplyHit(
                other,
                m_BulletData.Damage,
                m_BulletData.StunDuration,
                m_BulletData.OwnerEntityId,
                m_BulletData.Direction,
                m_BulletData.KnockbackForce,
                CachedTransform.position))
            {
                PlayHitEffect();
                NotifyGatlingAttackSpeedBoost();

                if (m_BulletData.Kind == BulletKind.Lead)
                {
                    SpawnScatterBullets(other);
                }

                GameEntry.Entity.HideEntity(this);
            }
        }

        private void SpawnScatterBullets(Collider hitCollider)
        {
            if (m_BulletData == null
                || string.IsNullOrEmpty(m_BulletData.ScatterBulletAssetName)
                || m_BulletData.ScatterCount <= 0
                || m_BulletData.ScatterDamage <= 0f)
            {
                return;
            }

            Vector3 origin = CachedTransform.position;
            Vector3 centerDirection = m_BulletData.Direction;
            centerDirection.y = 0f;
            if (centerDirection.sqrMagnitude <= 0.001f)
            {
                centerDirection = CachedTransform.forward;
                centerDirection.y = 0f;
            }

            centerDirection.Normalize();
            float halfArc = m_BulletData.ScatterArcDegrees * 0.5f;
            int ignoreEntityId = ResolveCharacterEntityId(hitCollider);

            for (int i = 0; i < m_BulletData.ScatterCount; i++)
            {
                float angle = halfArc > 0f ? Random.Range(-halfArc, halfArc) : 0f;
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * centerDirection;
                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
                BulletData scatterData = BulletData.Create(
                    m_BulletData.ScatterBulletAssetName,
                    m_BulletData.OwnerEntityId,
                    origin,
                    rotation,
                    direction,
                    m_BulletData.ScatterDamage,
                    m_BulletData.Speed,
                    stunDuration: m_BulletData.ScatterStunDuration,
                    kind: BulletKind.Scatter);
                scatterData.IgnoreCharacterEntityId = ignoreEntityId;
                scatterData.HitEffect = m_BulletData.HitEffect;
                scatterData.GatlingAttackSpeedBoost = m_BulletData.GatlingAttackSpeedBoost;
                GameEntry.Entity.ShowBullet(scatterData);
            }
        }

        private void PlayHitEffect()
        {
            if (m_BulletData == null || m_BulletData.HitEffect == EffectEnum.Unknown)
            {
                return;
            }

            GameEntry.Juice?.PlayEffect(m_BulletData.HitEffect, CachedTransform.position);
        }

        private void NotifyGatlingAttackSpeedBoost()
        {
            if (m_BulletData == null || !m_BulletData.GatlingAttackSpeedBoost)
            {
                return;
            }

            CharacterEntity owner = GameEntry.Entity.GetGameEntity(m_BulletData.OwnerEntityId) as CharacterEntity;
            if (owner == null)
            {
                return;
            }

            CharacterWeaponComponent weaponComponent = owner.GetComponent<CharacterWeaponComponent>();
            if (weaponComponent?.CurrentWeaponEntity is GatlingWeaponEntity gatling)
            {
                gatling.OnEnemyHit();
            }
        }

        private static int ResolveCharacterEntityId(Collider collider)
        {
            if (collider == null)
            {
                return 0;
            }

            RagdollOwnerDamageProxy ragdollProxy = collider.GetComponent<RagdollOwnerDamageProxy>();
            if (ragdollProxy != null)
            {
                return ragdollProxy.OwnerEntityId;
            }

            CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
            return receiver != null ? receiver.GetCharacterEntityId() : 0;
        }

        private static bool IsColliderBelongingToEntity(Collider other, int entityId)
        {
            if (other == null || entityId == 0)
            {
                return false;
            }

            CharacterEntity character = GameEntry.Entity.GetGameEntity(entityId) as CharacterEntity;
            if (character == null)
            {
                return false;
            }

            return other.transform.IsChildOf(character.CachedTransform);
        }
    }
}
