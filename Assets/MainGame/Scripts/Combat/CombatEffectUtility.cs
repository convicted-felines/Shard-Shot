using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 命中效果：伤害、眩晕、击退等战斗反馈的统一入口。
    /// </summary>
    public static class CombatEffectUtility
    {
        public static bool TryApplyHit(
            Collider collider,
            float damage,
            float stunDuration,
            int attackerEntityId = 0,
            Vector3 knockbackDirection = default,
            float knockbackForce = 0f,
            Vector3 forceOriginWorld = default)
        {
            if (collider == null)
            {
                return false;
            }

            bool hit;
            RagdollOwnerDamageProxy ragdollProxy = collider.GetComponent<RagdollOwnerDamageProxy>();
            if (ragdollProxy != null)
            {
                hit = ragdollProxy.ApplyHit(damage, stunDuration, attackerEntityId);
            }
            else
            {
                CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
                hit = receiver != null && receiver.ApplyHit(damage, stunDuration, attackerEntityId);
            }

            if (hit && knockbackForce > 0f)
            {
                TryApplyKnockback(collider, knockbackDirection, knockbackForce, forceOriginWorld);
            }

            return hit;
        }

        private static void TryApplyKnockback(
            Collider collider,
            Vector3 direction,
            float force,
            Vector3 forceOriginWorld)
        {
            CharacterEntity character = ResolveCharacterFromCollider(collider);
            if (character == null || character is not IDamageable damageable || !damageable.IsAlive)
            {
                return;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            direction.Normalize();
            CharacterKnockdownComponent knockdown = character.GetComponent<CharacterKnockdownComponent>();
            if (knockdown != null && knockdown.IsInRagdollPhase)
            {
                Vector3 origin = forceOriginWorld;
                if (origin == Vector3.zero)
                {
                    origin = collider.bounds.center;
                }

                knockdown.ApplyRagdollExplosionForce(origin, force);
                return;
            }

            if (knockdown != null && knockdown.IsKnockedDown)
            {
                return;
            }

            CharacterKnockbackComponent knockback = character.GetComponent<CharacterKnockbackComponent>();
            if (knockback != null)
            {
                knockback.ApplyKnockback(direction, force);
                if (character.Rigidbody != null && !character.Rigidbody.isKinematic)
                {
                    knockback.ApplyToRigidbody(character.Rigidbody, Vector3.zero);
                }

                return;
            }

            Rigidbody rigidbody = character.Rigidbody;
            if (rigidbody != null && !rigidbody.isKinematic)
            {
                CharacterRigidbodyUtility.ApplyPlanarKnockback(rigidbody, direction, force);
            }
        }

        private static CharacterEntity ResolveCharacterFromCollider(Collider collider)
        {
            if (collider == null || GameEntry.Entity == null)
            {
                return null;
            }

            RagdollOwnerDamageProxy ragdollProxy = collider.GetComponent<RagdollOwnerDamageProxy>();
            if (ragdollProxy != null && ragdollProxy.OwnerEntityId != 0)
            {
                return GameEntry.Entity.GetGameEntity(ragdollProxy.OwnerEntityId) as CharacterEntity;
            }

            CharacterDamageReceiver receiver = collider.GetComponent<CharacterDamageReceiver>();
            int entityId = receiver != null ? receiver.GetCharacterEntityId() : 0;
            return entityId != 0 ? GameEntry.Entity.GetGameEntity(entityId) as CharacterEntity : null;
        }
    }
}
