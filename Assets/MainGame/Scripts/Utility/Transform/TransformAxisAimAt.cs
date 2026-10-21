using UnityEngine;

namespace MainGame
{
    /// <summary>
    /// 将本物体某一局部轴对齐到「自身位置 → 目标位置」的方向，用于自行车小关节等视觉模拟。
    /// 可选单轴旋转约束，仅绕指定轴转动（铰链式）。
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class TransformAxisAimAt : MonoBehaviour
    {
        public enum LocalAxisDirection
        {
            PositiveX,
            NegativeX,
            PositiveY,
            NegativeY,
            PositiveZ,
            NegativeZ,
        }

        public enum TwistUpMode
        {
            ParentUp,
            WorldUp,
            TargetUp,
            CustomTransformUp,
            CustomWorldDirection,
        }

        public enum AimUpdatePhase
        {
            Update,
            LateUpdate,
            FixedUpdate,
        }

        public enum RotationConstraintMode
        {
            None,
            SingleAxis,
        }

        public enum AxisReferenceSpace
        {
            SelfLocal,
            ParentLocal,
            CustomTransformLocal,
            WorldDirection,
        }

        private static readonly Vector3 s_WorldUp = Vector3.up;

        [Header("引用")]
        [SerializeField]
        [Tooltip("要指向的目标；为空时不更新旋转")]
        private Transform m_target;

        [SerializeField]
        [Tooltip("可选：瞄准起点；为空时使用本物体 position")]
        private Transform m_aimOrigin;

        [Header("轴向")]
        [SerializeField]
        [Tooltip("需要对齐到目标方向的局部轴")]
        private LocalAxisDirection m_localAxis = LocalAxisDirection.NegativeZ;

        [SerializeField]
        [Tooltip("绕瞄准轴的 twist 参考（避免 LookRotation 万向节翻转）；单轴约束模式下可忽略")]
        private TwistUpMode m_twistUpMode = TwistUpMode.ParentUp;

        [SerializeField]
        [Tooltip("TwistUpMode 为 CustomTransformUp 时使用")]
        private Transform m_twistUpTransform;

        [SerializeField]
        [Tooltip("TwistUpMode 为 CustomWorldDirection 时使用（世界空间）")]
        private Vector3 m_twistUpWorldDirection = Vector3.up;

        [Header("旋转约束")]
        [SerializeField]
        [Tooltip("None：自由旋转；SingleAxis：仅绕指定轴旋转")]
        private RotationConstraintMode m_rotationConstraint = RotationConstraintMode.None;

        [SerializeField]
        [Tooltip("单轴约束时的转动轴")]
        private LocalAxisDirection m_hingeAxis = LocalAxisDirection.PositiveX;

        [SerializeField]
        [Tooltip("转动轴所在空间")]
        private AxisReferenceSpace m_hingeAxisSpace = AxisReferenceSpace.ParentLocal;

        [SerializeField]
        [Tooltip("HingeAxisSpace 为 CustomTransformLocal 时使用")]
        private Transform m_hingeAxisReference;

        [SerializeField]
        [Tooltip("HingeAxisSpace 为 WorldDirection 时使用（世界空间）")]
        private Vector3 m_hingeAxisWorldDirection = Vector3.right;

        [SerializeField]
        [Tooltip("单轴约束基准局部旋转的参考父级；为空时使用直接父物体；无父级时使用启用瞬间的世界旋转")]
        private Transform m_rotationBaseReference;

        [Header("更新")]
        [SerializeField]
        private AimUpdatePhase m_updatePhase = AimUpdatePhase.LateUpdate;

        [SerializeField]
        [Tooltip("旋转平滑时间（秒）；0 表示立即对齐")]
        [Min(0f)]
        private float m_rotationSmoothTime;

        [SerializeField]
        [Tooltip("目标与本物体距离小于该值时不更新（米）")]
        [Min(0f)]
        private float m_minAimDistance = 0.0001f;

        [SerializeField]
        [Tooltip("关闭后不再驱动旋转")]
        private bool m_aimEnabled = true;

        private Quaternion m_baseLocalRotation = Quaternion.identity;
        private Quaternion m_baseWorldRotation = Quaternion.identity;
        private bool m_hasBaseRotation;
        private bool m_useWorldBaseRotation;

        /// <summary> 运行时替换瞄准目标。 </summary>
        public Transform Target
        {
            get => m_target;
            set => m_target = value;
        }

        /// <summary> 是否启用瞄准。 </summary>
        public bool AimEnabled
        {
            get => m_aimEnabled;
            set => m_aimEnabled = value;
        }

        private void OnEnable()
        {
            CaptureBaseRotation();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
                CaptureBaseRotation();
        }
#endif

        private void CaptureBaseRotation()
        {
            Transform baseReference = ResolveRotationBaseReference();
            if (baseReference != null)
            {
                m_baseLocalRotation = transform.localRotation;
                m_useWorldBaseRotation = false;
            }
            else
            {
                m_baseWorldRotation = transform.rotation;
                m_useWorldBaseRotation = true;
            }

            m_hasBaseRotation = true;
        }

        private Transform ResolveRotationBaseReference()
        {
            if (m_rotationBaseReference != null)
                return m_rotationBaseReference;

            return transform.parent;
        }

        private void Update()
        {
            if (m_updatePhase == AimUpdatePhase.Update)
                ApplyAim(Time.deltaTime);
        }

        private void LateUpdate()
        {
            if (m_updatePhase == AimUpdatePhase.LateUpdate)
                ApplyAim(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (m_updatePhase == AimUpdatePhase.FixedUpdate)
                ApplyAim(Time.fixedDeltaTime);
        }

        private void ApplyAim(float deltaTime)
        {
            if (!m_aimEnabled || m_target == null)
                return;

            if (!m_hasBaseRotation)
                CaptureBaseRotation();

            Vector3 origin = m_aimOrigin != null ? m_aimOrigin.position : transform.position;
            Vector3 toTarget = m_target.position - origin;
            if (toTarget.sqrMagnitude < m_minAimDistance * m_minAimDistance)
                return;

            Vector3 aimDirection = toTarget.normalized;
            Quaternion desiredRotation = m_rotationConstraint == RotationConstraintMode.SingleAxis
                ? ComputeSingleAxisConstrainedRotation(aimDirection)
                : ComputeUnconstrainedRotation(aimDirection);

            if (m_rotationSmoothTime <= 0f)
            {
                transform.rotation = desiredRotation;
                return;
            }

            float blend = 1f - Mathf.Exp(-deltaTime / m_rotationSmoothTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }

        private Quaternion ComputeUnconstrainedRotation(Vector3 aimDirection)
        {
            Vector3 twistUp = ResolveTwistUp(aimDirection);
            return ComputeWorldRotation(aimDirection, GetLocalAxisVector(m_localAxis), twistUp);
        }

        private Quaternion ComputeSingleAxisConstrainedRotation(Vector3 aimDirection)
        {
            Quaternion baseWorldRotation = ResolveBaseWorldRotation();
            Vector3 hingeAxisWorld = ResolveHingeAxisWorld();
            if (hingeAxisWorld.sqrMagnitude < 1e-8f)
                return baseWorldRotation;

            hingeAxisWorld.Normalize();
            return ComputeSingleAxisConstrainedRotation(
                baseWorldRotation,
                GetLocalAxisVector(m_localAxis),
                hingeAxisWorld,
                aimDirection);
        }

        private Quaternion ResolveBaseWorldRotation()
        {
            if (m_useWorldBaseRotation)
                return m_baseWorldRotation;

            Transform baseReference = ResolveRotationBaseReference();
            return baseReference != null
                ? baseReference.rotation * m_baseLocalRotation
                : m_baseLocalRotation;
        }

        private Vector3 ResolveHingeAxisWorld()
        {
            switch (m_hingeAxisSpace)
            {
                case AxisReferenceSpace.SelfLocal:
                    return transform.TransformDirection(GetLocalAxisVector(m_hingeAxis));

                case AxisReferenceSpace.ParentLocal:
                    if (transform.parent != null)
                        return transform.parent.TransformDirection(GetLocalAxisVector(m_hingeAxis));
                    break;

                case AxisReferenceSpace.CustomTransformLocal:
                    if (m_hingeAxisReference != null)
                        return m_hingeAxisReference.TransformDirection(GetLocalAxisVector(m_hingeAxis));
                    break;

                case AxisReferenceSpace.WorldDirection:
                    return m_hingeAxisWorldDirection;
            }

            return GetLocalAxisVector(m_hingeAxis);
        }

        /// <summary>
        /// 在仅允许绕 <paramref name="hingeAxisWorld"/> 旋转的前提下，使局部瞄准轴尽量指向 <paramref name="targetDirectionWorld"/>。
        /// </summary>
        public static Quaternion ComputeSingleAxisConstrainedRotation(
            Quaternion baseWorldRotation,
            Vector3 localAimAxis,
            Vector3 hingeAxisWorld,
            Vector3 targetDirectionWorld)
        {
            if (hingeAxisWorld.sqrMagnitude < 1e-8f || targetDirectionWorld.sqrMagnitude < 1e-8f)
                return baseWorldRotation;

            Vector3 hingeAxis = hingeAxisWorld.normalized;
            Vector3 targetDirection = targetDirectionWorld.normalized;
            Vector3 restAimDirection = (baseWorldRotation * localAimAxis).normalized;

            Vector3 from = Vector3.ProjectOnPlane(restAimDirection, hingeAxis);
            Vector3 to = Vector3.ProjectOnPlane(targetDirection, hingeAxis);
            if (from.sqrMagnitude < 1e-8f || to.sqrMagnitude < 1e-8f)
                return baseWorldRotation;

            from.Normalize();
            to.Normalize();
            float angle = Vector3.SignedAngle(from, to, hingeAxis);
            return Quaternion.AngleAxis(angle, hingeAxis) * baseWorldRotation;
        }

        private Vector3 ResolveTwistUp(Vector3 aimDirection)
        {
            switch (m_twistUpMode)
            {
                case TwistUpMode.WorldUp:
                    return ResolveTwistUpFallback(aimDirection, s_WorldUp);

                case TwistUpMode.TargetUp:
                    return ResolveTwistUpFallback(aimDirection, m_target != null ? m_target.up : s_WorldUp);

                case TwistUpMode.CustomTransformUp:
                    if (m_twistUpTransform != null)
                        return ResolveTwistUpFallback(aimDirection, m_twistUpTransform.up);
                    break;

                case TwistUpMode.CustomWorldDirection:
                    if (m_twistUpWorldDirection.sqrMagnitude > 1e-8f)
                        return ResolveTwistUpFallback(aimDirection, m_twistUpWorldDirection.normalized);
                    break;

                default:
                    if (transform.parent != null)
                        return ResolveTwistUpFallback(aimDirection, transform.parent.up);
                    break;
            }

            return ResolveTwistUpFallback(aimDirection, s_WorldUp);
        }

        private static Vector3 ResolveTwistUpFallback(Vector3 aimDirection, Vector3 referenceUp)
        {
            if (Vector3.Cross(aimDirection, referenceUp).sqrMagnitude >= 1e-6f)
                return referenceUp;

            Vector3 fallback = Vector3.Cross(aimDirection, Vector3.right);
            if (fallback.sqrMagnitude >= 1e-6f)
                return Vector3.right;

            return Vector3.forward;
        }

        private static Vector3 GetLocalAxisVector(LocalAxisDirection axis)
        {
            switch (axis)
            {
                case LocalAxisDirection.PositiveX:
                    return Vector3.right;
                case LocalAxisDirection.NegativeX:
                    return Vector3.left;
                case LocalAxisDirection.PositiveY:
                    return Vector3.up;
                case LocalAxisDirection.NegativeY:
                    return Vector3.down;
                case LocalAxisDirection.NegativeZ:
                    return Vector3.back;
                default:
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// 使 <paramref name="localAxisNormalized"/>（局部空间）对齐到 <paramref name="worldDirectionNormalized"/>（世界空间）。
        /// </summary>
        public static Quaternion ComputeWorldRotation(Vector3 worldDirectionNormalized, Vector3 localAxisNormalized, Vector3 twistReferenceUp)
        {
            Vector3 direction = worldDirectionNormalized.sqrMagnitude > 1e-8f ? worldDirectionNormalized.normalized : Vector3.forward;
            Vector3 axis = localAxisNormalized.sqrMagnitude > 1e-8f ? localAxisNormalized.normalized : Vector3.forward;
            Vector3 up = twistReferenceUp.sqrMagnitude > 1e-8f ? twistReferenceUp.normalized : s_WorldUp;

            Quaternion alignForward = Quaternion.LookRotation(direction, up);
            Quaternion localAxisToForward = Quaternion.FromToRotation(axis, Vector3.forward);
            return alignForward * localAxisToForward;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (m_target == null)
                return;

            Vector3 origin = m_aimOrigin != null ? m_aimOrigin.position : transform.position;
            Vector3 axisWorld = transform.rotation * GetLocalAxisVector(m_localAxis);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(origin, m_target.position);
            Gizmos.DrawSphere(origin, 0.008f);
            Gizmos.DrawSphere(m_target.position, 0.008f);

            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin, axisWorld * 0.12f);

            if (m_rotationConstraint == RotationConstraintMode.SingleAxis)
            {
                Vector3 hingeAxisWorld = ResolveHingeAxisWorld();
                if (hingeAxisWorld.sqrMagnitude > 1e-8f)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawRay(origin, hingeAxisWorld.normalized * 0.15f);
                }
            }
        }
#endif
    }
}
