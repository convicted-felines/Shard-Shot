using System.Collections.Generic;
using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 从角色骨骼层级采集的姿态快照，供布偶实体创建后还原当前姿势。
    /// </summary>
    public sealed class RagdollBonePoseSnapshot
    {
        private const string RootPath = "";

        private struct BonePose
        {
            public string BonePath;
            public bool IsRoot;
            public Vector3 WorldPosition;
            public Quaternion WorldRotation;
            public Quaternion LocalRotation;
        }

        private readonly List<BonePose> m_Bones = new List<BonePose>(48);

        public bool IsValid => m_Bones.Count > 0;

        public void Clear()
        {
            m_Bones.Clear();
        }

        public RagdollBonePoseSnapshot Clone()
        {
            var clone = new RagdollBonePoseSnapshot();
            clone.m_Bones.AddRange(m_Bones);
            return clone;
        }

        public static RagdollBonePoseSnapshot CaptureFromHierarchy(Transform characterRoot, string boneRootName)
        {
            var snapshot = new RagdollBonePoseSnapshot();
            if (characterRoot == null || string.IsNullOrEmpty(boneRootName))
            {
                return snapshot;
            }

            Transform boneRoot = FindChildRecursive(characterRoot, boneRootName);
            if (boneRoot == null)
            {
                return snapshot;
            }

            snapshot.CaptureRecursive(boneRoot, isRoot: true, RootPath);
            return snapshot;
        }

        public bool TryApplyToRagdoll(Transform ragdollRoot, string boneRootName)
        {
            if (!IsValid || ragdollRoot == null || string.IsNullOrEmpty(boneRootName))
            {
                return false;
            }

            Transform hips = FindChildRecursive(ragdollRoot, boneRootName);
            if (hips == null)
            {
                return false;
            }

            var pathToPose = BuildPathMap();
            if (!pathToPose.TryGetValue(RootPath, out BonePose hipsPose) || !hipsPose.IsRoot)
            {
                return false;
            }

            hips.SetPositionAndRotation(hipsPose.WorldPosition, hipsPose.WorldRotation);
            ApplyLocalRotationsRecursive(hips, pathToPose, RootPath);
            return true;
        }

        private Dictionary<string, BonePose> BuildPathMap()
        {
            var pathToPose = new Dictionary<string, BonePose>(m_Bones.Count);
            for (int i = 0; i < m_Bones.Count; i++)
            {
                pathToPose[m_Bones[i].BonePath] = m_Bones[i];
            }

            return pathToPose;
        }

        private static int ApplyLocalRotationsRecursive(Transform bone, Dictionary<string, BonePose> pathToPose, string pathFromRoot)
        {
            int appliedCount = 0;
            for (int i = 0; i < bone.childCount; i++)
            {
                Transform child = bone.GetChild(i);
                string childPath = BuildChildPath(pathFromRoot, child.name);
                if (pathToPose.TryGetValue(childPath, out BonePose pose) && !pose.IsRoot)
                {
                    child.localRotation = pose.LocalRotation;
                    appliedCount++;
                }

                appliedCount += ApplyLocalRotationsRecursive(child, pathToPose, childPath);
            }

            return appliedCount;
        }

        private void CaptureRecursive(Transform bone, bool isRoot, string pathFromRoot)
        {
            m_Bones.Add(new BonePose
            {
                BonePath = pathFromRoot,
                IsRoot = isRoot,
                WorldPosition = bone.position,
                WorldRotation = bone.rotation,
                LocalRotation = bone.localRotation
            });

            for (int i = 0; i < bone.childCount; i++)
            {
                Transform child = bone.GetChild(i);
                string childPath = BuildChildPath(pathFromRoot, child.name);
                CaptureRecursive(child, isRoot: false, childPath);
            }
        }

        private static string BuildChildPath(string parentPath, string childName)
        {
            return string.IsNullOrEmpty(parentPath) ? childName : parentPath + "/" + childName;
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
