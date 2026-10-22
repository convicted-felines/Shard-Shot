using System.Collections;
using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 死亡布偶实体数据。
    /// </summary>
    public sealed class RagdollEntityData : EntityData
    {
        public Vector3 Velocity;
        public string BoneRootName;
        public RagdollBonePoseSnapshot BonePoseSnapshot;
        public bool DetachLimbs;
        public Vector3 ForceOriginWorld;
        public float ScatterForce;
        public float HideDelay;

        public RagdollEntityData()
            : base(0, 0)
        {
        }

        public static RagdollEntityData Create(int entityId, Vector3 position, Quaternion rotation, Vector3 velocity,
            string boneRootName, RagdollBonePoseSnapshot bonePoseSnapshot, string assetName,
            bool detachLimbs, Vector3 forceOriginWorld, float scatterForce, float hideDelay)
        {
            var data = ReferencePool.Acquire<RagdollEntityData>();
            data.SetId(entityId);
            data.SetTypeId(0);
            data.Position = position;
            data.Rotation = rotation;
            data.Velocity = velocity;
            data.BoneRootName = boneRootName;
            data.BonePoseSnapshot = bonePoseSnapshot?.Clone();
            data.AssetName = assetName;
            data.EntityName = assetName;
            data.DetachLimbs = detachLimbs;
            data.ForceOriginWorld = forceOriginWorld;
            data.ScatterForce = scatterForce;
            data.HideDelay = hideDelay;
            return data;
        }

        public override void Clear()
        {
            base.Clear();
            Velocity = Vector3.zero;
            BoneRootName = null;
            BonePoseSnapshot?.Clear();
            BonePoseSnapshot = null;
            DetachLimbs = false;
            ForceOriginWorld = Vector3.zero;
            ScatterForce = 0f;
            HideDelay = 0f;
        }
    }

    /// <summary>
    /// 死亡布偶实体：采集角色姿态后启用刚体物理；散架模式下对各刚体施加冲量，关节断裂完全由预制体 breakForce/breakTorque 决定。
    /// </summary>
    public sealed class RagdollEntity : Entity
    {
        private const float CharacterJointProjectionDistance = 0.1f;
        private const float CharacterJointProjectionAngle = 180f;

        private RagdollEntityData m_Data;
        private Rigidbody m_HipsRigidbody;
        private Transform m_HipsTransform;
        private Animator m_Animator;
        private List<Rigidbody> m_CachedRigidbodies;
        private Dictionary<Joint, (Rigidbody connectedBody, float breakForce, float breakTorque)> m_DefaultJointState;
        private List<DefaultLocalPose> m_DefaultLocalPoses;
        private Coroutine m_ReleasePhysicsCoroutine;

        private struct DefaultLocalPose
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            CacheDefaultJointState();
            CacheDefaultLocalPoses();
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            m_Data = userData as RagdollEntityData;
            if (m_Data == null)
            {
                Log.Error("RagdollEntityData is invalid.");
                return;
            }

            StopReleasePhysicsCoroutine();

            if (!m_Data.DetachLimbs)
            {
                RestoreDefaultJointConnections();
            }

            m_HipsTransform = FindChildRecursive(CachedTransform, m_Data.BoneRootName);
            if (m_HipsTransform == null)
            {
                Log.Warning("RagdollEntity: bone root '{0}' not found, fallback to entity root.", m_Data.BoneRootName);
                m_HipsTransform = CachedTransform;
            }

            m_HipsRigidbody = m_HipsTransform.GetComponent<Rigidbody>();
            if (m_HipsRigidbody == null && m_HipsTransform != CachedTransform)
            {
                m_HipsRigidbody = CachedTransform.GetComponent<Rigidbody>();
            }

            m_Animator = CachedTransform.GetComponentInChildren<Animator>();
            if (m_Animator != null)
            {
                m_Animator.enabled = false;
            }

            ApplyCapturedPoseAndSchedulePhysicsRelease();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            StopReleasePhysicsCoroutine();
            ResetRagdollPhysicsForPool();

            if (m_Data == null || !m_Data.DetachLimbs)
            {
                RestoreDefaultJointConnections();
            }

            if (m_Animator != null)
            {
                m_Animator.enabled = true;
            }

            m_Data = null;
            m_HipsRigidbody = null;
            m_HipsTransform = null;
            m_Animator = null;
            base.OnHide(isShutdown, userData);
        }

        private void CacheDefaultJointState()
        {
            if (m_DefaultJointState != null)
            {
                return;
            }

            m_DefaultJointState = new Dictionary<Joint, (Rigidbody connectedBody, float breakForce, float breakTorque)>();
            Joint[] joints = CachedTransform.GetComponentsInChildren<Joint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                Joint joint = joints[i];
                m_DefaultJointState[joint] = (joint.connectedBody, joint.breakForce, joint.breakTorque);
            }
        }

        private void CacheDefaultLocalPoses()
        {
            if (m_DefaultLocalPoses != null)
            {
                return;
            }

            Transform[] transforms = CachedTransform.GetComponentsInChildren<Transform>(true);
            m_DefaultLocalPoses = new List<DefaultLocalPose>(transforms.Length);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];
                if (transform == CachedTransform)
                {
                    continue;
                }

                m_DefaultLocalPoses.Add(new DefaultLocalPose
                {
                    Transform = transform,
                    LocalPosition = transform.localPosition,
                    LocalRotation = transform.localRotation,
                    LocalScale = transform.localScale
                });
            }
        }

        private void ResetHierarchyToDefaultLocalPoses()
        {
            if (m_DefaultLocalPoses == null)
            {
                return;
            }

            for (int i = 0; i < m_DefaultLocalPoses.Count; i++)
            {
                DefaultLocalPose pose = m_DefaultLocalPoses[i];
                if (pose.Transform == null)
                {
                    continue;
                }

                pose.Transform.localPosition = pose.LocalPosition;
                pose.Transform.localRotation = pose.LocalRotation;
                pose.Transform.localScale = pose.LocalScale;
            }
        }

        private void RestoreDefaultJointConnections()
        {
            if (m_DefaultJointState == null)
            {
                return;
            }

            foreach (KeyValuePair<Joint, (Rigidbody connectedBody, float breakForce, float breakTorque)> entry in m_DefaultJointState)
            {
                Joint joint = entry.Key;
                if (joint == null)
                {
                    continue;
                }

                joint.connectedBody = entry.Value.connectedBody;
                joint.breakForce = entry.Value.breakForce;
                joint.breakTorque = entry.Value.breakTorque;
            }
        }

        private void ApplyCapturedPoseAndSchedulePhysicsRelease()
        {
            EnsureRigidbodyCache();
            PrepareRigidbodiesForPoseEdit(m_CachedRigidbodies);

            if (m_Data.DetachLimbs)
            {
                ResetHierarchyToDefaultLocalPoses();
            }

            bool poseApplied = m_Data.BonePoseSnapshot != null
                && m_Data.BonePoseSnapshot.TryApplyToRagdoll(CachedTransform, m_Data.BoneRootName);
            if (!poseApplied)
            {
                Log.Warning("RagdollEntity: failed to apply bone pose snapshot.");
            }

            Physics.SyncTransforms();
            SyncRigidbodiesFromTransforms(m_CachedRigidbodies);

            RealignJointAnchorsToCurrentPose();
            if (!m_Data.DetachLimbs)
            {
                ConfigureCharacterJointProjection();
            }

            m_ReleasePhysicsCoroutine = StartCoroutine(ReleasePhysicsAfterPoseSettled());
        }

        private IEnumerator ReleasePhysicsAfterPoseSettled()
        {
            yield return new WaitForFixedUpdate();
            Physics.SyncTransforms();

            if (m_Data != null && m_Data.DetachLimbs)
            {
                SyncRigidbodiesFromTransforms(m_CachedRigidbodies);
            }

            RealignJointAnchorsToCurrentPose();
            ReleaseRigidbodiesToSimulation(m_CachedRigidbodies);

            if (m_Data != null)
            {
                ApplyVelocity(m_Data.Velocity);
                if (m_Data.ScatterForce > 0f)
                {
                    ApplyScatterForce(m_Data.ForceOriginWorld, m_Data.ScatterForce);
                }

                if (m_Data.HideDelay > 0f)
                {
                    DelayHide(m_Data.HideDelay);
                }
            }

            m_ReleasePhysicsCoroutine = null;
        }

        private void StopReleasePhysicsCoroutine()
        {
            if (m_ReleasePhysicsCoroutine == null)
            {
                return;
            }

            StopCoroutine(m_ReleasePhysicsCoroutine);
            m_ReleasePhysicsCoroutine = null;
        }

        private void EnsureRigidbodyCache()
        {
            if (m_CachedRigidbodies == null)
            {
                m_CachedRigidbodies = new List<Rigidbody>(32);
            }
            else
            {
                m_CachedRigidbodies.Clear();
            }

            CachedTransform.GetComponentsInChildren(true, m_CachedRigidbodies);
        }

        private static void PrepareRigidbodiesForPoseEdit(List<Rigidbody> rigidbodies)
        {
            for (int i = 0; i < rigidbodies.Count; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }

        private static void SyncRigidbodiesFromTransforms(List<Rigidbody> rigidbodies)
        {
            for (int i = 0; i < rigidbodies.Count; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;
            }
        }

        private static void ReleaseRigidbodiesToSimulation(List<Rigidbody> rigidbodies)
        {
            for (int i = 0; i < rigidbodies.Count; i++)
            {
                Rigidbody rb = rigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = false;
            }

            Physics.SyncTransforms();
        }

        private void RealignJointAnchorsToCurrentPose()
        {
            Joint[] joints = CachedTransform.GetComponentsInChildren<Joint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                Joint joint = joints[i];
                Rigidbody connectedBody = joint.connectedBody;
                if (connectedBody == null)
                {
                    continue;
                }

                joint.autoConfigureConnectedAnchor = false;
                Vector3 anchorWorld = joint.transform.TransformPoint(joint.anchor);
                joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(anchorWorld);
            }
        }

        private void ConfigureCharacterJointProjection()
        {
            CharacterJoint[] joints = CachedTransform.GetComponentsInChildren<CharacterJoint>(true);
            for (int i = 0; i < joints.Length; i++)
            {
                CharacterJoint joint = joints[i];
                joint.enableProjection = true;
                joint.projectionDistance = CharacterJointProjectionDistance;
                joint.projectionAngle = CharacterJointProjectionAngle;
            }
        }

        private void ResetRagdollPhysicsForPool()
        {
            EnsureRigidbodyCache();
            PrepareRigidbodiesForPoseEdit(m_CachedRigidbodies);

            if (m_Data != null && m_Data.DetachLimbs)
            {
                ResetHierarchyToDefaultLocalPoses();
                SyncRigidbodiesFromTransforms(m_CachedRigidbodies);
            }

            Physics.SyncTransforms();
        }

        private void ApplyVelocity(Vector3 velocity)
        {
            if (m_Data != null && m_Data.DetachLimbs)
            {
                EnsureRigidbodyCache();
                for (int i = 0; i < m_CachedRigidbodies.Count; i++)
                {
                    Rigidbody rb = m_CachedRigidbodies[i];
                    if (rb != null)
                    {
                        rb.velocity = velocity;
                    }
                }

                return;
            }

            if (m_HipsRigidbody != null)
            {
                m_HipsRigidbody.velocity = velocity;
            }
        }

        private void ApplyScatterForce(Vector3 forceOriginWorld, float scatterForce)
        {
            EnsureRigidbodyCache();
            for (int i = 0; i < m_CachedRigidbodies.Count; i++)
            {
                Rigidbody rb = m_CachedRigidbodies[i];
                if (rb == null)
                {
                    continue;
                }

                Vector3 offset = rb.worldCenterOfMass - forceOriginWorld;
                if (offset.sqrMagnitude <= 0.0001f)
                {
                    offset = Vector3.up;
                }

                rb.AddForce(offset.normalized * scatterForce, ForceMode.Impulse);
            }
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
