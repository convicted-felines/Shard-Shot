using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    /// <summary>
    /// 进度观察运行时：按流程与信号创建观察实体，并持久化任务完成态。
    /// </summary>
    public sealed class ProgressObserverComponent : GameFrameworkComponent
    {
        private const string CompletedIdsSettingKey = "MainGame.ProgressObserver.CompletedIds";
        private const char CompletedIdSeparator = '|';

        private readonly HashSet<string> m_CompletedIdScratch = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> m_ActiveObserverIds = new HashSet<string>(StringComparer.Ordinal);
        private bool m_CompletedStoreInitialized;
        private Type m_CurrentProcedureType;

        public bool IsCompleted(string observerId)
        {
            if (string.IsNullOrEmpty(observerId))
            {
                return false;
            }

            EnsureCompletedStoreInitialized();
            LoadCompletedIdsInto(m_CompletedIdScratch);
            return m_CompletedIdScratch.Contains(observerId.Trim());
        }

        public void MarkCompleted(string observerId)
        {
            if (string.IsNullOrEmpty(observerId))
            {
                return;
            }

            EnsureCompletedStoreInitialized();
            LoadCompletedIdsInto(m_CompletedIdScratch);
            if (!m_CompletedIdScratch.Add(observerId.Trim()))
            {
                return;
            }

            SaveCompletedIdsFrom(m_CompletedIdScratch);
        }

        public void OnProcedureEnter(Type procedureType)
        {
            if (!isActiveAndEnabled || procedureType == null)
            {
                return;
            }

            m_CurrentProcedureType = procedureType;
            EvaluateSpawn(ProgressObserverSpawnMode.ProcedureEnter, ProgressObserverSignal.None);
        }

        public void OnProcedureLeave(Type procedureType)
        {
            if (procedureType == null || m_CurrentProcedureType != procedureType)
            {
                return;
            }

            m_CurrentProcedureType = null;
        }

        public void OnSignal(ProgressObserverSignal signal, object args = null)
        {
            if (!isActiveAndEnabled || signal == ProgressObserverSignal.None)
            {
                return;
            }

            EvaluateSpawn(ProgressObserverSpawnMode.OnSignal, signal, args);
        }

        public bool TrySpawn(string observerId)
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (!ProgressObserverRegistry.TryGetDefinition(observerId, out ProgressObserverDefinition definition))
            {
                Log.Warning("ProgressObserver: unknown observer id '{0}'.", observerId);
                return false;
            }

            return TrySpawnDefinition(definition);
        }

        internal void RegisterActiveObserver(string observerId)
        {
            if (!string.IsNullOrEmpty(observerId))
            {
                m_ActiveObserverIds.Add(observerId.Trim());
            }
        }

        internal void UnregisterActiveObserver(string observerId)
        {
            if (!string.IsNullOrEmpty(observerId))
            {
                m_ActiveObserverIds.Remove(observerId.Trim());
            }
        }

        private void EvaluateSpawn(ProgressObserverSpawnMode spawnMode, ProgressObserverSignal signal, object args = null)
        {
            IReadOnlyList<ProgressObserverDefinition> definitions = ProgressObserverRegistry.All;
            for (int i = 0; i < definitions.Count; i++)
            {
                ProgressObserverDefinition definition = definitions[i];
                if (definition == null || definition.SpawnMode != spawnMode)
                {
                    continue;
                }

                if (spawnMode == ProgressObserverSpawnMode.OnSignal && definition.SpawnSignal != signal)
                {
                    continue;
                }

                if (!MatchesCurrentProcedure(definition))
                {
                    continue;
                }

                TrySpawnDefinition(definition);
            }
        }

        private bool MatchesCurrentProcedure(ProgressObserverDefinition definition)
        {
            if (definition.ActiveInProcedures == null || definition.ActiveInProcedures.Length == 0)
            {
                return m_CurrentProcedureType != null;
            }

            if (m_CurrentProcedureType == null)
            {
                return false;
            }

            Type[] procedures = definition.ActiveInProcedures;
            for (int i = 0; i < procedures.Length; i++)
            {
                Type required = procedures[i];
                if (required == null)
                {
                    continue;
                }

                if (required == m_CurrentProcedureType || required.IsAssignableFrom(m_CurrentProcedureType))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TrySpawnDefinition(ProgressObserverDefinition definition)
        {
            if (definition == null || definition.LogicType == null || string.IsNullOrEmpty(definition.Id))
            {
                return false;
            }

            string id = definition.Id.Trim();
            if (IsCompleted(id))
            {
                return false;
            }

            if (m_ActiveObserverIds.Contains(id))
            {
                return false;
            }

            EntityComponent entityComponent = GameEntry.Entity;
            if (entityComponent == null)
            {
                Log.Warning("ProgressObserver: Entity is null, skip spawn '{0}'.", id);
                return false;
            }

            int entityId = CommonUtility.PermanentId;
            var data = new ProgressObserverEntityData(entityId, 0)
            {
                AssetName = "EmptyEntity",
                ObserverId = id,
            };

            entityComponent.ShowProgressObserverEntity(definition.LogicType, data);
            return true;
        }

        private void EnsureCompletedStoreInitialized()
        {
            if (m_CompletedStoreInitialized)
            {
                return;
            }

            m_CompletedStoreInitialized = true;
            SettingComponent setting = GameEntry.Setting;
            if (setting == null)
            {
                Log.Warning("ProgressObserver: Setting invalid, completion store init skipped.");
                return;
            }

            if (!setting.HasSetting(CompletedIdsSettingKey))
            {
                SaveCompletedIdsFrom(m_CompletedIdScratch);
            }
        }

        private void LoadCompletedIdsInto(HashSet<string> target)
        {
            target.Clear();
            SettingComponent setting = GameEntry.Setting;
            if (setting == null)
            {
                return;
            }

            string raw = setting.GetString(CompletedIdsSettingKey, string.Empty);
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            string[] parts = raw.Split(CompletedIdSeparator);
            for (int i = 0; i < parts.Length; i++)
            {
                string part = parts[i]?.Trim();
                if (!string.IsNullOrEmpty(part))
                {
                    target.Add(part);
                }
            }
        }

        private void SaveCompletedIdsFrom(HashSet<string> ids)
        {
            SettingComponent setting = GameEntry.Setting;
            if (setting == null)
            {
                return;
            }

            if (ids == null || ids.Count == 0)
            {
                setting.SetString(CompletedIdsSettingKey, string.Empty);
                setting.Save();
                return;
            }

            var list = new List<string>(ids);
            list.Sort(StringComparer.Ordinal);
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(CompletedIdSeparator);
                }

                sb.Append(list[i]);
            }

            setting.SetString(CompletedIdsSettingKey, sb.ToString());
            setting.Save();
        }
    }
}
