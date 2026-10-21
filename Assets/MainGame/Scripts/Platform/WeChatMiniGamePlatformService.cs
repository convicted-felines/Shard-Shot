using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 微信小游戏平台测试服务。
    /// 后续可在这里接入微信小游戏 SDK。
    /// </summary>
    public class WeChatMiniGamePlatformService : PlatformServiceBase
    {
        public override PlatformType PlatformType
        {
            get
            {
                return PlatformType.WeChatMiniGame;
            }
        }

        public override bool IsFeatureSupported(PlatformFeature feature)
        {
            return feature == PlatformFeature.RewardedAd
                || feature == PlatformFeature.BannerAd
                || feature == PlatformFeature.Share
                || feature == PlatformFeature.Vibrate
                || feature == PlatformFeature.CustomerService;
        }

        public override bool TryShowRewardedAd(string adUnitId, Action<bool> callback)
        {
            int count = IncreaseTestCounter("RewardedAd");
            Log.Info("[WeChatMiniGame] Test rewarded ad invoked. AdUnitId='{0}', Count='{1}'.", adUnitId, count);
            callback?.Invoke(true);
            return true;
        }

        public override bool TryShare(string title, string imageUrl, string query)
        {
            int count = IncreaseTestCounter("Share");
            Log.Info("[WeChatMiniGame] Test share invoked. Title='{0}', ImageUrl='{1}', Query='{2}', Count='{3}'.", title, imageUrl, query, count);
            return true;
        }

        public override bool TryInvokeCustomFeature(string featureName, object userData)
        {
            if (string.Equals(featureName, "OpenCustomerService", StringComparison.Ordinal))
            {
                int count = IncreaseTestCounter("CustomerService");
                Log.Info("[WeChatMiniGame] Test customer service invoked. Count='{0}'.", count);
                return true;
            }

            if (string.Equals(featureName, "ClearTestCounters", StringComparison.Ordinal))
            {
                ClearTestCounter("RewardedAd");
                ClearTestCounter("Share");
                ClearTestCounter("CustomerService");
                Log.Info("[WeChatMiniGame] Test counters cleared.");
                return true;
            }

            return base.TryInvokeCustomFeature(featureName, userData);
        }

        /// <summary>
        /// 体力不足：后续在此弹窗引导观看激励广告补满体力。
        /// 合并到微信分支后改为类似：
        /// <code>
        /// OpenAdPopup(() => { GameEntry.BuiltinData.RefillStaminaToMax(); onResolved?.Invoke(); });
        /// return NotifyStaminaResolvePendingAsync();
        /// </code>
        /// </summary>
        public override PlatformResourceResolveResult HandleInsufficientStamina(int requiredAmount, Action onResolved = null)
        {
            return base.HandleInsufficientStamina(requiredAmount, onResolved);
        }
    }
}
