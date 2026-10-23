using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 游戏入口。
    /// </summary>
    public partial class GameEntry : MonoBehaviour
    {
        public static BuiltinDataComponent BuiltinData
        {
            get;
            private set;
        }

        public static ControlComponent Control
        {
            get;
            private set;
        }

        public static PlatformComponent Platform
        {
            get;
            private set;
        }

        public static ProgressObserverComponent ProgressObserver
        {
            get;
            private set;
        }

        public static JuiceComponent Juice
        {
            get;
            private set;
        }

        private static void InitCustomComponents()
        {
            BuiltinData = UnityGameFramework.Runtime.GameEntry.GetComponent<BuiltinDataComponent>();
            Control = UnityGameFramework.Runtime.GameEntry.GetComponent<ControlComponent>();
            Platform = UnityGameFramework.Runtime.GameEntry.GetComponent<PlatformComponent>();
            ProgressObserver = UnityGameFramework.Runtime.GameEntry.GetComponent<ProgressObserverComponent>();
            Juice = UnityGameFramework.Runtime.GameEntry.GetComponent<JuiceComponent>();
        }
    }
}
