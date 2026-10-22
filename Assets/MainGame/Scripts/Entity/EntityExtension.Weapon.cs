using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static partial class EntityExtension
    {
        /// <summary>
        /// 创建武器实体（资源来自 Weapon 表），创建后由 <see cref="WeaponEntity"/> 挂接到角色。
        /// </summary>
        public static void ShowWeapon(this EntityComponent entityComponent, WeaponData data)
        {
            if (data == null)
            {
                Log.Warning("Weapon data is invalid.");
                return;
            }

            DRWeapon drWeapon = GameEntry.DataTable.GetDataRow<DRWeapon>(data.TypeId);
            if (drWeapon == null)
            {
                Log.Warning("Can not load weapon id '{0}' from data table.", data.TypeId);
                return;
            }

            data.AssetName = drWeapon.AssetName;
            data.EntityName = drWeapon.Name;
            Type logicType = ResolveWeaponLogicType(data.TypeId, drWeapon);
            entityComponent.ShowEntity(logicType, Constant.EntityGroup.Default,
                AssetUtility.GetWeaponAsset(drWeapon.AssetName), Constant.AssetPriority.Other, data);
        }

        private static Type ResolveWeaponLogicType(int weaponId, DRWeapon config)
        {
            switch ((WeaponEnum)weaponId)
            {
                case WeaponEnum.加特林:
                case WeaponEnum.榴弹炮:
                    return typeof(RangedWeaponEntity);
            }

            if (config != null && config.WeaponType == Constant.WeaponType.Melee)
            {
                return typeof(MeleeWeaponEntity);
            }

            return typeof(RangedWeaponEntity);
        }
    }
}
