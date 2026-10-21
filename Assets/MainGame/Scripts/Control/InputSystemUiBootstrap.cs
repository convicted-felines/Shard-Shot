using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace MainGame
{
    /// <summary>
    /// 确保存在 EventSystem，并将 StandaloneInputModule 替换为 InputSystemUIInputModule。
    /// 入口场景卸载后仍保持可用（DontDestroyOnLoad）。
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class InputSystemUiBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            EnsureInputSystemUiModule();
        }

        public static void EnsureInputSystemUiModule()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                eventSystem = Object.FindObjectOfType<EventSystem>();
            }

            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                Object.DontDestroyOnLoad(go);
                return;
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() != null)
            {
                StandaloneInputModule legacy = eventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                {
                    legacy.enabled = false;
                }

                Object.DontDestroyOnLoad(eventSystem.gameObject);
                return;
            }

            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Object.Destroy(oldModule);
            }

            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            Object.DontDestroyOnLoad(eventSystem.gameObject);
        }
    }
}
