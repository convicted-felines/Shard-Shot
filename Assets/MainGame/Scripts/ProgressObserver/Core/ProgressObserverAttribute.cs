using System;

namespace MainGame
{
    /// <summary>
    /// 声明进度观察实体逻辑：Id、创建时机、适用流程。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ProgressObserverAttribute : Attribute
    {
        public string Id { get; }
        public ProgressObserverSpawnMode SpawnMode { get; }
        public ProgressObserverSignal SpawnSignal { get; }
        public Type[] ActiveInProcedures { get; }

        public ProgressObserverAttribute(
            string id,
            ProgressObserverSpawnMode spawnMode = ProgressObserverSpawnMode.ProcedureEnter,
            ProgressObserverSignal spawnSignal = ProgressObserverSignal.None,
            params Type[] activeInProcedures)
        {
            Id = id;
            SpawnMode = spawnMode;
            SpawnSignal = spawnSignal;
            ActiveInProcedures = activeInProcedures ?? Array.Empty<Type>();
        }
    }
}
