using System.Collections.Generic;
using GameFramework.Entity;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        private static readonly List<IEntity> s_RagdollEntityBuffer = new List<IEntity>();

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

        /// <summary>
        /// 回收 RagDoll 组内所有已加载布偶（死亡散架、击飞残留等）。
        /// </summary>
        public static void HideAllLoadedRagdolls(this EntityComponent entityComponent)
        {
            if (entityComponent == null)
            {
                return;
            }

            IEntityGroup group = entityComponent.GetEntityGroup(Constant.EntityGroup.RagDoll);
            if (group == null)
            {
                return;
            }

            s_RagdollEntityBuffer.Clear();
            group.GetAllEntities(s_RagdollEntityBuffer);
            for (int i = 0; i < s_RagdollEntityBuffer.Count; i++)
            {
                IEntity entity = s_RagdollEntityBuffer[i];
                if (entity == null)
                {
                    continue;
                }

                entityComponent.HideEntity(entity.Id);
            }
        }
    }
}
