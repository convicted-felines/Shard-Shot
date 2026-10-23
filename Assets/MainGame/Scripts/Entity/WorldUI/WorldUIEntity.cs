using GameFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 世界空间 UI 实体：跟随进度条，或一次性上浮飘字。
    /// 生命周期与普通实体一致：Show 取出 / Hide 回池。
    /// </summary>
    public class WorldUIEntity : Entity
    {
        private const float FloatFadeStartNormalized = 0.55f;

        [SerializeField]
        private Image m_ProgressFillImage = null;

        [SerializeField]
        private TMP_Text m_Text = null;

        private WorldUIData m_WorldUIData;
        private CanvasGroup m_CanvasGroup;
        private bool m_Floating;
        private Vector3 m_FloatStartPosition;
        private float m_FloatElapsed;

        public float Progress
        {
            get { return m_WorldUIData != null ? m_WorldUIData.Progress : 0f; }
        }

        protected override void OnShow(object userData)
        {
            WorldUIGroupCanvas.EnsureReady();
            base.OnShow(userData);

            m_WorldUIData = userData as WorldUIData;
            if (m_WorldUIData == null)
            {
                Log.Error("WorldUIEntity data is invalid.");
                return;
            }

            Name = string.IsNullOrEmpty(m_WorldUIData.EntityName)
                ? Utility.Text.Format("WorldUI_{0}", Id)
                : m_WorldUIData.EntityName;

            EnsureProgressFillImage();
            EnsureText();

            m_CanvasGroup = gameObject.GetOrAddComponent<CanvasGroup>();
            m_CanvasGroup.alpha = 1f;
            m_CanvasGroup.blocksRaycasts = false;
            m_CanvasGroup.interactable = false;
            CachedTransform.localScale = Vector3.one;
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (!string.IsNullOrEmpty(m_WorldUIData.DisplayText) && m_Text != null)
            {
                m_Text.text = m_WorldUIData.DisplayText;
            }

            m_Floating = m_WorldUIData.IsFloatingText;
            m_FloatElapsed = 0f;
            m_FloatStartPosition = m_WorldUIData.Position;
            if (m_Floating)
            {
                m_WorldUIData.FollowEntityId = 0;
            }

            SetProgress(m_WorldUIData.Progress);
            ApplyPose(true);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            m_WorldUIData = null;
            m_CanvasGroup = null;
            m_Floating = false;
            m_FloatElapsed = 0f;
            base.OnHide(isShutdown, userData);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            if (m_WorldUIData == null)
            {
                return;
            }

            if (m_Floating)
            {
                TickFloating(elapseSeconds);
                return;
            }

            ApplyPose(false);
        }

        public void SetProgress(float progress01)
        {
            if (m_WorldUIData != null)
            {
                m_WorldUIData.Progress = Mathf.Clamp01(progress01);
            }

            if (m_ProgressFillImage != null)
            {
                m_ProgressFillImage.fillAmount = m_WorldUIData != null ? m_WorldUIData.Progress : Mathf.Clamp01(progress01);
            }
        }

        public void BindFollow(int followEntityId, Vector3 followOffset)
        {
            if (m_WorldUIData == null)
            {
                return;
            }

            m_WorldUIData.FollowEntityId = followEntityId;
            m_WorldUIData.FollowOffset = followOffset;
        }

        private void TickFloating(float elapseSeconds)
        {
            m_FloatElapsed += elapseSeconds;
            float duration = m_WorldUIData.FloatDuration;
            float t = Mathf.Clamp01(m_FloatElapsed / duration);
            Vector3 position = m_FloatStartPosition + Vector3.up * (m_WorldUIData.FloatRiseDistance * t);
            m_WorldUIData.Position = position;

            if (m_CanvasGroup != null)
            {
                if (t <= FloatFadeStartNormalized)
                {
                    m_CanvasGroup.alpha = 1f;
                }
                else
                {
                    float fadeT = (t - FloatFadeStartNormalized) / (1f - FloatFadeStartNormalized);
                    m_CanvasGroup.alpha = 1f - fadeT;
                }
            }

            ApplyPose(true);

            if (m_FloatElapsed >= duration)
            {
                m_Floating = false;
                GameEntry.Entity.HideEntity(this);
            }
        }

        private void ApplyPose(bool force)
        {
            if (m_WorldUIData == null)
            {
                return;
            }

            Vector3 position = m_WorldUIData.Position;
            if (!m_Floating && m_WorldUIData.FollowEntityId != 0 && GameEntry.Entity != null)
            {
                Entity follow = GameEntry.Entity.GetGameEntity(m_WorldUIData.FollowEntityId);
                if (follow != null)
                {
                    if (m_WorldUIData.FollowMode == WorldUIFollowMode.CharacterHealthBar
                        && follow is CharacterEntity character
                        && character.TryGetWorldHealthBarPosition(m_WorldUIData.FixedWorldY, out Vector3 barPosition))
                    {
                        position = barPosition;
                    }
                    else
                    {
                        position = follow.CachedTransform.position + m_WorldUIData.FollowOffset;
                    }

                    m_WorldUIData.Position = position;
                }
                else if (!force)
                {
                    return;
                }
            }

            Quaternion rotation = m_WorldUIData.Rotation;
            if (m_WorldUIData.FaceCamera)
            {
                Camera worldCamera = WorldUIGroupCanvas.WorldCamera;
                if (worldCamera != null)
                {
                    Vector3 forward = worldCamera.transform.forward;
                    if (forward.sqrMagnitude > 0.0001f)
                    {
                        rotation = Quaternion.LookRotation(forward, worldCamera.transform.up);
                        m_WorldUIData.Rotation = rotation;
                    }
                }
            }

            CachedTransform.SetPositionAndRotation(position, rotation);
        }

        private void EnsureProgressFillImage()
        {
            if (m_ProgressFillImage != null)
            {
                return;
            }

            Transform bar = CachedTransform.Find("bar");
            if (bar != null)
            {
                m_ProgressFillImage = bar.GetComponent<Image>();
            }
        }

        private void EnsureText()
        {
            if (m_Text != null)
            {
                return;
            }

            m_Text = GetComponentInChildren<TMP_Text>(true);
        }
    }
}
