//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 实体扩展
    /// </summary>
    public static partial class EntityExtension
    {


        

        /// <summary>
        /// 获取游戏实体
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public static Entity GetGameEntity(this EntityComponent entityComponent, int entityId)
        {
            UnityGameFramework.Runtime.Entity entity = entityComponent.GetEntity(entityId);
            if (entity == null)
            {
                return null;
            }

            return (Entity)entity.Logic;
        }

        public static void HideEntity(this EntityComponent entityComponent, Entity entity)
        {
            if (entityComponent.HasEntity(entity.Id))
            {
                entityComponent.HideEntity(entity.Entity);
            }
        }
        public static void HideEntity(this EntityComponent entityComponent, int entityId)
        {
            if (entityComponent.HasEntity(entityId))
            {
                entityComponent.HideEntity(entityId);
            }
        }
        public static void AttachEntity(this EntityComponent entityComponent, Entity entity, int ownerId, string parentTransformPath = null, object userData = null)
        {
            entityComponent.AttachEntity(entity.Entity, ownerId, parentTransformPath, userData);
        }

        public static void AttachEntity(this EntityComponent entityComponent, Entity attachedEntity, Entity owner, string parentTransformPath = null, object userData = null)
        {
            entityComponent.AttachEntity(attachedEntity.Entity, owner.Entity, parentTransformPath, userData);
        }
        public static void DetachEntity(this EntityComponent entityComponent, Entity childentEntity, object userData = null)
        {
            entityComponent.DetachEntity(childentEntity.Entity, userData);
        }



        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="logicType">逻辑类</param>
        /// <param name="entityGroup">实体放置的组</param>
        /// <param name="priority">优先级</param>
        /// <param name="data">用户数据</param>
        private static void ShowEntity(this EntityComponent entityComponent, Type logicType, string entityGroup, int priority, EntityData data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return;
            }

            DREntity drEntity = GameEntry.DataTable.GetDataRow<DREntity>(data.TypeId);
            if (drEntity == null)
            {
                Log.Warning("Can not load entity id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            entityComponent.ShowEntity
                (data.Id, logicType, AssetUtility.GetEntityAsset(drEntity.AssetName),
                entityGroup, priority, data);
        }

        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="entityComponent"></param>
        /// <param name="logicType"></param>
        /// <param name="entityGroup"></param>
        /// <param name="assetPath"></param>
        /// <param name="priority"></param>
        /// <param name="data"></param>
        private static void ShowEntity(this EntityComponent entityComponent, Type logicType, string entityGroup, string assetPath, int priority, EntityData data)
        {
            if (data == null)
            {
                Log.Warning("Data is invalid.");
                return;
            }

            entityComponent.ShowEntity(data.Id, logicType, assetPath, entityGroup, priority, data);
        }

        /// <summary>
        /// 创建进度观察实体：统一使用 <see cref="AssetUtility.GetEmptyEntity"/>。
        /// </summary>
        public static void ShowProgressObserverEntity(this EntityComponent entityComponent, Type logicType, ProgressObserverEntityData data)
        {
            if (entityComponent == null || logicType == null || data == null)
            {
                Log.Warning("ShowProgressObserverEntity: invalid argument.");
                return;
            }

            entityComponent.ShowEntity(data.Id, logicType, AssetUtility.GetEmptyEntity(), Constant.EntityGroup.System, Constant.AssetPriority.Other, data);
        }

        /// <summary>
        /// 显示世界 UI（资源来自 WorldUI 表，挂到 WorldUI 实体组）。
        /// </summary>
        public static void ShowWorldUI(this EntityComponent entityComponent, WorldUIData data)
        {
            if (data == null)
            {
                Log.Warning("WorldUI data is invalid.");
                return;
            }

            DRWorldUI drWorldUI = GameEntry.DataTable.GetDataRow<DRWorldUI>(data.TypeId);
            if (drWorldUI == null)
            {
                Log.Warning("Can not load world UI id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            data.AssetName = drWorldUI.AssetName;
            data.EntityName = drWorldUI.Name;
            WorldUIGroupCanvas.EnsureReady();
            entityComponent.ShowEntity(typeof(WorldUIEntity), Constant.EntityGroup.WorldUI,
                AssetUtility.GetWorldUIAsset(drWorldUI.AssetName), Constant.AssetPriority.UIFormAsset, data);
        }

        /// <summary>
        /// 在目标实体头顶弹出一次性世界飘字（默认 <see cref="WorldUIEnum.普通字体1"/>）。
        /// </summary>
        public static void ShowWorldUIFloatingText(this EntityComponent entityComponent, int followEntityId, string displayText,
            WorldUIEnum worldUIEnum = WorldUIEnum.普通字体1, float headOffsetY = 2.4f)
        {
            if (entityComponent == null || string.IsNullOrEmpty(displayText) || followEntityId == 0)
            {
                return;
            }

            Entity follow = entityComponent.GetGameEntity(followEntityId);
            if (follow == null)
            {
                Log.Warning("ShowWorldUIFloatingText: follow entity '{0}' not found.", followEntityId);
                return;
            }

            Vector3 start = follow.CachedTransform.position + Vector3.up * headOffsetY;
            entityComponent.ShowWorldUIFloatingText(start, displayText, worldUIEnum);
        }

        /// <summary>
        /// 在世界坐标弹出一次性世界飘字（上浮后自动隐藏，默认 <see cref="WorldUIEnum.普通字体1"/>）。
        /// </summary>
        public static void ShowWorldUIFloatingText(this EntityComponent entityComponent, Vector3 worldPosition, string displayText,
            WorldUIEnum worldUIEnum = WorldUIEnum.普通字体1, float duration = 1.2f, float riseDistance = 0.8f)
        {
            if (entityComponent == null || string.IsNullOrEmpty(displayText))
            {
                return;
            }

            WorldUIData data = WorldUIData.CreateFloatingText(worldUIEnum, worldPosition, displayText, duration, riseDistance);
            entityComponent.ShowWorldUI(data);
        }
    }
}
