namespace MainGame
{
    /// <summary>
    /// 外部信号：由 Bridge 或流程回调转发至 <see cref="ProgressObserverComponent"/>。
    /// </summary>
    public enum ProgressObserverSignal
    {
        None = 0,
        /// <summary>流程进入（与 SpawnMode.ProcedureEnter 配合）。</summary>
        ProcedureEnter = 1,
        /// <summary>流程离开。</summary>
        ProcedureLeave = 2,
    }
}
