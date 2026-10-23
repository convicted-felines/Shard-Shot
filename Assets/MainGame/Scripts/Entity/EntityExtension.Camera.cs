using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        private const string TopDownFollowCameraAssetName = "TopDownFollowCamera";

        /// <summary>
        /// 创建俯视角跟随摄像机（资源位于 Entities/Other，不走配置表）。
        /// </summary>
        public static void ShowTopDownFollowCamera(this EntityComponent entityComponent, TopDownFollowCameraData data)
        {
            if (data == null)
            {
                Log.Warning("TopDownFollowCamera data is invalid.");
                return;
            }

            data.AssetName = TopDownFollowCameraAssetName;
            data.EntityName = TopDownFollowCameraAssetName;
            entityComponent.ShowEntity(typeof(TopDownFollowCameraEntity), Constant.EntityGroup.Default,
                AssetUtility.GetOtherEntityAsset(TopDownFollowCameraAssetName), Constant.AssetPriority.Other, data);
        }
    }
}
