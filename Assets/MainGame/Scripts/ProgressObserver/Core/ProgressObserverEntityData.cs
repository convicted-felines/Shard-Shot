namespace MainGame
{
    /// <summary>
    /// 进度观察实体数据，由 <see cref="ProgressObserverComponent"/> 创建时注入。
    /// </summary>
    public sealed class ProgressObserverEntityData : EntityData
    {
        public ProgressObserverEntityData(int entityId, int typeId) : base(entityId, typeId)
        {
        }

        public string ObserverId { get; set; }

        public override void Clear()
        {
            base.Clear();
            ObserverId = null;
        }
    }
}
