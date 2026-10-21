namespace MainGame
{
    public static partial class Constant
    {
        /// <summary>
        /// 平台画质 / 渲染管线相关常量。
        /// </summary>
        public static class Graphics
        {
            public const string DouYinQualityName = "DouYin";
            public const string WeChatQualityName = "WeChat";
            public const string AndroidQualityName = "Android";

            public const string DouYinPipelineAssetPath = "Assets/Settings/douyin_PipelineAsset.asset";
            public const string WeChatPipelineAssetPath = "Assets/Settings/wechat_PipelineAsset.asset";
            public const string AndroidPipelineAssetPath = "Assets/Settings/android_PipelineAsset.asset";
            public const string DouYinForwardRendererPath = "Assets/Settings/douyin_PipelineAsset_ForwardRenderer.asset";
            public const string WeChatForwardRendererPath = "Assets/Settings/wechat_PipelineAsset_ForwardRenderer.asset";
            public const string AndroidForwardRendererPath = "Assets/Settings/android_PipelineAsset_ForwardRenderer.asset";

            /// <summary>抖音 / 微信小游戏推荐目标帧率。</summary>
            public const int MiniGameTargetFrameRate = 30;

            /// <summary>原生安卓（TapTap 等）推荐目标帧率。</summary>
            public const int AndroidTargetFrameRate = 60;
        }
    }
}
