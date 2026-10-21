using GameFramework;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 编辑器非运行态下，Game Framework 的 BaseComponent 不会执行 InitJsonHelper，
    /// 导致 Utility.Json 不可用。持久数据导出/导入等编辑器工具需先调用 EnsureJsonHelper。
    /// </summary>
    internal static class GameFrameworkJsonHelperBootstrap
    {
        private static bool s_Ensured;

        internal static void EnsureJsonHelper()
        {
#if UNITY_EDITOR
            if (Application.isPlaying || s_Ensured)
            {
                return;
            }

            Utility.Json.SetJsonHelper(new LitJsonHelper());
            s_Ensured = true;
#endif
        }
    }
}
