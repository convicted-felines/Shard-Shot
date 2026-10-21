namespace MainGame
{
    /// <summary>
    /// 平台能力枚举。
    /// 用于区分“通用能力”和“平台独占能力”。
    /// </summary>
    public enum PlatformFeature
    {
        Login = 0,
        RewardedAd = 1,
        InterstitialAd = 2,
        BannerAd = 3,
        Share = 4,
        Vibrate = 5,
        CustomerService = 6,
        Shortcut = 7,
        Custom = 100
    }
}
