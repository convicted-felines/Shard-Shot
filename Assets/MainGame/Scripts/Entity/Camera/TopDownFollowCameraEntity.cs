using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 俯视角跟随摄像机。脚本需预先挂在预制体上，参数在 Inspector 调整。
    /// </summary>
    public class TopDownFollowCameraEntity : Entity
    {
        private static readonly List<UnityGameFramework.Runtime.Entity> s_EntityBuffer =
            new List<UnityGameFramework.Runtime.Entity>(8);

        [SerializeField]
        private Vector3 m_FollowOffset = new Vector3(0f, 12f, -8f);

        [SerializeField]
        private float m_FollowSmoothSpeed = 8f;

        [SerializeField]
        private float m_LookAtHeightOffset = 1f;

        private int m_FollowEntityId;
        private Transform m_FollowTarget;
        private Camera m_Camera;
        private Quaternion m_FixedRotation;
        private Vector3 m_SmoothFollowPosition;
        private readonly List<ShakeImpulse> m_ShakeImpulses = new List<ShakeImpulse>(4);

        private struct ShakeImpulse
        {
            public float Amplitude;
            public float Duration;
            public float Frequency;
            public float Elapsed;
            public float SeedX;
            public float SeedY;
            public float SeedZ;
        }

        /// <summary>
        /// 叠加一次衰减 Perlin 震屏。
        /// </summary>
        public void AddImpulse(float amplitude, float duration, float frequency = 25f)
        {
            if (amplitude <= 0f || duration <= 0f)
            {
                return;
            }

            m_ShakeImpulses.Add(new ShakeImpulse
            {
                Amplitude = amplitude,
                Duration = duration,
                Frequency = frequency,
                Elapsed = 0f,
                SeedX = Random.Range(0f, 100f),
                SeedY = Random.Range(0f, 100f),
                SeedZ = Random.Range(0f, 100f),
            });
        }

        /// <summary>
        /// 切换跟随实体；布偶实体会优先跟随其 BoneRoot（RagBoneRoot）。
        /// </summary>
        public void SetFollowEntity(int entityId)
        {
            m_FollowEntityId = entityId;
            m_FollowTarget = null;
            TryBindFollowTarget();
        }

        /// <summary>
        /// 查找当前已加载的俯视角跟随摄像机。
        /// </summary>
        public static bool TryGetActive(out TopDownFollowCameraEntity camera)
        {
            camera = null;
            if (GameEntry.Entity == null)
            {
                return false;
            }

            s_EntityBuffer.Clear();
            GameEntry.Entity.GetAllLoadedEntities(s_EntityBuffer);
            for (int i = 0; i < s_EntityBuffer.Count; i++)
            {
                UnityGameFramework.Runtime.Entity ugfEntity = s_EntityBuffer[i];
                if (ugfEntity == null)
                {
                    continue;
                }

                camera = ugfEntity.Logic as TopDownFollowCameraEntity;
                if (camera != null)
                {
                    return true;
                }
            }

            return false;
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            TopDownFollowCameraData data = userData as TopDownFollowCameraData;
            if (data == null)
            {
                Log.Error("TopDownFollowCameraEntity data is invalid.");
                return;
            }

            m_FollowEntityId = data.FollowEntityId;
            m_FixedRotation = ComputeFixedRotation();
            m_Camera = GetComponent<Camera>();
            if (m_Camera != null)
            {
                WorldUIGroupCanvas.SetWorldCamera(m_Camera);
            }

            Name = "TopDownFollowCamera";
            CachedTransform.rotation = m_FixedRotation;
            m_SmoothFollowPosition = CachedTransform.position;
            m_ShakeImpulses.Clear();
            TryBindFollowTarget();
            GameEntry.Sound.RefreshBuiltInAudioListener();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            if (m_Camera != null)
            {
                WorldUIGroupCanvas.ClearWorldCamera(m_Camera);
            }

            GameEntry.Sound.RefreshBuiltInAudioListener();
            m_FollowEntityId = 0;
            m_FollowTarget = null;
            m_Camera = null;
            m_ShakeImpulses.Clear();
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_FollowTarget == null)
            {
                TryBindFollowTarget();
                if (m_FollowTarget == null)
                {
                    return;
                }
            }

            Vector3 desiredPosition = m_FollowTarget.position + m_FollowOffset;
            m_SmoothFollowPosition = Vector3.Lerp(
                m_SmoothFollowPosition,
                desiredPosition,
                m_FollowSmoothSpeed * elapseSeconds);
            CachedTransform.position = m_SmoothFollowPosition + UpdateAndGetShakeOffset(elapseSeconds);
            CachedTransform.rotation = m_FixedRotation;
        }

        private Vector3 UpdateAndGetShakeOffset(float deltaTime)
        {
            if (m_ShakeImpulses.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 offset = Vector3.zero;
            for (int i = m_ShakeImpulses.Count - 1; i >= 0; i--)
            {
                ShakeImpulse impulse = m_ShakeImpulses[i];
                impulse.Elapsed += deltaTime;
                if (impulse.Elapsed >= impulse.Duration)
                {
                    m_ShakeImpulses.RemoveAt(i);
                    continue;
                }

                float decay = 1f - impulse.Elapsed / impulse.Duration;
                float t = impulse.Elapsed * impulse.Frequency;
                float amp = impulse.Amplitude * decay;
                offset.x += (Mathf.PerlinNoise(impulse.SeedX + t, 0f) - 0.5f) * 2f * amp;
                offset.y += (Mathf.PerlinNoise(impulse.SeedY + t, 0f) - 0.5f) * 2f * amp * 0.35f;
                offset.z += (Mathf.PerlinNoise(impulse.SeedZ + t, 0f) - 0.5f) * 2f * amp;
                m_ShakeImpulses[i] = impulse;
            }

            return offset;
        }

        private Quaternion ComputeFixedRotation()
        {
            Vector3 lookDirection = new Vector3(0f, m_LookAtHeightOffset, 0f) - m_FollowOffset;
            if (lookDirection.sqrMagnitude < 0.0001f)
            {
                return CachedTransform.rotation;
            }

            return Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }

        private void TryBindFollowTarget()
        {
            if (m_FollowEntityId == 0)
            {
                return;
            }

            Entity followEntity = GameEntry.Entity.GetGameEntity(m_FollowEntityId);
            if (followEntity == null)
            {
                m_FollowTarget = null;
                return;
            }

            RagdollEntity ragdoll = followEntity as RagdollEntity;
            if (ragdoll != null && ragdoll.BoneRoot != null)
            {
                m_FollowTarget = ragdoll.BoneRoot;
                return;
            }

            m_FollowTarget = followEntity.CachedTransform;
        }
    }
}
