namespace MainGame
{
    public static partial class Constant
    {
        /// <summary>
        /// 实体组：按对象池生命周期策略划分（方案三：混合分组）。
        /// </summary>
        public static class EntityGroup
        {
            /// <summary>场景级玩法实体（角色、武器、摄像机）。</summary>
            public const string Gameplay = "Gameplay";

            /// <summary>子弹、手雷等高频投射物。</summary>
            public const string Projectile = "Projectile";

            /// <summary>命中、爆炸、枪口火光等短命 VFX。</summary>
            public const string Effect = "Effect";

            /// <summary>世界空间 UI（组根挂 World Space Canvas，见 <see cref="WorldUIGroupCanvas"/>）。</summary>
            public const string WorldUI = "WorldUI";

            /// <summary>系统/菜单逻辑实体（ProgressObserver 等）。</summary>
            public const string System = "System";

            /// <summary>死亡布偶（重型物理，极少并发）。</summary>
            public const string RagDoll = "RagDoll";
        }
    }
}
