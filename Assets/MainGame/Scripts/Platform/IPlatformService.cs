using System;

namespace MainGame
{
    /// <summary>
    /// 平台服务抽象。
    /// 共性能力走统一接口，差异能力通过 Feature 判断或自定义入口扩展。
    /// </summary>
    public interface IPlatformService
    {
        PlatformType PlatformType { get; }

        string ServiceName { get; }

        void Initialize(PlatformComponent platformComponent);

        bool IsFeatureSupported(PlatformFeature feature);

        bool TryShowRewardedAd(string adUnitId, Action<bool> callback);

        bool TryShowInterstitialAd(string adUnitId);

        bool TryShare(string title, string imageUrl, string query);

        bool TryInvokeCustomFeature(string featureName, object userData);

        /// <summary>
        /// 体力不足时的平台响应。
        /// 默认平台可直接补满；抖音/微信等可弹广告窗，成功后通过 <paramref name="onResolved"/> 通知。
        /// </summary>
        /// <param name="requiredAmount">本次需要的体力。</param>
        /// <param name="onResolved">资源已可用时的回调（同步解决时也会立即调用）。</param>
        PlatformResourceResolveResult HandleInsufficientStamina(int requiredAmount, Action onResolved = null);

        /// <summary>
        /// 金币不足时的平台响应。
        /// 默认平台显示提示；其他平台可按需扩展（广告兑换等）。
        /// </summary>
        /// <param name="requiredAmount">本次需要的金币。</param>
        /// <param name="onResolved">资源已可用时的回调（同步解决时也会立即调用）。</param>
        PlatformResourceResolveResult HandleInsufficientGold(int requiredAmount, Action onResolved = null);
    }
}
