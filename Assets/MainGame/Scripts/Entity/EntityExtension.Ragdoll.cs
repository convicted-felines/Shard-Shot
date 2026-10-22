using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        /// <summary>
        /// 显示死亡布偶实体。
        /// </summary>
        public static void ShowRagdoll(this EntityComponent entityComponent, RagdollEntityData data)
        {
            if (data == null || string.IsNullOrEmpty(data.AssetName))
            {
                Log.Warning("Ragdoll data is invalid.");
                return;
            }

            entityComponent.ShowEntity(typeof(RagdollEntity), Constant.EntityGroup.RagDoll,
                AssetUtility.GetCharacterRagdollAsset(data.AssetName), Constant.AssetPriority.Other, data);
        }
    }
}
