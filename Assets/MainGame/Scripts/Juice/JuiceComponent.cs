using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 战斗视听反馈调度器。后续在武器开火、命中、爆炸、击杀等挂点调用。
    /// </summary>
    public sealed class JuiceComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 播放表驱动特效（世界坐标）。
        /// </summary>
        public void PlayEffect(EffectEnum effectEnum, Vector3 position, Quaternion rotation)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            GameEntry.Entity.ShowEffect(effectEnum, position, rotation);
        }

        /// <summary>
        /// 播放表驱动特效（世界坐标，默认朝向）。
        /// </summary>
        public void PlayEffect(EffectEnum effectEnum, Vector3 position)
        {
            PlayEffect(effectEnum, position, Quaternion.identity);
        }

        /// <summary>
        /// 播放表驱动特效（世界坐标，旋转保持 Prefab 初始朝向）。
        /// </summary>
        public void PlayEffectAtPrefabRotation(EffectEnum effectEnum, Vector3 position)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            GameEntry.Entity.ShowEffectAtPrefabRotation(effectEnum, position);
        }

        /// <summary>
        /// 播放附着于实体的特效。
        /// </summary>
        public void PlayEffectAttached(EffectEnum effectEnum, int followEntityId, Vector3 followOffset = default)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown || followEntityId == 0)
            {
                return;
            }

            GameEntry.Entity.ShowEffectAttached(effectEnum, followEntityId, followOffset);
        }

        /// <summary>
        /// 播放附着于实体子节点的特效，并保持世界朝向。
        /// </summary>
        public void PlayEffectAttachedToTransform(EffectEnum effectEnum, int followEntityId,
            string followTransformPath, Quaternion worldRotation, Vector3 localOffset = default)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown || followEntityId == 0
                || string.IsNullOrEmpty(followTransformPath))
            {
                return;
            }

            GameEntry.Entity.ShowEffectAttachedToTransform(
                effectEnum, followEntityId, followTransformPath, worldRotation, localOffset);
        }

        // 以下 API 供后续 Day 1~2 接入（震屏 / 音效 / HitStop）。

        public void OnWeaponFire(WeaponEnum weaponType, Vector3 position)
        {
        }

        public void OnHit(Vector3 position)
        {
        }

        public void OnExplosion(Vector3 position, float radius)
        {
        }

        public void OnKill(Vector3 position)
        {
        }
    }
}
