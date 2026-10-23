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
        private Transform[] m_Muzzles;

        [SerializeField]
        private Transform m_ShellEjectPoint;

        [SerializeField]
        [Tooltip("相对角色根节点的逻辑开火高度。Q 版动画会使视觉枪口下沉，子弹高度不跟枪口 Y。")]
        private float m_FireHeight = 1.0f;

        [SerializeField]
        [Tooltip("枪口不可用时，相对角色正前方的默认出弹距离。")]
        private float m_DefaultForwardDistance = 0.8f;

        public Transform[] Muzzles => m_Muzzles;

        public Transform ShellEjectPoint => m_ShellEjectPoint;

        /// <summary>
        /// 沿角色水平前方发射子弹；每个枪口各发射一颗，高度使用角色逻辑开火高度，不受动画枪口下沉影响。
        /// </summary>
        public virtual void FireBullet()
        {
            if (Owner == null || WeaponConfig == null || string.IsNullOrEmpty(WeaponConfig.BulletAssetName))
            {
                return;
            }

            Vector3 baseDirection = GetOwnerPlanarForward();

            if (m_Muzzles == null || m_Muzzles.Length == 0)
            {
                FireFromMuzzle(baseDirection, null);
                return;
            }

            bool hasValidMuzzle = false;
            for (int i = 0; i < m_Muzzles.Length; i++)
            {
                Transform muzzle = m_Muzzles[i];
                if (muzzle == null)
                {
                    continue;
                }

                hasValidMuzzle = true;
                FireFromMuzzle(baseDirection, muzzle);
            }

            if (!hasValidMuzzle)
            {
                FireFromMuzzle(baseDirection, null);
            }
        }

        /// <summary>
        /// 子类可覆写，为每发子弹施加方向扰动（如加特林叠射速散布）。
        /// </summary>
        protected virtual Vector3 ApplySpawnDirectionVariation(Vector3 baseDirection)
        {
            return baseDirection;
        }

        protected void FireFromMuzzle(Vector3 baseDirection, Transform muzzle)
        {
            Vector3 direction = ApplySpawnDirectionVariation(baseDirection);
            Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);
            SpawnBullet(direction, rotation, muzzle);
        }

        private void SpawnBullet(Vector3 direction, Quaternion rotation, Transform muzzle)
        {
            BulletData data = CreateBulletData(direction, rotation, muzzle);
            if (data == null)
            {
                return;
            }

            GameEntry.Entity.ShowBullet(data);
        }

        /// <summary>
        /// 子类可覆写以改变单发子弹参数（如加特林铅弹概率）。
        /// </summary>
        protected virtual BulletData CreateBulletData(Vector3 direction, Quaternion rotation, Transform muzzle)
        {
            if (Owner == null || WeaponConfig == null)
            {
                return null;
            }

            Vector3 spawnPosition = GetBulletSpawnPosition(direction, muzzle);
            return BulletData.Create(
                WeaponConfig.BulletAssetName,
                Owner.Id,
                spawnPosition,
                rotation,
                direction,
                WeaponConfig.Damage,
                WeaponConfig.BulletSpeed,
                WeaponConfig.DamageCountdown);
        }

        /// <summary>
        /// 俯视角：前进方向取角色自身 Z 轴正方向在水平面的投影。
        /// </summary>
        protected Vector3 GetOwnerPlanarForward()
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

        protected Vector3 GetBulletSpawnPosition(Vector3 planarForward, Transform muzzle)
        {
            Vector3 ownerPosition = Owner.CachedTransform.position;

            if (muzzle != null)
            {
                // 取枪口水平偏移（双管等多枪口时保留侧向间距）；Y 使用逻辑开火高度。
                Vector3 toMuzzle = muzzle.position - ownerPosition;
                Vector3 muzzleSpawnPosition = ownerPosition + new Vector3(toMuzzle.x, 0f, toMuzzle.z);
                muzzleSpawnPosition.y = ownerPosition.y + m_FireHeight;
                return muzzleSpawnPosition;
            }

            Vector3 defaultSpawnPosition = ownerPosition + planarForward * m_DefaultForwardDistance;
            defaultSpawnPosition.y = ownerPosition.y + m_FireHeight;
            return defaultSpawnPosition;
        }
    }
}
