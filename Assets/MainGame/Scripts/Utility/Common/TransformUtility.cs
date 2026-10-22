using UnityEngine;

namespace MainGame
{
    public static class TransformUtility
    {
        /// <summary>
        /// 获取 target 相对 root 的层级路径（用于 Entity AttachEntity 的 parentTransformPath）。
        /// </summary>
        public static string GetRelativeTransformPath(Transform root, Transform target)
        {
            if (root == null || target == null)
            {
                return null;
            }

            if (target == root)
            {
                return string.Empty;
            }

            string path = target.name;
            Transform current = target.parent;
            while (current != null && current != root)
            {
                path = current.name + "/" + path;
                current = current.parent;
            }

            return current == root ? path : null;
        }
    }
}
