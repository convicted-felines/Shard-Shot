namespace MainGame
{
    /// <summary>
    /// 外部信号入口：业务层只 Raise，观察逻辑在 <see cref="ProgressObserverEntity"/> 子类中实现。
    /// </summary>
    public static class ProgressObserverBridge
    {
        public static void NotifySignal(ProgressObserverSignal signal, object context = null)
        {
            GameEntry.ProgressObserver?.OnSignal(signal, context);
        }
    }
}
