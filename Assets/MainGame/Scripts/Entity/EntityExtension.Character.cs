using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        /// <summary>
        /// 创建角色实体（资源来自 Character 表）。
        /// </summary>
        public static void ShowCharacter(this EntityComponent entityComponent, CharacterData data)
        {
            if (data == null)
            {
                Log.Warning("Character data is invalid.");
                return;
            }

            DRCharacter drCharacter = GameEntry.DataTable.GetDataRow<DRCharacter>(data.TypeId);
            if (drCharacter == null)
            {
                Log.Warning("Can not load character id '{0}' from data table.", data.TypeId.ToString());
                return;
            }

            data.AssetName = drCharacter.AssetName;
            data.EntityName = drCharacter.Name;
            Type logicType;
            if (data.TypeId == (int)CharacterEnum.玩家角色)
            {
                logicType = typeof(PlayerCharacterEntity);
            }
            else if (data.TypeId == (int)CharacterEnum.骑士机器人)
            {
                logicType = typeof(TargetCharacterEntity);
            }
            else
            {
                logicType = typeof(CharacterEntity);
            }
            entityComponent.ShowEntity(logicType, Constant.EntityGroup.Default,
                AssetUtility.GetCharacterAsset(drCharacter.AssetName), Constant.AssetPriority.Other, data);
        }
    }
}
