using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        /// <summary>
        /// 显示特效（资源来自 Effect 表，挂到 Effect 实体组）。
        /// </summary>
        public static void ShowEffect(this EntityComponent entityComponent, EffectData data)
        {
            if (entityComponent == null || data == null)
            {
                Log.Warning("Effect data is invalid.");
                return;
            }

            DREffect drEffect = GameEntry.DataTable.GetDataRow<DREffect>(data.TypeId);
            if (drEffect == null)
            {
                Log.Warning("Can not load effect id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            if (string.IsNullOrEmpty(drEffect.AssetName))
            {
                Log.Warning("Effect id '{0}' has empty AssetName.", data.TypeId.ToString());
                return;
            }

            data.AssetName = drEffect.AssetName;
            data.EntityName = drEffect.Name;
            if (!data.SuppressTableEffectHeight && data.EffectHeight == 0f)
            {
                data.EffectHeight = drEffect.EffectHeight;
            }

            entityComponent.ShowEntity(typeof(EffectEntity), Constant.EntityGroup.Effect,
                AssetUtility.GetEffectAsset(drEffect.AssetName), Constant.AssetPriority.Other, data);
        }

        /// <summary>
        /// 在世界坐标播放一次性特效。
        /// </summary>
        public static void ShowEffect(this EntityComponent entityComponent, EffectEnum effectEnum, Vector3 position,
            Quaternion rotation)
        {
            if (entityComponent == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            EffectData data = EffectData.Create(effectEnum, position, rotation);
            entityComponent.ShowEffect(data);
        }

        /// <summary>
        /// 在世界坐标播放一次性特效（默认朝向）。
        /// </summary>
        public static void ShowEffect(this EntityComponent entityComponent, EffectEnum effectEnum, Vector3 position)
        {
            entityComponent.ShowEffect(effectEnum, position, Quaternion.identity);
        }

        /// <summary>
        /// 在世界坐标播放一次性特效，旋转保持 Prefab 初始朝向。
        /// </summary>
        public static void ShowEffectAtPrefabRotation(this EntityComponent entityComponent, EffectEnum effectEnum,
            Vector3 position)
        {
            if (entityComponent == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            EffectData data = EffectData.CreateAtPrefabRotation(effectEnum, position);
            entityComponent.ShowEffect(data);
        }

        /// <summary>
        /// 在目标实体上播放附着特效。
        /// </summary>
        public static void ShowEffectAttached(this EntityComponent entityComponent, EffectEnum effectEnum,
            int followEntityId, Vector3 followOffset = default)
        {
            if (entityComponent == null || effectEnum == EffectEnum.Unknown || followEntityId == 0)
            {
                return;
            }

            EffectData data = EffectData.CreateAttached(effectEnum, followEntityId, followOffset);
            entityComponent.ShowEffect(data);
        }

        /// <summary>
        /// 附着到目标实体的子节点，并保持世界朝向（如枪口火焰跟随枪口动画且 Z 轴对准开火方向）。
        /// </summary>
        public static int ShowEffectAttachedToTransform(this EntityComponent entityComponent, EffectEnum effectEnum,
            int followEntityId, string followTransformPath, Quaternion worldRotation, Vector3 localOffset = default,
            bool disableAutoHide = false)
        {
            if (entityComponent == null || effectEnum == EffectEnum.Unknown || followEntityId == 0
                || string.IsNullOrEmpty(followTransformPath))
            {
                return 0;
            }

            EffectData data = EffectData.CreateAttachedToTransform(
                effectEnum, followEntityId, followTransformPath, worldRotation, localOffset);
            data.DisableAutoHide = disableAutoHide;
            entityComponent.ShowEffect(data);
            return data.Id;
        }

        /// <summary>
        /// 附着于目标实体播放特效，旋转保持 Prefab 初始朝向。
        /// </summary>
        /// <returns>特效实体 Id；创建失败时返回 0。</returns>
        public static int ShowEffectAttachedAtPrefabRotation(this EntityComponent entityComponent,
            EffectEnum effectEnum, int followEntityId, Vector3 followOffset = default, bool disableAutoHide = false,
            bool suppressTableEffectHeight = false)
        {
            if (entityComponent == null || effectEnum == EffectEnum.Unknown || followEntityId == 0)
            {
                return 0;
            }

            EffectData data = EffectData.CreateAttachedAtPrefabRotation(
                effectEnum, followEntityId, followOffset, disableAutoHide, suppressTableEffectHeight);
            entityComponent.ShowEffect(data);
            return data.Id;
        }
    }
}
