using UnityEngine;
using UnityEngine.Rendering;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 按平台切换 QualitySettings 质量层与限帧。
    /// 质量层与管线资源由编辑器菜单 Tools/Graphics/Ensure MiniGame Quality Levels 注册。
    /// </summary>
    public static class PlatformGraphicsUtility
    {
        /// <summary>
        /// 按当前平台应用画质：抖音 → DouYin，微信 → WeChat，原生安卓 → Android。
        /// 小游戏额外限 30 帧；原生安卓目标 60 帧并关闭 VSync。
        /// </summary>
        public static void ApplyForPlatform(PlatformType platformType)
        {
            switch (platformType)
            {
                case PlatformType.DouYinMiniGame:
                    TrySetQualityLevel(Constant.Graphics.DouYinQualityName);
                    ApplyMiniGameFramePacing();
                    break;
                case PlatformType.WeChatMiniGame:
                    TrySetQualityLevel(Constant.Graphics.WeChatQualityName);
                    ApplyMiniGameFramePacing();
                    break;
                default:
                    if (Application.platform == RuntimePlatform.Android)
                    {
                        TrySetQualityLevel(Constant.Graphics.AndroidQualityName);
                        ApplyAndroidFramePacing();
                    }
                    break;
            }
        }

        public static void ApplyMiniGameFramePacing()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Constant.Graphics.MiniGameTargetFrameRate;
            Log.Info(
                "Platform graphics: mini-game frame pacing applied. targetFrameRate={0}, vSyncCount={1}",
                Application.targetFrameRate,
                QualitySettings.vSyncCount);
        }

        public static void ApplyAndroidFramePacing()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Constant.Graphics.AndroidTargetFrameRate;
            Log.Info(
                "Platform graphics: android frame pacing applied. targetFrameRate={0}, vSyncCount={1}",
                Application.targetFrameRate,
                QualitySettings.vSyncCount);
        }

        public static bool TrySetQualityLevel(string qualityName)
        {
            if (string.IsNullOrEmpty(qualityName))
            {
                return false;
            }

            string[] names = QualitySettings.names;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] != qualityName)
                {
                    continue;
                }

                if (QualitySettings.GetQualityLevel() != i)
                {
                    QualitySettings.SetQualityLevel(i, true);
                }

                RenderPipelineAsset pipeline = QualitySettings.renderPipeline;
                Log.Info(
                    "Platform graphics: quality '{0}' (index={1}) applied. pipeline={2}",
                    qualityName,
                    i,
                    pipeline != null ? pipeline.name : "null");
                return true;
            }

            Log.Warning(
                "Platform graphics: quality level '{0}' not found. Run menu 'Tools/Graphics/Ensure MiniGame Quality Levels' in Editor, then rebuild.",
                qualityName);
            return false;
        }
    }
}
