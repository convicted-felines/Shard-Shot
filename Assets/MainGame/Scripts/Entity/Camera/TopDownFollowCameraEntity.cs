using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 俯视角跟随摄像机。脚本需预先挂在预制体上，参数在 Inspector 调整。
    /// </summary>
    public class TopDownFollowCameraEntity : Entity
    {
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
            CachedTransform.position = Vector3.Lerp(
                CachedTransform.position,
                desiredPosition,
                m_FollowSmoothSpeed * elapseSeconds);
            CachedTransform.rotation = m_FixedRotation;
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
            m_FollowTarget = followEntity != null ? followEntity.CachedTransform : null;
        }
    }
}
