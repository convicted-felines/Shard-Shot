using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 刷怪流程参数（由 <see cref="JuiceComponent"/> 在 Inspector 配置）。
    /// </summary>
    public readonly struct SpawnFlowSettings
    {
        public int MaxAliveCount { get; }
        public float SpawnBound { get; }
        public float SpawnIntervalInitial { get; }
        public float SpawnIntervalMin { get; }
        public float AccelerationDuration { get; }
        public float WaveInterval { get; }
        public float BurstSpawnInterval { get; }

        public SpawnFlowSettings(
            int maxAliveCount,
            float spawnBound,
            float spawnIntervalInitial,
            float spawnIntervalMin,
            float accelerationDuration,
            float waveInterval,
            float burstSpawnInterval)
        {
            MaxAliveCount = maxAliveCount;
            SpawnBound = spawnBound;
            SpawnIntervalInitial = spawnIntervalInitial;
            SpawnIntervalMin = spawnIntervalMin;
            AccelerationDuration = accelerationDuration;
            WaveInterval = waveInterval;
            BurstSpawnInterval = burstSpawnInterval;
        }
    }

    /// <summary>
    /// 战斗视听反馈调度器。后续在武器开火、命中、爆炸、击杀等挂点调用。
    /// </summary>
    public sealed class JuiceComponent : GameFrameworkComponent
    {
        /// <summary>
        /// 播放表驱动特效（世界坐标）。
        /// </summary>
        public void PlayEffect(EffectEnum effectEnum, Vector3 position, Quaternion rotation)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            GameEntry.Entity.ShowEffect(effectEnum, position, rotation);
        }

        /// <summary>
        /// 播放表驱动特效（世界坐标，默认朝向）。
        /// </summary>
        public void PlayEffect(EffectEnum effectEnum, Vector3 position)
        {
            PlayEffect(effectEnum, position, Quaternion.identity);
        }

        /// <summary>
        /// 播放表驱动特效（世界坐标，旋转保持 Prefab 初始朝向）。
        /// </summary>
        public void PlayEffectAtPrefabRotation(EffectEnum effectEnum, Vector3 position)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown)
            {
                return;
            }

            GameEntry.Entity.ShowEffectAtPrefabRotation(effectEnum, position);
        }

        /// <summary>
        /// 播放附着于实体的特效。
        /// </summary>
        public void PlayEffectAttached(EffectEnum effectEnum, int followEntityId, Vector3 followOffset = default)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown || followEntityId == 0)
            {
                return;
            }

            GameEntry.Entity.ShowEffectAttached(effectEnum, followEntityId, followOffset);
        }

        /// <summary>
        /// 播放附着于实体子节点的特效，并保持世界朝向。
        /// </summary>
        public void PlayEffectAttachedToTransform(EffectEnum effectEnum, int followEntityId,
            string followTransformPath, Quaternion worldRotation, Vector3 localOffset = default)
        {
            if (GameEntry.Entity == null || effectEnum == EffectEnum.Unknown || followEntityId == 0
                || string.IsNullOrEmpty(followTransformPath))
            {
                return;
            }

            GameEntry.Entity.ShowEffectAttachedToTransform(
                effectEnum, followEntityId, followTransformPath, worldRotation, localOffset);
        }

        [Header("震屏 - 距离衰减（共用）")]
        [SerializeField]
        [Tooltip("与 3D 音效一致：小于该距离时震幅不衰减")]
        private float m_ShakeMinDistance = SoundExtension.SpatialMinDistance;

        [SerializeField]
        [Tooltip("与 3D 音效一致：大于等于该距离时震幅为 0")]
        private float m_ShakeMaxDistance = 100f;

        [Header("震屏 - 爆炸")]
        [SerializeField]
        [Tooltip("爆炸震动位移幅度（世界单位）")]
        private float m_ExplosionShakeAmplitude = 0.035f;

        [SerializeField]
        [Tooltip("爆炸震动持续时间（秒）")]
        private float m_ExplosionShakeDuration = 0.1f;

        [SerializeField]
        [Tooltip("爆炸震动 Perlin 噪声频率")]
        private float m_ExplosionShakeFrequency = 28f;

        [Header("震屏 - 加特林开火")]
        [SerializeField]
        [Tooltip("每发开火震动位移幅度（世界单位）；高射速下多发叠加形成嗡嗡感")]
        private float m_GatlingFireShakeAmplitude = 0.012f;

        [SerializeField]
        [Tooltip("单发震动持续时间（秒）；宜短于射速间隔以便连发叠加")]
        private float m_GatlingFireShakeDuration = 0.04f;

        [SerializeField]
        [Tooltip("单发震动 Perlin 噪声频率；偏高更像枪械共振")]
        private float m_GatlingFireShakeFrequency = 40f;

        [Header("震屏 - 角色死亡")]
        [SerializeField]
        [Tooltip("角色死亡震动位移幅度（世界单位）")]
        private float m_DeathShakeAmplitude = 0.025f;

        [SerializeField]
        [Tooltip("角色死亡震动持续时间（秒）")]
        private float m_DeathShakeDuration = 0.08f;

        [SerializeField]
        [Tooltip("角色死亡震动 Perlin 噪声频率")]
        private float m_DeathShakeFrequency = 28f;

        [Header("刷怪流程")]
        [SerializeField]
        [Tooltip("场上怪物数量上限")]
        private int m_SpawnMaxAliveCount = 40;

        [SerializeField]
        [Tooltip("刷怪区域半边长（世界单位，以原点为中心）")]
        private float m_SpawnBound = 20f;

        [SerializeField]
        [Tooltip("开局刷怪间隔（秒）")]
        private float m_SpawnIntervalInitial = 5f;

        [SerializeField]
        [Tooltip("加速后的最短刷怪间隔（秒）")]
        private float m_SpawnIntervalMin = 0.5f;

        [SerializeField]
        [Tooltip("刷怪间隔从初始值缩短到最短值所需时间（秒）")]
        private float m_SpawnAccelerationDuration = 20f;

        [SerializeField]
        [Tooltip("达到最短间隔后，每隔多久触发一次补满")]
        private float m_SpawnWaveInterval = 8f;

        [SerializeField]
        [Tooltip("补满阶段每只怪之间的生成间隔（秒）")]
        private float m_SpawnBurstInterval = 0.08f;

        public SpawnFlowSettings SpawnFlow => new SpawnFlowSettings(
            Mathf.Max(1, m_SpawnMaxAliveCount),
            Mathf.Max(0.1f, m_SpawnBound),
            Mathf.Max(0.01f, m_SpawnIntervalInitial),
            Mathf.Max(0.01f, m_SpawnIntervalMin),
            Mathf.Max(0.01f, m_SpawnAccelerationDuration),
            Mathf.Max(0.01f, m_SpawnWaveInterval),
            Mathf.Max(0.01f, m_SpawnBurstInterval));

        public void OnWeaponFire(WeaponEnum weaponType, Vector3 position)
        {
            switch (weaponType)
            {
                case WeaponEnum.加特林:
                    TryPlayAttenuatedShake(
                        position,
                        m_GatlingFireShakeAmplitude,
                        m_GatlingFireShakeDuration,
                        m_GatlingFireShakeFrequency);
                    break;
            }
        }

        public void OnHit(Vector3 position)
        {
        }

        /// <summary>
        /// 爆炸反馈：按与摄像机的距离做对数衰减（与 3D 音效一致）。
        /// </summary>
        public void OnExplosion(Vector3 position, float radius)
        {
            TryPlayAttenuatedShake(
                position,
                m_ExplosionShakeAmplitude,
                m_ExplosionShakeDuration,
                m_ExplosionShakeFrequency);
        }

        /// <summary>
        /// 任意角色死亡时的震屏反馈。
        /// </summary>
        public void OnCharacterDeath(Vector3 position)
        {
            TryPlayAttenuatedShake(
                position,
                m_DeathShakeAmplitude,
                m_DeathShakeDuration,
                m_DeathShakeFrequency);
        }

        public void OnKill(Vector3 position)
        {
        }

        private void TryPlayAttenuatedShake(Vector3 worldPosition, float amplitude, float duration, float frequency)
        {
            if (!TopDownFollowCameraEntity.TryGetActive(out TopDownFollowCameraEntity camera))
            {
                return;
            }

            float distance = Vector3.Distance(camera.CachedTransform.position, worldPosition);
            float attenuation = SoundExtension.EvaluateLogarithmicAttenuation(
                distance, m_ShakeMinDistance, m_ShakeMaxDistance);
            if (attenuation <= 0f)
            {
                return;
            }

            camera.AddImpulse(amplitude * attenuation, duration, frequency);
        }
    }
}
