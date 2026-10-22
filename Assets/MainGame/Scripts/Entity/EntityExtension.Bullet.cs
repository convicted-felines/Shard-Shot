using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        /// <summary>
        /// 创建子弹实体（资源与武器同目录）。
        /// </summary>
        public static void ShowBullet(this EntityComponent entityComponent, BulletData data)
        {
            if (data == null || string.IsNullOrEmpty(data.BulletAssetName))
            {
                Log.Warning("Bullet data is invalid.");
                return;
            }

            data.AssetName = data.BulletAssetName;
            data.EntityName = data.BulletAssetName;
            Type logicType = data.ExplodeDelay > 0f ? typeof(GrenadeBulletEntity) : typeof(BulletEntity);
            entityComponent.ShowEntity(logicType, Constant.EntityGroup.Default,
                AssetUtility.GetWeaponAsset(data.BulletAssetName), Constant.AssetPriority.Other, data);
        }
    }
}
