using UnityEditor;
using UnityEngine;

namespace MainGame.Editor
{
    public class CopyPath : EditorWindow
    {
        [MenuItem("GameObject/Copy Path", priority = 0)]
        static void CopyGameObjectPathCommand(MenuCommand menuCommand)
        {
            CopySelectedPath(includeRoot: true);
        }

        [MenuItem("GameObject/Copy Relative Path (Attach)", priority = 1)]
        static void CopyRelativeAttachPathCommand(MenuCommand menuCommand)
        {
            CopySelectedPath(includeRoot: false);
        }

        private static void CopySelectedPath(bool includeRoot)
        {
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject != null)
            {
                string gameObjectPath = GetGameObjectPath(selectedObject, includeRoot);
                EditorGUIUtility.systemCopyBuffer = gameObjectPath;
                Debug.Log(includeRoot
                    ? "选择的节点路径为: " + gameObjectPath
                    : "挂接相对路径为: " + gameObjectPath);
            }
            else
            {
                Debug.LogWarning("No GameObject selected.");
            }
        }

        private static string GetGameObjectPath(GameObject obj, bool includeRoot)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                if (parent.name != "Main Camera")
                {
                    path = parent.name + "/" + path;
                }

                parent = parent.parent;
            }

            if (!includeRoot)
            {
                int slashIndex = path.IndexOf('/');
                path = slashIndex >= 0 ? path.Substring(slashIndex + 1) : string.Empty;
            }

            return path;
        }
    }

}