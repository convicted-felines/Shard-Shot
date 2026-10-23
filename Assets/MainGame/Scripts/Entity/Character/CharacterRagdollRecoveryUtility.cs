using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 击飞布偶起身位姿计算：由布偶臀部推算角色根节点，布偶骨骼缓动拟合到起身动画第一帧后播放动画。
    /// </summary>
    public static class CharacterRagdollRecoveryUtility
    {
        public const string ProneGetUpAnimatorStateName = "趴起身";
        public const string SitGetUpAnimatorStateName = "坐起身";

        private const float RecoverBelowGroundProbeSky = 40f;
        private const float RecoverBelowGroundProbeDown = 250f;
        private const float RecoverBelowGroundEpsilon = 0.12f;
        private const float RecoverGroundMinNormalY = 0.35f;
        private const float VerticalForwardThreshold = 0.6f;
        private const float RecoveryYawCandidateMergeAngle = 5f;

        /// <summary>
        /// 根据布偶臀部 Z 轴朝向选择起身动画：朝向上用坐起身，朝向地面用趴起身。
        /// </summary>
        public static string ResolveGetUpAnimatorStateName(RagdollEntity ragdoll)
        {
            Transform hips = ragdoll?.HipsTransform;
            if (hips == null)
            {
                return SitGetUpAnimatorStateName;
            }

            float upComponent = Vector3.Dot(hips.forward, Vector3.up);
            return upComponent >= 0f
                ? SitGetUpAnimatorStateName
                : ProneGetUpAnimatorStateName;
        }

        /// <summary>
        /// 采集布偶姿态、对齐角色根节点并恢复显示；返回布偶快照与起身动画第一帧快照供后续混合。
        /// </summary>
        public static bool TryPrepareRecoverPoseBlend(
            CharacterEntity character,
            RagdollEntity ragdoll,
            string getUpAnimatorStateName,
            out RagdollBonePoseSnapshot ragdollPose,
            out RagdollBonePoseSnapshot animFirstFramePose)
        {
            ragdollPose = null;
            animFirstFramePose = null;
            if (character == null)
            {
                return false;
            }

            string boneRootName = character.GetRagBoneRootName();
            if (ragdoll != null)
            {
                ragdollPose = RagdollBonePoseSnapshot.CaptureFromHierarchy(ragdoll.CachedTransform, boneRootName);
            }

            CharacterPrefabConfig config = character.PrefabConfig;
            Vector3 ragdollHipsWorld = character.CachedTransform.position;
            if (ragdoll != null && ragdoll.TryGetHipsWorldPosition(out Vector3 hipsWorld))
            {
                ragdollHipsWorld = hipsWorld;
            }

            if (ragdoll != null)
            {
                GameEntry.Entity.HideEntity(ragdoll);
            }

            character.SetAnimatorEnabledForPoseBlend(true);
            Quaternion recoveryRotation = ResolveBestRecoveryWorldRotation(
                character,
                ragdoll,
                getUpAnimatorStateName,
                boneRootName,
                ragdollPose,
                config,
                ragdollHipsWorld);
            Vector3 recoveryPosition = ResolveRecoveryWorldPositionFromHips(
                character.CachedTransform,
                config,
                ragdollHipsWorld,
                recoveryRotation);

            character.SetCharacterWorldPose(recoveryPosition, recoveryRotation);
            animFirstFramePose = character.SampleGetUpAnimationFirstFrame(getUpAnimatorStateName, boneRootName);

            Transform hipBone = FindHipBoneTransform(character.CachedTransform, config);
            if (hipBone != null && animFirstFramePose != null && animFirstFramePose.IsValid)
            {
                Vector3 hipLocalOffset = character.CachedTransform.InverseTransformPoint(hipBone.position);
                recoveryPosition = ragdollHipsWorld - recoveryRotation * hipLocalOffset;
                character.SetCharacterWorldPose(recoveryPosition, recoveryRotation);
                animFirstFramePose = character.SampleGetUpAnimationFirstFrame(getUpAnimatorStateName, boneRootName);
            }

            if (ragdollPose == null
                || !ragdollPose.IsValid
                || animFirstFramePose == null
                || !animFirstFramePose.IsValid)
            {
                character.SetKnockdownPresentationActive(true);
                return false;
            }

            character.SetKnockdownPresentationActive(true);
            ragdollPose.TryApplyToHierarchy(character.CachedTransform, boneRootName);
            ragdollPose = RagdollBonePoseSnapshot.CaptureFromHierarchy(character.CachedTransform, boneRootName);
            character.SetAnimatorEnabledForPoseBlend(false);
            return ragdollPose.IsValid && animFirstFramePose.IsValid;
        }

        public static bool TryApplyRecoverPoseBlend(
            CharacterEntity character,
            RagdollBonePoseSnapshot ragdollPose,
            RagdollBonePoseSnapshot animFirstFramePose,
            float blend01)
        {
            if (character == null || ragdollPose == null || animFirstFramePose == null)
            {
                return false;
            }

            string boneRootName = character.GetRagBoneRootName();
            if (blend01 >= 1f)
            {
                return animFirstFramePose.TryApplyExactToHierarchy(character.CachedTransform, boneRootName);
            }

            return RagdollBonePoseSnapshot.TryApplyBlend(
                character.CachedTransform,
                boneRootName,
                ragdollPose,
                animFirstFramePose,
                blend01);
        }

        public static Vector3 ResolveRecoveryWorldPositionFromHips(
            Transform characterRoot,
            CharacterPrefabConfig config,
            Vector3 ragdollHipsWorld,
            Quaternion recoveryRotation)
        {
            Vector3 hipLocalOffset = ResolveHipLocalOffsetFromRoot(characterRoot, config);
            return ragdollHipsWorld - recoveryRotation * hipLocalOffset;
        }

        /// <summary>
        /// 平面地图：将根节点 XZ 保持不变，Y 强制为 0。
        /// </summary>
        public static bool TryGetGroundedRootPosition(Vector3 rootWorld, out Vector3 groundedRoot)
        {
            groundedRoot = new Vector3(rootWorld.x, 0f, rootWorld.z);
            return true;
        }

        public static Vector3 ResolveHipLocalOffsetFromRoot(Transform characterRoot, CharacterPrefabConfig config)
        {
            Transform hipBone = FindHipBoneTransform(characterRoot, config);
            return hipBone != null
                ? characterRoot.InverseTransformPoint(hipBone.position)
                : Vector3.zero;
        }

        /// <summary>
        /// 在多个水平朝候选中，选取与布偶臀部世界旋转最接近的起身动画第一帧对应根 yaw。
        /// </summary>
        private static Quaternion ResolveBestRecoveryWorldRotation(
            CharacterEntity character,
            RagdollEntity ragdoll,
            string getUpAnimatorStateName,
            string boneRootName,
            RagdollBonePoseSnapshot ragdollPose,
            CharacterPrefabConfig config,
            Vector3 ragdollHipsWorld)
        {
            Quaternion fallbackRotation = ExtractUprightYawRotation(character.CachedTransform.rotation);
            if (ragdoll == null || ragdoll.HipsTransform == null || ragdollPose == null || !ragdollPose.IsValid)
            {
                return fallbackRotation;
            }

            if (!ragdollPose.TryGetRootWorldPose(out _, out Quaternion ragdollHipsRotation))
            {
                return ResolveRecoveryWorldRotation(fallbackRotation, ragdoll);
            }

            List<Quaternion> candidates = BuildRecoveryYawCandidates(ragdoll, fallbackRotation);
            Quaternion bestRotation = fallbackRotation;
            float bestAngle = float.MaxValue;
            Vector3 sharedPosition = ResolveRecoveryWorldPositionFromHips(
                character.CachedTransform,
                config,
                ragdollHipsWorld,
                fallbackRotation);

            for (int i = 0; i < candidates.Count; i++)
            {
                Quaternion candidateRotation = candidates[i];
                character.SetCharacterWorldPose(sharedPosition, candidateRotation);
                RagdollBonePoseSnapshot candidatePose = character.SampleGetUpAnimationFirstFrame(
                    getUpAnimatorStateName,
                    boneRootName);
                if (!candidatePose.TryGetRootWorldPose(out _, out Quaternion candidateHipsRotation))
                {
                    continue;
                }

                float angle = Quaternion.Angle(ragdollHipsRotation, candidateHipsRotation);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestRotation = candidateRotation;
                }
            }

            return bestRotation;
        }

        private static List<Quaternion> BuildRecoveryYawCandidates(RagdollEntity ragdoll, Quaternion fallbackRotation)
        {
            var candidates = new List<Quaternion>(8);
            Transform hips = ragdoll.HipsTransform;
            float forwardUp = Vector3.Dot(hips.forward, Vector3.up);
            if (Mathf.Abs(forwardUp) >= VerticalForwardThreshold)
            {
                TryAddRecoveryYawCandidate(candidates, hips.up);
                TryAddRecoveryYawCandidate(candidates, -hips.up);
                TryAddRecoveryYawCandidate(candidates, hips.right);
                TryAddRecoveryYawCandidate(candidates, -hips.right);
            }
            else
            {
                TryAddRecoveryYawCandidate(candidates, hips.forward);
                TryAddRecoveryYawCandidate(candidates, -hips.forward);
                TryAddRecoveryYawCandidate(candidates, hips.right);
                TryAddRecoveryYawCandidate(candidates, -hips.right);
            }

            TryAddRecoveryYawCandidate(candidates, fallbackRotation * Vector3.forward);
            return candidates;
        }

        private static void TryAddRecoveryYawCandidate(List<Quaternion> candidates, Vector3 worldDirection)
        {
            if (!TryGetHorizontalLookRotation(worldDirection, out Quaternion lookRotation))
            {
                return;
            }

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Quaternion.Angle(candidates[i], lookRotation) < RecoveryYawCandidateMergeAngle)
                {
                    return;
                }
            }

            candidates.Add(lookRotation);
        }

        public static Quaternion ResolveRecoveryWorldRotation(Quaternion fallbackRotation, RagdollEntity ragdoll)
        {
            if (ragdoll != null && ragdoll.HipsTransform != null)
            {
                Transform hips = ragdoll.HipsTransform;
                float forwardUp = Vector3.Dot(hips.forward, Vector3.up);
                if (Mathf.Abs(forwardUp) >= VerticalForwardThreshold)
                {
                    if (TryGetHorizontalLookRotation(hips.up, out Quaternion fromUp))
                    {
                        return fromUp;
                    }

                    if (TryGetHorizontalLookRotation(-hips.up, out Quaternion fromNegUp))
                    {
                        return fromNegUp;
                    }

                    if (TryGetHorizontalLookRotation(hips.right, out Quaternion fromRight))
                    {
                        return fromRight;
                    }

                    if (TryGetHorizontalLookRotation(-hips.right, out Quaternion fromNegRight))
                    {
                        return fromNegRight;
                    }
                }
                else
                {
                    if (TryGetHorizontalLookRotation(hips.forward, out Quaternion fromForward))
                    {
                        return fromForward;
                    }

                    if (TryGetHorizontalLookRotation(hips.right, out Quaternion fromRight))
                    {
                        return fromRight;
                    }
                }
            }

            return ExtractUprightYawRotation(fallbackRotation);
        }

        private static Transform FindHipBoneTransform(Transform root, CharacterPrefabConfig config)
        {
            if (root == null)
            {
                return null;
            }

            if (config != null && config.RagBoneRoot != null)
            {
                Transform hips = FindChildRecursive(root, config.RagBoneRoot.name);
                if (hips != null)
                {
                    return hips;
                }
            }

            return root;
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

        private static Quaternion ExtractUprightYawRotation(Quaternion source)
        {
            Vector3 euler = source.eulerAngles;
            return Quaternion.Euler(0f, euler.y, 0f);
        }

        private static bool TryGetHorizontalLookRotation(Vector3 worldDirection, out Quaternion lookRotation)
        {
            Vector3 flat = worldDirection;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f)
            {
                lookRotation = Quaternion.identity;
                return false;
            }

            lookRotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            return true;
        }

        private static Vector3 SnapRecoveryRootToGround(Vector3 rootWorld)
        {
            return TryGetGroundedRootPosition(rootWorld, out Vector3 groundedRoot)
                ? groundedRoot
                : rootWorld;
        }
    }
}
