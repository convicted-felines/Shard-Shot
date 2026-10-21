using System;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 抖音小游戏平台测试服务。
    /// 后续可在这里接入抖音小游戏 SDK。
    /// </summary>
    public class DouYinMiniGamePlatformService : PlatformServiceBase
    {
        public override PlatformType PlatformType
        {
            get
            {
                return PlatformType.DouYinMiniGame;
            }
        }

        public override bool IsFeatureSupported(PlatformFeature feature)
        {
            return feature == PlatformFeature.RewardedAd
                || feature == PlatformFeature.InterstitialAd
                || feature == PlatformFeature.Share
                || feature == PlatformFeature.Shortcut
                || feature == PlatformFeature.Vibrate;
        }

        public override bool TryShowRewardedAd(string adUnitId, Action<bool> callback)
        {
            int count = IncreaseTestCounter("RewardedAd");
            Log.Info("[DouYinMiniGame] Test rewarded ad invoked. AdUnitId='{0}', Count='{1}'.", adUnitId, count);
            callback?.Invoke(true);
            return true;
        }

        public override bool TryShowInterstitialAd(string adUnitId)
        {
            int count = IncreaseTestCounter("InterstitialAd");
            Log.Info("[DouYinMiniGame] Test interstitial ad invoked. AdUnitId='{0}', Count='{1}'.", adUnitId, count);
            return true;
        }

        public override bool TryShare(string title, string imageUrl, string query)
        {
            int count = IncreaseTestCounter("Share");
            Log.Info("[DouYinMiniGame] Test share invoked. Title='{0}', ImageUrl='{1}', Query='{2}', Count='{3}'.", title, imageUrl, query, count);
            return true;
        }

        public override bool TryInvokeCustomFeature(string featureName, object userData)
        {
            if (string.Equals(featureName, "ShowFavoriteGuide", StringComparison.Ordinal))
            {
                int count = IncreaseTestCounter("FavoriteGuide");
                Log.Info("[DouYinMiniGame] Test favorite guide invoked. Count='{0}'.", count);
                return true;
            }

            if (string.Equals(featureName, "ClearTestCounters", StringComparison.Ordinal))
            {
                ClearTestCounter("RewardedAd");
                ClearTestCounter("InterstitialAd");
                ClearTestCounter("Share");
                ClearTestCounter("FavoriteGuide");
                Log.Info("[DouYinMiniGame] Test counters cleared.");
                return true;
            }

            return base.TryInvokeCustomFeature(featureName, userData);
        }

        /// <summary>
        /// 体力不足：后续在此弹窗引导观看激励广告补满体力。
        /// 合并到抖音分支后改为类似：
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
