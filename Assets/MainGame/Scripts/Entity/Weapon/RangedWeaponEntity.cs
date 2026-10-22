using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 远程武器实体。枪口、弹壳抛出点等在 Prefab 上直接配置。
    /// </summary>
    public class RangedWeaponEntity : WeaponEntity
    {
        [SerializeField]
        private Transform m_Muzzle;

        [SerializeField]
        private Transform m_ShellEjectPoint;

        [SerializeField]
        [Tooltip("相对角色根节点的逻辑开火高度。Q 版动画会使视觉枪口下沉，子弹高度不跟枪口 Y。")]
        private float m_FireHeight = 1.0f;

        [SerializeField]
        [Tooltip("枪口不可用时，相对角色正前方的默认出弹距离。")]
        private float m_DefaultForwardDistance = 0.8f;

        public Transform Muzzle => m_Muzzle;

        public Transform ShellEjectPoint => m_ShellEjectPoint;

        /// <summary>
        /// 沿角色水平前方发射子弹；高度使用角色逻辑开火高度，不受动画枪口下沉影响。
        /// </summary>
        public void FireBullet()
        {
            if (Owner == null || WeaponConfig == null || string.IsNullOrEmpty(WeaponConfig.BulletAssetName))
            {
                return;
            }

            Vector3 direction = GetOwnerPlanarForward();
            Vector3 spawnPosition = GetBulletSpawnPosition(direction);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

            BulletData data = BulletData.Create(
                WeaponConfig.BulletAssetName,
                Owner.Id,
                spawnPosition,
                rotation,
                direction,
                WeaponConfig.Damage,
                WeaponConfig.BulletSpeed,
                WeaponConfig.DamageCountdown);

            GameEntry.Entity.ShowBullet(data);
        }

        /// <summary>
        /// 俯视角：前进方向取角色自身 Z 轴正方向在水平面的投影。
        /// </summary>
        private Vector3 GetOwnerPlanarForward()
        {
            Vector3 forward = Owner.Rigidbody != null
                ? Owner.Rigidbody.rotation * Vector3.forward
                : Owner.CachedTransform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude <= 0.001f)
            {
                return Vector3.forward;
            }

            return forward.normalized;
        }

        private Vector3 GetBulletSpawnPosition(Vector3 planarForward)
        {
            Vector3 ownerPosition = Owner.CachedTransform.position;
            float forwardDistance = m_DefaultForwardDistance;

            if (m_Muzzle != null)
            {
                // 仅取枪口水平前向距离；Y / 侧向由动画驱动，不用于出弹点。
                Vector3 toMuzzle = m_Muzzle.position - ownerPosition;
                Vector3 planarToMuzzle = new Vector3(toMuzzle.x, 0f, toMuzzle.z);
                float projected = Vector3.Dot(planarToMuzzle, planarForward);
                forwardDistance = projected >= 0.2f
                    ? projected
                    : Mathf.Max(planarToMuzzle.magnitude, m_DefaultForwardDistance);
            }

            Vector3 spawnPosition = ownerPosition + planarForward * forwardDistance;
            spawnPosition.y = ownerPosition.y + m_FireHeight;
            return spawnPosition;
        }
    }
}
