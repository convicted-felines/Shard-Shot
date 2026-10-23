namespace MainGame
{
    public static partial class Constant
    {
        /// <summary>短命 VFX 实体默认参数。</summary>
        public static class Effect
        {
            /// <summary>无法从粒子系统推算时长时的兜底隐藏时间（秒）。</summary>
            public const float DefaultLifetime = 3f;

            /// <summary>粒子推算时长上额外等待（秒），避免尾粒子未播完就回收。</summary>
            public const float LifetimePadding = 0.15f;
        }
    }
}
