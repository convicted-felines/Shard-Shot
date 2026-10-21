using System;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 平台服务基类。
    /// </summary>
    public abstract class PlatformServiceBase : MonoBehaviour, IPlatformService
    {
        private PlatformComponent m_PlatformComponent;

        public PlatformComponent PlatformComponent
        {
            get
            {
                return m_PlatformComponent;
            }
        }

        public abstract PlatformType PlatformType
        {
            get;
        }

        public virtual string ServiceName
        {
            get
            {
                return GetType().Name;
            }
        }

        public virtual void Initialize(PlatformComponent platformComponent)
        {
            m_PlatformComponent = platformComponent;
            Log.Info("Platform service '{0}' initialized for platform '{1}'.", ServiceName, PlatformType);
        }

        public virtual bool IsFeatureSupported(PlatformFeature feature)
        {
            return false;
        }

        public virtual bool TryShowRewardedAd(string adUnitId, Action<bool> callback)
        {
            Log.Warning("RewardedAd is not supported on platform '{0}'.", PlatformType);
            callback?.Invoke(false);
            return false;
        }

        public virtual bool TryShowInterstitialAd(string adUnitId)
        {
            Log.Warning("InterstitialAd is not supported on platform '{0}'.", PlatformType);
            return false;
        }

        public virtual bool TryShare(string title, string imageUrl, string query)
        {
            Log.Warning("Share is not supported on platform '{0}'.", PlatformType);
            return false;
        }

        public virtual bool TryInvokeCustomFeature(string featureName, object userData)
        {
            Log.Warning("Custom feature '{0}' is not supported on platform '{1}'.", featureName, PlatformType);
            return false;
        }

        /// <summary>
        /// 体力不足默认响应：仅提示。具体平台（如 <see cref="DefaultPlatformService"/>）可覆盖为补满或弹广告。
        /// </summary>
        public virtual PlatformResourceResolveResult HandleInsufficientStamina(int requiredAmount, Action onResolved = null)
        {
            Log.Warning("Stamina insufficient on platform '{0}', required={1}.", PlatformType, requiredAmount);
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

        /// <summary>
        /// 金币不足默认响应：显示提示。具体平台可覆盖为广告兑换等逻辑。
        /// </summary>
        public virtual PlatformResourceResolveResult HandleInsufficientGold(int requiredAmount, Action onResolved = null)
        {
            Log.Warning("Gold insufficient on platform '{0}', required={1}.", PlatformType, requiredAmount);
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

        /// <summary>
        /// 体力已补满后通知业务：同步解决时立即回调并返回 <see cref="PlatformResourceResolveResult.ResolvedImmediately"/>。
        /// </summary>
        protected PlatformResourceResolveResult NotifyStaminaResolvedImmediately(Action onResolved)
        {
            onResolved?.Invoke();
            return PlatformResourceResolveResult.ResolvedImmediately;
        }

        /// <summary>
        /// 标记异步处理中（如广告弹窗）。调用方应自行在补满成功后调用传入的 onResolved。
        /// </summary>
        protected PlatformResourceResolveResult NotifyStaminaResolvePendingAsync()
        {
            return PlatformResourceResolveResult.PendingAsync;
        }

        protected int IncreaseTestCounter(string counterName)
        {
            string key = GetCounterKey(counterName);
            int nextValue = PlayerPrefs.GetInt(key, 0) + 1;
            PlayerPrefs.SetInt(key, nextValue);
            PlayerPrefs.Save();
            return nextValue;
        }

        protected void ClearTestCounter(string counterName)
        {
            string key = GetCounterKey(counterName);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        private string GetCounterKey(string counterName)
        {
            return string.Format("Platform.Test.{0}.{1}", PlatformType, counterName);
        }
    }
}
