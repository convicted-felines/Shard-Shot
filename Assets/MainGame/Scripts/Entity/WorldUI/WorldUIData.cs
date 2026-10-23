using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>世界 UI 跟随模式。</summary>
    public enum WorldUIFollowMode
    {
        /// <summary>跟随目标 Transform 位置 + 偏移。</summary>
        Default = 0,

        /// <summary>角色血条：固定世界 Y，XZ 来自角色根或击飞布偶臀部。</summary>
        CharacterHealthBar = 1,
    }

    /// <summary>
    /// 世界 UI 实体数据。TypeId 对应 <see cref="WorldUIEnum"/> / DRWorldUI.Id。
    /// </summary>
    public sealed class WorldUIData : EntityData
    {
        public WorldUIData()
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

        /// <summary>跟随模式。</summary>
        public WorldUIFollowMode FollowMode
        {
            get;
            set;
        }

        /// <summary><see cref="WorldUIFollowMode.CharacterHealthBar"/> 时使用的固定世界 Y。</summary>
        public float FixedWorldY
        {
            get;
            set;
        }

        /// <summary>是否朝向世界 UI 摄像机（billboard）。</summary>
        public bool FaceCamera
        {
            get;
            set;
        }

        /// <summary>初始进度 0~1（进度条类可用）。</summary>
        public float Progress
        {
            get;
            set;
        }

        /// <summary>展示文本（飘字等）；空则不改 TMP。</summary>
        public string DisplayText
        {
            get;
            set;
        }

        /// <summary>上浮持续时间（秒）；&lt;=0 表示不上浮。</summary>
        public float FloatDuration
        {
            get;
            set;
        }

        /// <summary>上浮世界距离。</summary>
        public float FloatRiseDistance
        {
            get;
            set;
        }

        public bool IsFloatingText
        {
            get { return FloatDuration > 0f; }
        }

        public static WorldUIData Create(WorldUIEnum worldUIEnum, int followEntityId, Vector3 followOffset, bool faceCamera = true)
        {
            WorldUIData data = ReferencePool.Acquire<WorldUIData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)worldUIEnum);
            data.FollowEntityId = followEntityId;
            data.FollowOffset = followOffset;
            data.FollowMode = WorldUIFollowMode.Default;
            data.FixedWorldY = 0f;
            data.FaceCamera = faceCamera;
            data.Progress = 0f;
            data.DisplayText = null;
            data.FloatDuration = 0f;
            data.FloatRiseDistance = 0f;
            data.Position = Vector3.zero;
            data.Rotation = Quaternion.identity;
            return data;
        }

        public static WorldUIData Create(WorldUIEnum worldUIEnum, Vector3 position, Quaternion rotation, bool faceCamera = true)
        {
            WorldUIData data = ReferencePool.Acquire<WorldUIData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId((int)worldUIEnum);
            data.FollowEntityId = 0;
            data.FollowOffset = Vector3.zero;
            data.FollowMode = WorldUIFollowMode.Default;
            data.FixedWorldY = 0f;
            data.FaceCamera = faceCamera;
            data.Progress = 0f;
            data.DisplayText = null;
            data.FloatDuration = 0f;
            data.FloatRiseDistance = 0f;
            data.Position = position;
            data.Rotation = rotation;
            return data;
        }

        /// <summary>角色血条：跟随角色实体，固定世界 Y，XZ 随角色或击飞布偶臀部。</summary>
        public static WorldUIData CreateCharacterHealthBar(WorldUIEnum worldUIEnum, int followEntityId, float progress01,
            float fixedWorldY = Constant.WorldUI.HealthBarFixedWorldY, bool faceCamera = true)
        {
            WorldUIData data = Create(worldUIEnum, followEntityId, Vector3.zero, faceCamera);
            data.FollowMode = WorldUIFollowMode.CharacterHealthBar;
            data.FixedWorldY = fixedWorldY;
            data.Progress = Mathf.Clamp01(progress01);
            return data;
        }

        /// <summary>一次性飘字：生成在世界坐标，上浮后自动隐藏。</summary>
        public static WorldUIData CreateFloatingText(WorldUIEnum worldUIEnum, Vector3 startPosition, string displayText,
            float duration = 1.2f, float riseDistance = 0.8f, bool faceCamera = true)
        {
            WorldUIData data = Create(worldUIEnum, startPosition, Quaternion.identity, faceCamera);
            data.DisplayText = displayText;
            data.FloatDuration = Mathf.Max(0.01f, duration);
            data.FloatRiseDistance = Mathf.Max(0f, riseDistance);
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            FollowEntityId = 0;
            FollowOffset = Vector3.zero;
            FollowMode = WorldUIFollowMode.Default;
            FixedWorldY = 0f;
            FaceCamera = false;
            Progress = 0f;
            DisplayText = null;
            FloatDuration = 0f;
            FloatRiseDistance = 0f;
        }
    }
}
