using System.Collections.Generic;

using UnityEngine;



namespace MainGame

{

    /// <summary>

    /// 单种怪物的刷怪权重配置。Weight 越大，出现概率越高。

    /// </summary>

    public readonly struct SpawnMonsterEntry

    {

        public CharacterEnum Type { get; }

        public float Weight { get; }



        public SpawnMonsterEntry(CharacterEnum type, float weight)

        {

            Type = type;

            Weight = weight;

        }

    }



    /// <summary>

    /// 刷怪管理：加速阶段逐只生成；达到最短间隔后，周期性以短间隔补满场上数量。

    /// </summary>

    public sealed class SpawnManager

    {

        private static readonly SpawnFlowSettings s_DefaultSpawnFlow = new SpawnFlowSettings(

            40, 20f, 5f, 0.5f, 20f, 8f, 0.08f);



        private readonly List<int> m_AliveEntityIds = new List<int>(64);

        private readonly int m_PlayerEntityId;

        private readonly SpawnMonsterEntry[] m_SpawnEntries;

        private readonly float m_TotalSpawnWeight;



        private float m_ElapsedTime;

        private float m_SpawnTimer;

        private float m_WaveTimer;

        private float m_BurstSpawnTimer;

        private bool m_IsBursting;



        public SpawnManager(int playerEntityId, SpawnMonsterEntry[] spawnEntries)

        {

            m_PlayerEntityId = playerEntityId;

            m_SpawnEntries = spawnEntries;

            m_TotalSpawnWeight = CalculateTotalWeight(spawnEntries);

        }



        public void Reset()

        {

            SpawnFlowSettings settings = ResolveSpawnFlow();



            m_AliveEntityIds.Clear();

            m_ElapsedTime = 0f;

            m_SpawnTimer = settings.SpawnIntervalInitial;

            m_WaveTimer = settings.WaveInterval;

            m_BurstSpawnTimer = 0f;

            m_IsBursting = false;

        }



        public void Update(float elapseSeconds)

        {

            m_ElapsedTime += elapseSeconds;

            PruneInactive();



            SpawnFlowSettings settings = ResolveSpawnFlow();

            if (IsAccelerationPhase(settings))

            {

                UpdateAccelerationPhase(elapseSeconds, settings);

                return;

            }



            UpdateWaveBurstPhase(elapseSeconds, settings);

        }



        public void ClearAll()

        {

            for (int i = 0; i < m_AliveEntityIds.Count; i++)

            {

                int entityId = m_AliveEntityIds[i];

                if (entityId != 0 && GameEntry.Entity.HasEntity(entityId))

                {

                    GameEntry.Entity.HideEntity(entityId);

                }

            }



            m_AliveEntityIds.Clear();

        }



        private void UpdateAccelerationPhase(float elapseSeconds, SpawnFlowSettings settings)

        {

            m_SpawnTimer -= elapseSeconds;

            if (m_SpawnTimer > 0f)

            {

                return;

            }



            TrySpawnOne(settings);

            m_SpawnTimer = GetCurrentSpawnInterval(settings);

        }



        private void UpdateWaveBurstPhase(float elapseSeconds, SpawnFlowSettings settings)

        {

            if (m_IsBursting)

            {

                m_BurstSpawnTimer -= elapseSeconds;

                while (m_BurstSpawnTimer <= 0f && m_AliveEntityIds.Count < settings.MaxAliveCount)

                {

                    TrySpawnOne(settings);

                    m_BurstSpawnTimer += settings.BurstSpawnInterval;

                }



                if (m_AliveEntityIds.Count >= settings.MaxAliveCount)

                {

                    m_IsBursting = false;

                    m_WaveTimer = settings.WaveInterval;

                }



                return;

            }



            m_WaveTimer -= elapseSeconds;

            if (m_WaveTimer > 0f)

            {

                return;

            }



            if (m_AliveEntityIds.Count < settings.MaxAliveCount)

            {

                m_IsBursting = true;

                m_BurstSpawnTimer = 0f;

                return;

            }



            m_WaveTimer = settings.WaveInterval;

        }



        private bool IsAccelerationPhase(SpawnFlowSettings settings)

        {

            return m_ElapsedTime < settings.AccelerationDuration;

        }



        private void TrySpawnOne(SpawnFlowSettings settings)

        {

            if (m_AliveEntityIds.Count >= settings.MaxAliveCount)

            {

                return;

            }



            SpawnMonster(settings);

        }



        private float GetCurrentSpawnInterval(SpawnFlowSettings settings)

        {

            float t = Mathf.Clamp01(m_ElapsedTime / settings.AccelerationDuration);

            return Mathf.Lerp(settings.SpawnIntervalInitial, settings.SpawnIntervalMin, t);

        }



        private void PruneInactive()

        {

            for (int i = m_AliveEntityIds.Count - 1; i >= 0; i--)

            {

                int entityId = m_AliveEntityIds[i];

                if (GameEntry.Entity.GetGameEntity(entityId) == null)

                {

                    m_AliveEntityIds.RemoveAt(i);

                }

            }

        }



        private void SpawnMonster(SpawnFlowSettings settings)

        {

            float spawnY = ResolveSpawnHeight();

            float x = Random.Range(-settings.SpawnBound, settings.SpawnBound);

            float z = Random.Range(-settings.SpawnBound, settings.SpawnBound);

            Vector3 position = new Vector3(x, spawnY, z);



            CharacterEnum monsterType = PickRandomMonsterType();

            CharacterData data = CharacterData.Create(monsterType, position, Quaternion.identity);

            m_AliveEntityIds.Add(data.Id);

            GameEntry.Entity.ShowCharacter(data);

        }



        private float ResolveSpawnHeight()

        {

            CharacterEntity player = GameEntry.Entity.GetGameEntity(m_PlayerEntityId) as CharacterEntity;

            return player != null ? player.CachedTransform.position.y : 0f;

        }



        private CharacterEnum PickRandomMonsterType()

        {

            if (m_SpawnEntries == null || m_SpawnEntries.Length == 0)

            {

                return CharacterEnum.骑士机器人;

            }



            if (m_SpawnEntries.Length == 1 || m_TotalSpawnWeight <= 0f)

            {

                return m_SpawnEntries[0].Type;

            }



            float roll = Random.Range(0f, m_TotalSpawnWeight);

            float cumulative = 0f;

            for (int i = 0; i < m_SpawnEntries.Length; i++)

            {

                cumulative += m_SpawnEntries[i].Weight;

                if (roll < cumulative)

                {

                    return m_SpawnEntries[i].Type;

                }

            }



            return m_SpawnEntries[m_SpawnEntries.Length - 1].Type;

        }



        private static SpawnFlowSettings ResolveSpawnFlow()

        {

            return GameEntry.Juice != null ? GameEntry.Juice.SpawnFlow : s_DefaultSpawnFlow;

        }



        private static float CalculateTotalWeight(SpawnMonsterEntry[] spawnEntries)

        {

            if (spawnEntries == null || spawnEntries.Length == 0)

            {

                return 0f;

            }



            float totalWeight = 0f;

            for (int i = 0; i < spawnEntries.Length; i++)

            {

                totalWeight += Mathf.Max(0f, spawnEntries[i].Weight);

            }



            return totalWeight;

        }

    }

}


