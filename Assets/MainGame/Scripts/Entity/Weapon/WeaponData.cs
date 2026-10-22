using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 武器实体数据。TypeId 对应 <see cref="WeaponEnum"/> / DRWeapon.Id。
    /// </summary>
    public sealed class WeaponData : EntityData
    {
        public WeaponData()
            : base(0, 0)
        {
        }

        /// <summary>装备目标角色实体 Id。</summary>
        public int OwnerEntityId
        {
            get;
            set;
        }

        /// <summary>相对角色根节点的挂点路径。</summary>
        public string AttachTransformPath
        {
            get;
            set;
        }

        public static WeaponData Create(WeaponEnum weaponEnum, int ownerEntityId, string attachTransformPath,
            Vector3 position, Quaternion rotation)
        {
            WeaponData data = ReferencePool.Acquire<WeaponData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)weaponEnum);
            data.OwnerEntityId = ownerEntityId;
            data.AttachTransformPath = attachTransformPath;
            data.Position = position;
            data.Rotation = rotation;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            OwnerEntityId = 0;
            AttachTransformPath = null;
        }
    }
}
