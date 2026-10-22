using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 子弹实体数据。
    /// </summary>
    public sealed class BulletData : EntityData
    {
        public BulletData()
            : base(0, 0)
        {
        }

        public int OwnerEntityId
        {
            get;
            set;
        }

        public float Damage
        {
            get;
            set;
        }

        public float Speed
        {
            get;
            set;
        }

        /// <summary>
        /// 榴弹引信时间；为 0 时表示即时命中子弹。
        /// </summary>
        public float ExplodeDelay
        {
            get;
            set;
        }

        public Vector3 Direction
        {
            get;
            set;
        }

        public string BulletAssetName
        {
            get;
            set;
        }

        public static BulletData Create(string bulletAssetName, int ownerEntityId, Vector3 position, Quaternion rotation,
            Vector3 direction, float damage, float speed, float explodeDelay = 0f)
        {
            BulletData data = ReferencePool.Acquire<BulletData>();
            data.SetId(CommonUtility.TemporaryId);
            data.SetTypeId(0);
            data.BulletAssetName = bulletAssetName;
            data.OwnerEntityId = ownerEntityId;
            data.Position = position;
            data.Rotation = rotation;
            data.Direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
            data.Damage = damage;
            data.Speed = speed;
            data.ExplodeDelay = explodeDelay;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            OwnerEntityId = 0;
            Damage = 0f;
            Speed = 0f;
            ExplodeDelay = 0f;
            Direction = Vector3.forward;
            BulletAssetName = null;
        }
    }
}
