using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace EpicToonFX
{
    /// <summary>
    /// Replaces legacy UI input modules in Epic Toon FX demo scenes.
    /// </summary>
    internal static class ETFXInputSystemUiBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnAfterSceneLoad()
        {
            EnsureInputSystemUiModule();
        }

        internal static void EnsureInputSystemUiModule()
        {
            EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
            for (int i = 0; i < eventSystems.Length; i++)
            {
                UpgradeEventSystem(eventSystems[i]);
            }
        }

        private static void UpgradeEventSystem(EventSystem eventSystem)
        {
            if (eventSystem == null)
            {
                return;
            }

            RemoveLegacyModules(eventSystem);

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }

        private static void RemoveLegacyModules(EventSystem eventSystem)
        {
            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Object.Destroy(standalone);
            }

            TouchInputModule touch = eventSystem.GetComponent<TouchInputModule>();
            if (touch != null)
            {
                Object.Destroy(touch);
            }
        }
    }
}
