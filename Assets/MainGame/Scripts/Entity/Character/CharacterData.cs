using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 角色实体数据。TypeId 对应 <see cref="CharacterEnum"/> / DRCharacter.Id。
    /// </summary>
    public sealed class CharacterData : EntityData
    {
        public CharacterData()
            : base(0, 0)
        {
        }

        public static CharacterData Create(CharacterEnum characterEnum, Vector3 position, Quaternion rotation)
        {
            CharacterData data = ReferencePool.Acquire<CharacterData>();
            data.SetId(CommonUtility.PermanentId);
            data.SetTypeId((int)characterEnum);
            data.Position = position;
            data.Rotation = rotation;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
        }
    }
}
