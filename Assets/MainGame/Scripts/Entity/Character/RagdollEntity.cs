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
        /// <summary>击飞可恢复布偶绑定的角色实体 Id；死亡布偶为 0。</summary>
        public int OwnerEntityId;

        public RagdollEntityData()
            : base(0, 0)
        {
        }

        public static RagdollEntityData Create(int entityId, Vector3 position, Quaternion rotation, Vector3 velocity,
            string boneRootName, RagdollBonePoseSnapshot bonePoseSnapshot, string assetName,
            bool detachLimbs, Vector3 forceOriginWorld, float scatterForce, float hideDelay,
            int ownerEntityId = 0)
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
            data.OwnerEntityId = ownerEntityId;
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
            OwnerEntityId = 0;
        }
    }

    /// <summary>
    /// 布偶实体：采集角色姿态后启用刚体物理。
    /// 死亡散架（DetachLimbs）时由 <see cref="RagdollJointDetachConfig"/> 脚本断开指定关节并施加冲量；
    /// 击飞布偶保持关节连接，回池时还原 connectedBody。
    /// </summary>
    public sealed class RagdollEntity : Entity
    {
        private const float CharacterJointProjectionDistance = 0.1f;
        private const float CharacterJointProjectionAngle = 180f;

        private RagdollEntityData m_Data;
        private CharacterPrefabConfig m_PrefabConfig;
        private RagdollJointDetachConfig m_JointDetachConfig;
        private Rigidbody m_HipsRigidbody;
        private Transform m_HipsTransform;
        private int m_OwnerEntityId;

        /// <summary>布偶骨骼根（通常为 hips / RagBoneRoot），供摄像机等跟随。</summary>
        public Transform BoneRoot => m_HipsTransform;

        /// <summary>臀部骨骼，起身位姿推算用。</summary>
        public Transform HipsTransform => m_HipsTransform != null ? m_HipsTransform : CachedTransform;

        public int OwnerEntityId => m_OwnerEntityId;

        public CharacterPrefabConfig PrefabConfig => m_PrefabConfig;

        private Animator m_Animator;
        private List<Rigidbody> m_CachedRigidbodies;
        private List<CharacterJointDefaultState> m_DefaultJointStates;
        private List<DefaultLocalPose> m_DefaultLocalPoses;
        private Coroutine m_ReleasePhysicsCoroutine;

        private struct DefaultLocalPose
        {
            public Transform Transform;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        private struct CharacterJointDefaultState
        {
            public Transform Transform;
            public Rigidbody ConnectedBody;
            public float BreakForce;
            public float BreakTorque;
            public Vector3 Anchor;
            public Vector3 Axis;
            public bool AutoConfigureConnectedAnchor;
            public Vector3 ConnectedAnchor;
            public Vector3 SwingAxis;
            public SoftJointLimit LowTwistLimit;
            public SoftJointLimit HighTwistLimit;
            public SoftJointLimit Swing1Limit;
            public SoftJointLimit Swing2Limit;
            public bool EnableProjection;
            public float ProjectionDistance;
            public float ProjectionAngle;
            public bool EnableCollision;
            public bool EnablePreprocessing;
            public float MassScale;
            public float ConnectedMassScale;

            public static CharacterJointDefaultState Capture(CharacterJoint joint)
            {
                return new CharacterJointDefaultState
                {
                    Transform = joint.transform,
                    ConnectedBody = joint.connectedBody,
                    BreakForce = joint.breakForce,
                    BreakTorque = joint.breakTorque,
                    Anchor = joint.anchor,
                    Axis = joint.axis,
                    AutoConfigureConnectedAnchor = joint.autoConfigureConnectedAnchor,
                    ConnectedAnchor = joint.connectedAnchor,
                    SwingAxis = joint.swingAxis,
                    LowTwistLimit = joint.lowTwistLimit,
                    HighTwistLimit = joint.highTwistLimit,
                    Swing1Limit = joint.swing1Limit,
                    Swing2Limit = joint.swing2Limit,
                    EnableProjection = joint.enableProjection,
                    ProjectionDistance = joint.projectionDistance,
                    ProjectionAngle = joint.projectionAngle,
                    EnableCollision = joint.enableCollision,
                    EnablePreprocessing = joint.enablePreprocessing,
                    MassScale = joint.massScale,
                    ConnectedMassScale = joint.connectedMassScale,
                };
            }

            public void Restore()
            {
                if (Transform == null)
                {
                    return;
                }

                CharacterJoint joint = Transform.GetComponent<CharacterJoint>();
                if (joint == null)
                {
                    joint = Transform.gameObject.AddComponent<CharacterJoint>();
                }

                joint.connectedBody = ConnectedBody;
                joint.breakForce = BreakForce;
                joint.breakTorque = BreakTorque;
                joint.anchor = Anchor;
                joint.axis = Axis;
                joint.autoConfigureConnectedAnchor = AutoConfigureConnectedAnchor;
                joint.connectedAnchor = ConnectedAnchor;
                joint.swingAxis = SwingAxis;
                joint.lowTwistLimit = LowTwistLimit;
                joint.highTwistLimit = HighTwistLimit;
                joint.swing1Limit = Swing1Limit;
                joint.swing2Limit = Swing2Limit;
                joint.enableProjection = EnableProjection;
                joint.projectionDistance = ProjectionDistance;
                joint.projectionAngle = ProjectionAngle;
                joint.enableCollision = EnableCollision;
                joint.enablePreprocessing = EnablePreprocessing;
                joint.massScale = MassScale;
                joint.connectedMassScale = ConnectedMassScale;
            }
        }

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_PrefabConfig = GetComponent<CharacterPrefabConfig>();
            m_JointDetachConfig = GetComponent<RagdollJointDetachConfig>();
            m_JointDetachConfig?.EnsureBoneTransformsCached();
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

            m_OwnerEntityId = m_Data.OwnerEntityId;
            StopReleasePhysicsCoroutine();
            RestoreDefaultJointConnections();
            Physics.SyncTransforms();

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

            SetupOwnerDamageProxies();
            ApplyCapturedPoseAndSchedulePhysicsRelease();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            CharacterHitFlashComponent flash = GetComponent<CharacterHitFlashComponent>();
            flash?.Clear();

            CharacterSustainedDimComponent dim = GetComponent<CharacterSustainedDimComponent>();
            dim?.Clear();

            StopReleasePhysicsCoroutine();
            ClearOwnerDamageProxies();
            ResetRagdollPhysicsForPool();
            RestoreDefaultJointConnections();

            if (m_Animator != null)
            {
                m_Animator.enabled = true;
            }

            m_Data = null;
            m_OwnerEntityId = 0;
            m_HipsRigidbody = null;
            m_HipsTransform = null;
            m_Animator = null;
            base.OnHide(isShutdown, userData);
        }

        public bool IsBoundToOwner(int ownerEntityId)
        {
            return ownerEntityId != 0 && m_OwnerEntityId == ownerEntityId;
        }

        public bool TryGetHipsWorldPosition(out Vector3 worldPosition)
        {
            if (m_HipsTransform == null)
            {
                worldPosition = default;
                return false;
            }

            worldPosition = m_HipsTransform.position;
            return true;
        }

        /// <summary>对已模拟的击飞布偶施加爆炸冲量。</summary>
        public void ApplyExplosionScatterForce(Vector3 forceOriginWorld, float scatterForce)
        {
            if (scatterForce <= 0f)
            {
                return;
            }

            ApplyScatterForce(forceOriginWorld, scatterForce);
        }

        /// <summary>将击飞布偶就地转为死亡散架：断开关节、施加冲量、解除角色绑定。</summary>
        public void ConvertToDeathScatter(Vector3 forceOriginWorld, float scatterForce, float hideDelay)
        {
            ClearOwnerDamageProxies();
            DetachConfiguredJoints();
            Physics.SyncTransforms();
            ApplyScatterForce(forceOriginWorld, scatterForce);

            m_OwnerEntityId = 0;
            if (m_Data != null)
            {
                m_Data.OwnerEntityId = 0;
                m_Data.DetachLimbs = true;
            }

            if (hideDelay > 0f)
            {
                DelayHide(hideDelay);
            }
        }

        private void SetupOwnerDamageProxies()
        {
            if (m_OwnerEntityId == 0)
            {
                return;
            }

            Collider[] colliders = CachedTransform.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || collider.isTrigger)
                {
                    continue;
                }

                RagdollOwnerDamageProxy proxy = collider.GetComponent<RagdollOwnerDamageProxy>();
                if (proxy == null)
                {
                    proxy = collider.gameObject.AddComponent<RagdollOwnerDamageProxy>();
                }

                proxy.Initialize(m_OwnerEntityId);

                if (Constant.Layer.CharacterLayerId >= 0)
                {
                    collider.gameObject.layer = Constant.Layer.CharacterLayerId;
                }
            }
        }

        private void ClearOwnerDamageProxies()
        {
            RagdollOwnerDamageProxy[] proxies = CachedTransform.GetComponentsInChildren<RagdollOwnerDamageProxy>(true);
            for (int i = 0; i < proxies.Length; i++)
            {
                if (proxies[i] != null)
                {
                    proxies[i].Clear();
                }
            }
        }

        private void CacheDefaultJointState()
        {
            if (m_DefaultJointStates != null)
            {
                return;
            }

            CharacterJoint[] joints = CachedTransform.GetComponentsInChildren<CharacterJoint>(true);
            m_DefaultJointStates = new List<CharacterJointDefaultState>(joints.Length);
            for (int i = 0; i < joints.Length; i++)
            {
                m_DefaultJointStates.Add(CharacterJointDefaultState.Capture(joints[i]));
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
            if (m_DefaultJointStates == null)
            {
                return;
            }

            for (int i = 0; i < m_DefaultJointStates.Count; i++)
            {
                m_DefaultJointStates[i].Restore();
            }
        }

        private void DetachConfiguredJoints()
        {
            if (m_JointDetachConfig != null)
            {
                m_JointDetachConfig.DetachConfiguredJoints();
                return;
            }

            Log.Warning(
                "RagdollEntity: DetachLimbs requested but RagdollJointDetachConfig is missing on '{0}'.",
                Name);
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

            if (m_Data != null && m_Data.DetachLimbs)
            {
                DetachConfiguredJoints();
                yield return new WaitForEndOfFrame();
                Physics.SyncTransforms();
            }

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
