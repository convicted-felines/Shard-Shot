using GameFramework.Entity;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// WorldUI 实体组根节点上的共享 World Space Canvas，并绑定世界 UI 摄像机。
    /// </summary>
    public static class WorldUIGroupCanvas
    {
        private const float DefaultWorldScale = 1f;

        private static Canvas s_Canvas;
        private static Camera s_WorldCamera;

        public static Canvas Canvas => s_Canvas;

        public static Camera WorldCamera => s_WorldCamera;

        /// <summary>
        /// 确保 WorldUI 组根已挂好 World Space Canvas（惰性，可在组创建后任意时刻调用）。
        /// </summary>
        public static bool EnsureReady()
        {
            if (s_Canvas != null)
            {
                return true;
            }

            if (GameEntry.Entity == null)
            {
                return false;
            }

            IEntityGroup group = GameEntry.Entity.GetEntityGroup(Constant.EntityGroup.WorldUI);
            if (group == null || group.Helper == null)
            {
                Log.Warning("WorldUIGroupCanvas: entity group '{0}' is not ready.", Constant.EntityGroup.WorldUI);
                return false;
            }

            EntityGroupHelperBase helper = group.Helper as EntityGroupHelperBase;
            if (helper == null)
            {
                Log.Warning("WorldUIGroupCanvas: entity group helper is invalid.");
                return false;
            }

            GameObject go = helper.gameObject;
            s_Canvas = go.GetOrAddComponent<Canvas>();
            s_Canvas.renderMode = RenderMode.WorldSpace;
            if (s_WorldCamera == null)
            {
                s_WorldCamera = Camera.main;
            }

            s_Canvas.worldCamera = s_WorldCamera;
            s_Canvas.overrideSorting = true;
            s_Canvas.sortingOrder = 100;

            // 世界 UI 默认不参与点击，避免挡住场景交互。
            GraphicRaycaster raycaster = go.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
            {
                raycaster.enabled = false;
            }

            RectTransform rectTransform = go.transform as RectTransform;
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(100f, 100f);
                rectTransform.localScale = Vector3.one * DefaultWorldScale;
            }
            else
            {
                go.transform.localScale = Vector3.one * DefaultWorldScale;
            }

            return true;
        }

        public static void SetWorldCamera(Camera camera)
        {
            s_WorldCamera = camera;
            if (!EnsureReady())
            {
                return;
            }

            s_Canvas.worldCamera = s_WorldCamera;
        }

        public static void ClearWorldCamera(Camera camera = null)
        {
            if (camera != null && s_WorldCamera != null && s_WorldCamera != camera)
            {
                return;
            }

            s_WorldCamera = null;
            if (s_Canvas != null)
            {
                s_Canvas.worldCamera = null;
            }
        }
    }
}
