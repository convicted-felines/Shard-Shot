namespace MainGame
{
    /// <summary>
    /// 进度观察实体创建时机（粗粒度由流程触发，细粒度由信号或 Manual 触发）。
    /// </summary>
    public enum ProgressObserverSpawnMode
    {
        ProcedureEnter = 0,
        OnSignal = 1,
        Manual = 2,
    }
}
