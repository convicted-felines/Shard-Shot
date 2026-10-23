using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 特效实体数据。TypeId 对应 <see cref="EffectEnum"/> / <see cref="DREffect"/>.Id。
    /// </summary>
    public sealed class EffectData : EntityData
    {
        public EffectData()
            : base(0, 0)
        {
        }

        /// <summary>跟随目标实体 Id；0 表示不跟随，仅用 Position。</summary>
        public int FollowEntityId
        {
            get;
            set;
        }

        /// <summary>相对跟随目标的世界空间偏移。</summary>
        public Vector3 FollowOffset
        {
            get;
            set;
        }

        /// <summary>来自特效表的垂直偏移（世界 Y）。</summary>
        public float EffectHeight
        {
            get;
            set;
        }

        /// <summary>强制隐藏时长（秒）；&lt;=0 时由 <see cref="EffectEntity"/> 根据粒子推算。</summary>
        public float AutoHideDuration
        {
            get;
            set;
        }

        /// <summary>为 true 时仅设置位置，旋转保持 Prefab 初始朝向（对象池复用安全）。</summary>
        public bool UsePrefabRotation
        {
            get;
            set;
        }

        /// <summary>为 true 时不按粒子时长自动回收，由外部 HideEntity。</summary>
        public bool DisableAutoHide
        {
            get;
            set;
        }

        public static EffectData Create(EffectEnum effectEnum, Vector3 position, Quaternion rotation)
        {
            EffectData data = ReferencePool.Acquire<EffectData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)effectEnum);
            data.FollowEntityId = 0;
            data.FollowOffset = Vector3.zero;
            data.EffectHeight = 0f;
            data.AutoHideDuration = 0f;
            data.Position = position;
            data.Rotation = rotation;
            data.UsePrefabRotation = false;
            data.DisableAutoHide = false;
            return data;
        }

        public static EffectData CreateAtPrefabRotation(EffectEnum effectEnum, Vector3 position)
        {
            EffectData data = ReferencePool.Acquire<EffectData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)effectEnum);
            data.FollowEntityId = 0;
            data.FollowOffset = Vector3.zero;
            data.EffectHeight = 0f;
            data.AutoHideDuration = 0f;
            data.Position = position;
            data.Rotation = Quaternion.identity;
            data.UsePrefabRotation = true;
            data.DisableAutoHide = false;
            return data;
        }

        public static EffectData CreateAttached(EffectEnum effectEnum, int followEntityId, Vector3 followOffset)
        {
            EffectData data = ReferencePool.Acquire<EffectData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)effectEnum);
            data.FollowEntityId = followEntityId;
            data.FollowOffset = followOffset;
            data.EffectHeight = 0f;
            data.AutoHideDuration = 0f;
            data.Position = Vector3.zero;
            data.Rotation = Quaternion.identity;
            data.UsePrefabRotation = false;
            data.DisableAutoHide = false;
            return data;
        }

        public static EffectData CreateAttachedAtPrefabRotation(EffectEnum effectEnum, int followEntityId,
            Vector3 followOffset = default, bool disableAutoHide = false)
        {
            EffectData data = CreateAttached(effectEnum, followEntityId, followOffset);
            data.UsePrefabRotation = true;
            data.DisableAutoHide = disableAutoHide;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            FollowEntityId = 0;
            FollowOffset = Vector3.zero;
            EffectHeight = 0f;
            AutoHideDuration = 0f;
            UsePrefabRotation = false;
            DisableAutoHide = false;
        }
    }
}
