using GameFramework;
using UnityEngine;

namespace MainGame
{
    public enum BulletKind
    {
        Normal = 0,
        Lead = 1,
        Scatter = 2,
    }

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

        /// <summary>
        /// 碰撞时忽略该角色实体（如铅弹分裂弹不再命中直接被铅弹击中的单位）。
        /// </summary>
        public int IgnoreCharacterEntityId
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

        /// <summary>
        /// 命中后定身时长（秒）；0 表示不眩晕。
        /// </summary>
        public float StunDuration
        {
            get;
            set;
        }

        public BulletKind Kind
        {
            get;
            set;
        }

        /// <summary>铅弹命中后扇形分裂的小弹资源名；仅 <see cref="BulletKind.Lead"/> 使用。</summary>
        public string ScatterBulletAssetName
        {
            get;
            set;
        }

        public int ScatterCount
        {
            get;
            set;
        }

        public float ScatterArcDegrees
        {
            get;
            set;
        }

        public float ScatterDamage
        {
            get;
            set;
        }

        public float ScatterStunDuration
        {
            get;
            set;
        }

        /// <summary>
        /// 命中击退强度（沿子弹飞行方向的平面冲量）；0 表示不击退。
        /// </summary>
        public float KnockbackForce
        {
            get;
            set;
        }

        /// <summary>
        /// 命中造成伤害时播放的特效；<see cref="EffectEnum.Unknown"/> 表示不播放。
        /// </summary>
        public EffectEnum HitEffect
        {
            get;
            set;
        }

        /// <summary>
        /// 命中敌人时为所属加特林武器叠加攻速。
        /// </summary>
        public bool GatlingAttackSpeedBoost
        {
            get;
            set;
        }

        public static BulletData Create(string bulletAssetName, int ownerEntityId, Vector3 position, Quaternion rotation,
            Vector3 direction, float damage, float speed, float explodeDelay = 0f, float stunDuration = 0f,
            BulletKind kind = BulletKind.Normal)
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
            data.StunDuration = stunDuration;
            data.Kind = kind;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            OwnerEntityId = 0;
            IgnoreCharacterEntityId = 0;
            Damage = 0f;
            Speed = 0f;
            ExplodeDelay = 0f;
            StunDuration = 0f;
            Kind = BulletKind.Normal;
            ScatterBulletAssetName = null;
            ScatterCount = 0;
            ScatterArcDegrees = 0f;
            ScatterDamage = 0f;
            ScatterStunDuration = 0f;
            KnockbackForce = 0f;
            HitEffect = EffectEnum.Unknown;
            GatlingAttackSpeedBoost = false;
            Direction = Vector3.forward;
            BulletAssetName = null;
        }
    }
}
