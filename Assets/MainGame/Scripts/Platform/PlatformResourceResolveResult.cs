namespace MainGame
{
    /// <summary>
    /// 平台处理体力/金币不足后的结果，供业务层决定是否立即重试或等待回调。
    /// </summary>
    public enum PlatformResourceResolveResult
    {
        /// <summary>未解决（已提示或拒绝）；业务应中止当前操作。</summary>
        Unresolved = 0,

        /// <summary>已同步解决（如默认平台直接补满体力）；业务可立即重试扣除/继续。</summary>
        ResolvedImmediately = 1,

        /// <summary>已异步接管（如弹广告窗）；业务应等待 <c>onResolved</c> 后再重试。</summary>
        PendingAsync = 2,
    }
}
