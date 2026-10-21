using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 俯视角跟随摄像机数据（不走配置表）。
    /// </summary>
    public sealed class TopDownFollowCameraData : EntityData
    {
        public TopDownFollowCameraData()
            : base(0, 0)
        {
        }

        public int FollowEntityId
        {
            get;
            set;
        }

        public static TopDownFollowCameraData Create(Vector3 position, int followEntityId)
        {
            TopDownFollowCameraData data = ReferencePool.Acquire<TopDownFollowCameraData>();
            data.SetId(CommonUtility.PermanentId);
            data.SetTypeId(0);
            data.Position = position;
            data.Rotation = Quaternion.identity;
            data.FollowEntityId = followEntityId;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            FollowEntityId = 0;
        }
    }
}
