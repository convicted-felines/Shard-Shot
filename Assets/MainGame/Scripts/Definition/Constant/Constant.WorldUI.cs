namespace MainGame
{
    public static partial class Constant
    {
        public static class WorldUI
        {
            /// <summary>角色世界血条固定世界高度（仅追踪 XZ）。</summary>
            public const float HealthBarFixedWorldY = 3f;

            /// <summary>伤害飘字水平散布半径（世界单位）；0 表示不散布。</summary>
            public const float FloatingTextScatterRadius = 0.65f;

            /// <summary>伤害飘字垂直散布半幅（世界单位）。</summary>
            public const float FloatingTextScatterVertical = 0.2f;
        }
    }
}
