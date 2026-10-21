using System;
using GameFramework;
using UnityEngine;
using UnityEngine.Serialization;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 平台管理组件。
    /// 用于统一管理平台能力入口，并在运行时切换到对应平台实现。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Main Game/Platform")]
    public class PlatformComponent : GameFrameworkComponent
    {
        [SerializeField]
        private bool m_InitializeOnAwake = true;

        [SerializeField]
        [FormerlySerializedAs("m_PlatformServiceTypeName")]
        private string m_PlatformHelperTypeName = "MainGame.DefaultPlatformService";

        [SerializeField]
        [FormerlySerializedAs("m_CustomPlatformService")]
        private PlatformServiceBase m_CustomPlatformHelper = null;

        private IPlatformService m_PlatformService;

        public PlatformType CurrentPlatform
        {
            get
            {
                EnsurePlatformService();
                return m_PlatformService != null ? m_PlatformService.PlatformType : RuntimePlatformType;
            }
        }

        public string CurrentServiceName
        {
            get
            {
                EnsurePlatformService();
                return m_PlatformService != null ? m_PlatformService.ServiceName : string.Empty;
            }
        }

        public PlatformType RuntimePlatformType
        {
            get
            {
                return ResolvePlatformType();
            }
        }

        public bool IsMiniGamePlatform
        {
            get
            {
                return CurrentPlatform == PlatformType.WeChatMiniGame || CurrentPlatform == PlatformType.DouYinMiniGame;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (m_InitializeOnAwake)
            {
                InitializePlatform();
            }
        }

        public void InitializePlatform()
        {
            if (m_PlatformService == null)
            {
                m_PlatformService = CreatePlatformService();
            }

            if (m_PlatformService == null)
            {
                Log.Error("Can not create platform service.");
                return;
            }

            m_PlatformService.Initialize(this);
            PlatformGraphicsUtility.ApplyForPlatform(m_PlatformService.PlatformType);
        }

        public void RegisterPlatformService(IPlatformService platformService, bool initialize = true)
        {
            if (platformService == null)
            {
                throw new GameFrameworkException("Platform service is invalid.");
            }

            m_PlatformService = platformService;
            Log.Info("Platform service switched to '{0}' for platform '{1}'.", m_PlatformService.ServiceName, m_PlatformService.PlatformType);

            if (initialize)
            {
                m_PlatformService.Initialize(this);
            }

            PlatformGraphicsUtility.ApplyForPlatform(m_PlatformService.PlatformType);
        }

        public void ResetPlatformService()
        {
            m_PlatformService = null;
        }

        public bool IsCurrentPlatform(PlatformType platformType)
        {
            return CurrentPlatform == platformType;
        }

        public bool IsFeatureSupported(PlatformFeature feature)
        {
            EnsurePlatformService();
            return m_PlatformService != null && m_PlatformService.IsFeatureSupported(feature);
        }

        public bool TryShowRewardedAd(string adUnitId, Action<bool> callback = null)
        {
            EnsurePlatformService();
            if (m_PlatformService == null)
            {
                callback?.Invoke(false);
                return false;
            }

            return m_PlatformService.TryShowRewardedAd(adUnitId, callback);
        }

        public bool TryShowInterstitialAd(string adUnitId)
        {
            EnsurePlatformService();
            return m_PlatformService != null && m_PlatformService.TryShowInterstitialAd(adUnitId);
        }

        public bool TryShare(string title, string imageUrl = null, string query = null)
        {
            EnsurePlatformService();
            return m_PlatformService != null && m_PlatformService.TryShare(title, imageUrl, query);
        }

        public bool TryInvokeCustomFeature(string featureName, object userData = null)
        {
            EnsurePlatformService();
            return m_PlatformService != null && m_PlatformService.TryInvokeCustomFeature(featureName, userData);
        }

        /// <summary>
        /// 体力不足时转交当前平台服务处理。见 <see cref="IPlatformService.HandleInsufficientStamina"/>。
        /// </summary>
        public PlatformResourceResolveResult HandleInsufficientStamina(int requiredAmount, Action onResolved = null)
        {
            EnsurePlatformService();
            if (m_PlatformService == null)
            {
                if (GameEntry.UI != null)
                {
                    GameEntry.UI.OpenDialog(new DialogParams
                    {
                        Mode = 1,
                        Message = "体力不足",
                    });
                }

                return PlatformResourceResolveResult.Unresolved;
            }

            return m_PlatformService.HandleInsufficientStamina(requiredAmount, onResolved);
        }

        /// <summary>
        /// 金币不足时转交当前平台服务处理。见 <see cref="IPlatformService.HandleInsufficientGold"/>。
        /// </summary>
        public PlatformResourceResolveResult HandleInsufficientGold(int requiredAmount, Action onResolved = null)
        {
            EnsurePlatformService();
            if (m_PlatformService == null)
            {
                if (GameEntry.UI != null)
                {
                    GameEntry.UI.OpenDialog(new DialogParams
                    {
                        Mode = 1,
                        Message = "金币不足",
                    });
                }

                return PlatformResourceResolveResult.Unresolved;
            }

            return m_PlatformService.HandleInsufficientGold(requiredAmount, onResolved);
        }

        private void EnsurePlatformService()
        {
            if (m_PlatformService == null)
            {
                InitializePlatform();
            }
        }

        private PlatformServiceBase CreatePlatformService()
        {
            PlatformServiceBase platformService = Helper.CreateHelper(m_PlatformHelperTypeName, m_CustomPlatformHelper);
            if (platformService == null)
            {
                return null;
            }

            platformService.name = "Platform Service";
            Transform cachedTransform = platformService.transform;
            cachedTransform.SetParent(transform);
            cachedTransform.localScale = Vector3.one;
            cachedTransform.localPosition = Vector3.zero;
            cachedTransform.localRotation = Quaternion.identity;
            return platformService;
        }

        private PlatformType ResolvePlatformType()
        {
#if WECHAT_MINIGAME
            return PlatformType.WeChatMiniGame;
#elif DOUYIN_MINIGAME
            return PlatformType.DouYinMiniGame;
#elif UNITY_EDITOR
            return PlatformType.Editor;
#else
            return PlatformType.Unknown;
#endif
        }
    }
}
