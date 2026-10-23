using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 短命粒子特效实体：Show 时播放粒子，播完后自动 Hide 回池。
    /// </summary>
    public class EffectEntity : Entity
    {
        private EffectData m_EffectData;
        private ParticleSystem[] m_ParticleSystems;
        private float m_HideTimer;
        private bool m_AutoHideScheduled;
        private Quaternion m_AuthoredRotation;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_AuthoredRotation = CachedTransform.rotation;
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);

            m_EffectData = userData as EffectData;
            if (m_EffectData == null)
            {
                Log.Error("EffectEntity data is invalid.");
                return;
            }

            Name = string.IsNullOrEmpty(m_EffectData.EntityName)
                ? Utility.Text.Format("Effect_{0}", Id)
                : m_EffectData.EntityName;

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            CachedTransform.localScale = m_EffectData.Scale;
            ApplyPose(true);
            PlayParticles();
            ScheduleAutoHide();
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_EffectData = null;
            m_ParticleSystems = null;
            m_HideTimer = 0f;
            m_AutoHideScheduled = false;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);

            if (m_EffectData == null)
            {
                return;
            }

            if (m_EffectData.FollowEntityId != 0)
            {
                ApplyPose(false);
            }

            if (!m_AutoHideScheduled)
            {
                return;
            }

            m_HideTimer -= elapseSeconds;
            if (m_HideTimer <= 0f)
            {
                GameEntry.Entity.HideEntity(this);
            }
        }

        private void ApplyPose(bool snapTransform)
        {
            if (m_EffectData == null)
            {
                return;
            }

            Vector3 position = m_EffectData.Position;
            if (m_EffectData.FollowEntityId != 0 && GameEntry.Entity != null)
            {
                Entity follow = GameEntry.Entity.GetGameEntity(m_EffectData.FollowEntityId);
                if (follow != null)
                {
                    position = follow.CachedTransform.position + m_EffectData.FollowOffset;
                }
            }

            if (m_EffectData.EffectHeight != 0f)
            {
                position.y += m_EffectData.EffectHeight;
            }

            m_EffectData.Position = position;

            if (snapTransform)
            {
                Quaternion rotation = m_EffectData.UsePrefabRotation
                    ? m_AuthoredRotation
                    : m_EffectData.Rotation;
                CachedTransform.SetPositionAndRotation(position, rotation);
            }
            else
            {
                CachedTransform.position = position;
            }
        }

        private void PlayParticles()
        {
            m_ParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
            if (m_ParticleSystems == null || m_ParticleSystems.Length == 0)
            {
                return;
            }

            for (int i = 0; i < m_ParticleSystems.Length; i++)
            {
                ParticleSystem particleSystem = m_ParticleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                particleSystem.Clear(true);
                particleSystem.Play(true);
            }
        }

        private void ScheduleAutoHide()
        {
            if (m_EffectData != null && m_EffectData.DisableAutoHide)
            {
                m_AutoHideScheduled = false;
                return;
            }

            float duration = m_EffectData != null && m_EffectData.AutoHideDuration > 0f
                ? m_EffectData.AutoHideDuration
                : ComputeParticleLifetime(m_ParticleSystems);

            m_HideTimer = duration;
            m_AutoHideScheduled = duration > 0f;
        }

        private static float ComputeParticleLifetime(ParticleSystem[] particleSystems)
        {
            if (particleSystems == null || particleSystems.Length == 0)
            {
                return Constant.Effect.DefaultLifetime;
            }

            float maxLifetime = 0f;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                ParticleSystem.MainModule main = particleSystem.main;
                float startLifetime = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : main.startLifetime.constant;
                float lifetime = main.duration + Mathf.Max(0f, startLifetime);
                if (lifetime > maxLifetime)
                {
                    maxLifetime = lifetime;
                }
            }

            if (maxLifetime <= 0f)
            {
                return Constant.Effect.DefaultLifetime;
            }

            return maxLifetime + Constant.Effect.LifetimePadding;
        }
    }
}
