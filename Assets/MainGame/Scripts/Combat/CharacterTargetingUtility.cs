using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 角色目标选取工具。
    /// </summary>
    public static class CharacterTargetingUtility
    {
        private static readonly List<UnityGameFramework.Runtime.Entity> s_EntityBuffer =
            new List<UnityGameFramework.Runtime.Entity>(32);

        /// <summary>
        /// 在攻击距离内查找最近的存活敌对角色。
        /// </summary>
        public static bool TryFindNearestHostile(CharacterEntity self, float attackRange, out CharacterEntity target)
        {
            target = null;
            if (self == null || attackRange <= 0f || GameEntry.Entity == null)
            {
                return false;
            }

            s_EntityBuffer.Clear();
            GameEntry.Entity.GetAllLoadedEntities(s_EntityBuffer);

            float maxSqrDistance = attackRange * attackRange;
            float nearestSqrDistance = maxSqrDistance;
            Vector3 selfPosition = self.CachedTransform.position;

            for (int i = 0; i < s_EntityBuffer.Count; i++)
            {
                UnityGameFramework.Runtime.Entity ugfEntity = s_EntityBuffer[i];
                if (ugfEntity == null || ugfEntity.Id == self.Id)
                {
                    continue;
                }

                CharacterEntity character = ugfEntity.Logic as CharacterEntity;
                if (character == null || !CharacterFactionUtility.IsHostile(self.Faction, character.Faction))
                {
                    continue;
                }

                IDamageable damageable = character as IDamageable;
                if (damageable != null && !damageable.IsAlive)
                {
                    continue;
                }

                Vector3 offset = character.CachedTransform.position - selfPosition;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > maxSqrDistance || sqrDistance >= nearestSqrDistance)
                {
                    continue;
                }

                nearestSqrDistance = sqrDistance;
                target = character;
            }

            return target != null;
        }

        /// <summary>
        /// 将角色水平旋转朝向目标位置。
        /// </summary>
        public static void RotateToward(CharacterEntity character, Vector3 worldPosition, float turnSpeed, float elapseSeconds)
        {
            if (character == null || turnSpeed <= 0f || elapseSeconds <= 0f)
            {
                return;
            }

            Vector3 direction = worldPosition - character.CachedTransform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            if (character.Rigidbody != null)
            {
                character.Rigidbody.angularVelocity = Vector3.zero;
                Quaternion newRotation = Quaternion.Slerp(
                    character.Rigidbody.rotation,
                    targetRotation,
                    Mathf.Clamp01(turnSpeed * elapseSeconds));
                character.Rigidbody.MoveRotation(newRotation);
                return;
            }

            character.CachedTransform.rotation = Quaternion.Slerp(
                character.CachedTransform.rotation,
                targetRotation,
                Mathf.Clamp01(turnSpeed * elapseSeconds));
        }
    }
}
