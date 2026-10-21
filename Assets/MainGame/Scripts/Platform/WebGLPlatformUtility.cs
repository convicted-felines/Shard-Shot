using System.Runtime.InteropServices;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// WebGL 运行环境辅助。通过 jslib 读取浏览器 UA / 触控信息，
    /// 弥补 <see cref="Application.isMobilePlatform"/> 在 Web 上不可靠的问题。
    /// </summary>
    public static class WebGLPlatformUtility
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int WebGL_IsMobileBrowser();
#endif

        /// <summary>
        /// 当前是否在手机/平板类浏览器（或小游戏容器）中运行。
        /// 非 WebGL 运行时回退到 <see cref="Application.isMobilePlatform"/>。
        /// </summary>
        public static bool IsMobileBrowser()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            try
            {
                return WebGL_IsMobileBrowser() != 0;
            }
            catch (System.EntryPointNotFoundException)
            {
                return Application.isMobilePlatform;
            }
#else
            return Application.isMobilePlatform;
#endif
        }
    }
}
