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
    /// 刷怪管理：在固定世界坐标范围内随机生成敌人，刷怪间隔随时间缩短直至上限。
    /// </summary>
    public sealed class SpawnManager
    {
        private const int MaxAliveCount = 40;
        private const float SpawnBound = 20f;
        private const float SpawnIntervalInitial = 5f;
        private const float SpawnIntervalMin = 0.5f;
        private const float AccelerationDuration = 20f;

        private readonly List<int> m_AliveEntityIds = new List<int>(MaxAliveCount);
        private readonly int m_PlayerEntityId;
        private readonly SpawnMonsterEntry[] m_SpawnEntries;
        private readonly float m_TotalSpawnWeight;

        private float m_ElapsedTime;
        private float m_SpawnTimer;

        public SpawnManager(int playerEntityId, SpawnMonsterEntry[] spawnEntries)
        {
            m_PlayerEntityId = playerEntityId;
            m_SpawnEntries = spawnEntries;
            m_TotalSpawnWeight = CalculateTotalWeight(spawnEntries);
        }

        public void Reset()
        {
            m_AliveEntityIds.Clear();
            m_ElapsedTime = 0f;
            m_SpawnTimer = SpawnIntervalInitial;
        }

        public void Update(float elapseSeconds)
        {
            m_ElapsedTime += elapseSeconds;
            m_SpawnTimer -= elapseSeconds;
            if (m_SpawnTimer > 0f)
            {
                return;
            }

            PruneInactive();
            if (m_AliveEntityIds.Count < MaxAliveCount)
            {
                SpawnMonster();
            }

            m_SpawnTimer = GetCurrentSpawnInterval();
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

        private float GetCurrentSpawnInterval()
        {
            float t = Mathf.Clamp01(m_ElapsedTime / AccelerationDuration);
            return Mathf.Lerp(SpawnIntervalInitial, SpawnIntervalMin, t);
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

        private void SpawnMonster()
        {
            float spawnY = ResolveSpawnHeight();
            float x = Random.Range(-SpawnBound, SpawnBound);
            float z = Random.Range(-SpawnBound, SpawnBound);
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
