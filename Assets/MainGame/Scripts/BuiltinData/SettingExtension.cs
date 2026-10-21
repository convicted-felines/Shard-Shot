using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using GameFramework;
using GameFramework.Localization;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{

    public static class SettingExtension
    {


    }

    #region 持久化数据辅助功能

    /// <summary>
    /// 持久化数据导出导入辅助功能
    /// </summary>
    public static class SettingPersistenceExtension
    {
        private const int SettingSnapshotVersion = 1;

        /// <summary>
        /// 将键值集合导出为设置快照 JSON。
        /// </summary>
        public static string ExportSettingsSnapshotJson(IDictionary<string, string> settings)
        {
            StringBuilder jsonBuilder = new StringBuilder(256);
            jsonBuilder.Append("{\n");
            jsonBuilder.Append("    \"version\": ").Append(SettingSnapshotVersion).Append(",\n");
            jsonBuilder.Append("    \"entries\": [\n");

            bool isFirstEntry = true;
            if (settings != null)
            {
                foreach (KeyValuePair<string, string> pair in settings)
                {
                    if (string.IsNullOrEmpty(pair.Key))
                    {
                        continue;
                    }

                    if (!isFirstEntry)
                    {
                        jsonBuilder.Append(",\n");
                    }

                    isFirstEntry = false;
                    jsonBuilder.Append("        {\n");
                    jsonBuilder.Append("            \"key\": ");
                    AppendJsonString(jsonBuilder, pair.Key);
                    jsonBuilder.Append(",\n");
                    jsonBuilder.Append("            \"value\": ");
                    AppendJsonString(jsonBuilder, pair.Value ?? string.Empty);
                    jsonBuilder.Append("\n        }");
                }
            }

            jsonBuilder.Append("\n    ]\n}");
            return jsonBuilder.ToString();
        }

        private static void AppendJsonString(StringBuilder jsonBuilder, string value)
        {
            jsonBuilder.Append('"');
            if (string.IsNullOrEmpty(value))
            {
                jsonBuilder.Append('"');
                return;
            }

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '\\':
                        jsonBuilder.Append("\\\\");
                        break;
                    case '"':
                        jsonBuilder.Append("\\\"");
                        break;
                    case '\b':
                        jsonBuilder.Append("\\b");
                        break;
                    case '\f':
                        jsonBuilder.Append("\\f");
                        break;
                    case '\n':
                        jsonBuilder.Append("\\n");
                        break;
                    case '\r':
                        jsonBuilder.Append("\\r");
                        break;
                    case '\t':
                        jsonBuilder.Append("\\t");
                        break;
                    default:
                        if (character < 32)
                        {
                            jsonBuilder.Append("\\u");
                            jsonBuilder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            jsonBuilder.Append(character);
                        }

                        break;
                }
            }

            jsonBuilder.Append('"');
        }

        /// <summary>
        /// 解析设置快照 JSON 到目标键值集合。
        /// </summary>
        public static bool TryParseSettingsSnapshotJson(string json, IDictionary<string, string> settings)
        {
            if (settings == null || string.IsNullOrEmpty(json))
            {
                return false;
            }

            GameFrameworkJsonHelperBootstrap.EnsureJsonHelper();

            SettingSnapshotPayload payload = null;
            try
            {
                payload = Utility.Json.ToObject<SettingSnapshotPayload>(json);
            }
            catch (Exception exception)
            {
                Log.Warning("Parse settings snapshot json failed, exception '{0}'.", exception);
                return false;
            }

            if (payload == null)
            {
                return false;
            }

            settings.Clear();
            List<SettingSnapshotEntry> entries = payload.entries ?? new List<SettingSnapshotEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                SettingSnapshotEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                settings[entry.key] = entry.value ?? string.Empty;
            }

            return true;
        }

        /// <summary>
        /// 导出当前全部本地持久化数据为 JSON 字符串。
        /// </summary>
        public static string ExportAllSettingsJson(this SettingComponent settingComponent)
        {
            if (settingComponent == null || settingComponent.Count < 0)
            {
                return ExportSettingsSnapshotJson(null);
            }

            string[] settingNames = settingComponent.GetAllSettingNames();
            if (settingNames == null || settingNames.Length == 0)
            {
                return ExportSettingsSnapshotJson(null);
            }

            Dictionary<string, string> settings = new Dictionary<string, string>(settingNames.Length, StringComparer.Ordinal);
            for (int i = 0; i < settingNames.Length; i++)
            {
                string settingName = settingNames[i];
                if (string.IsNullOrEmpty(settingName))
                {
                    continue;
                }

                settings[settingName] = settingComponent.GetString(settingName, string.Empty) ?? string.Empty;
            }

            return ExportSettingsSnapshotJson(settings);
        }

        /// <summary>
        /// 将 JSON 中的全部数据强制覆盖到当前本地持久化数据。
        /// </summary>
        public static SettingSnapshotImportResult ImportAllSettingsJson(this SettingComponent settingComponent, string json, bool clearExistingSettings = true)
        {
            SettingSnapshotImportResult result = new SettingSnapshotImportResult();
            if (settingComponent == null || settingComponent.Count < 0 || string.IsNullOrEmpty(json))
            {
                return result;
            }

            Dictionary<string, string> importedSettings = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!TryParseSettingsSnapshotJson(json, importedSettings))
            {
                return result;
            }

            if (clearExistingSettings)
            {
                result.RemovedCount = settingComponent.Count > 0 ? settingComponent.Count : 0;
                settingComponent.RemoveAllSettings();
            }

            foreach (KeyValuePair<string, string> pair in importedSettings)
            {
                if (string.IsNullOrEmpty(pair.Key))
                {
                    continue;
                }

                settingComponent.SetString(pair.Key, pair.Value ?? string.Empty);
                result.ImportedCount++;
            }

            result.Succeeded = true;
            return result;
        }

        [Serializable]
        public sealed class SettingSnapshotPayload
        {
            public int version = SettingSnapshotVersion;
            public List<SettingSnapshotEntry> entries = new List<SettingSnapshotEntry>();
        }

        [Serializable]
        public sealed class SettingSnapshotEntry
        {
            public string key;
            public string value;
        }
    }

    public struct SettingSnapshotImportResult
    {
        public int ImportedCount;
        public int RemovedCount;
        public bool Succeeded;
    }

    #endregion

    #region 云同步
    /// <summary>
    /// 云同步扩展功能
    /// </summary>
    public static class SettingCloudSyncExtension
    {
        private const int CloudSyncVersion = 1;
        private static CloudSyncRuntimeOptions s_RuntimeOptions = CloudSyncRuntimeOptions.Default;

        public static CloudSyncRuntimeOptions RuntimeOptions => s_RuntimeOptions;

        /// <summary>
        /// 设置云同步运行时选项。
        /// </summary>
        public static void ConfigureCloudSyncRuntime(CloudSyncRuntimeOptions options)
        {
            s_RuntimeOptions = options.Normalize();
        }

        /// <summary>
        /// 将指定键登记到云同步注册表。
        /// </summary>
        public static void TrackCloudSyncedKey(this SettingComponent settingComponent, string settingKey, string valueType)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return;
            }

            CloudSyncRegistry registry = LoadRegistry(settingComponent);
            registry.Upsert(settingKey, CommonUtility.NormalizeSettingValueType(valueType));
            SaveRegistry(settingComponent, registry);
        }

        /// <summary>
        /// 仅在键已存在时登记到云同步注册表。
        /// </summary>
        public static void TrackCloudSyncedKeyIfExists(this SettingComponent settingComponent, string settingKey, string valueType)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey) || !settingComponent.HasSetting(settingKey))
            {
                return;
            }

            TrackCloudSyncedKey(settingComponent, settingKey, valueType);
        }

        /// <summary>
        /// 写入布尔值并尝试登记云同步。
        /// </summary>
        public static void SetCloudSyncedBool(this SettingComponent settingComponent, string settingKey, bool value)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return;
            }

            TrySetCloudSyncedValue(settingComponent, settingKey, "bool");
            settingComponent.SetBool(settingKey, value);
            MarkCloudSyncPendingUpload(settingComponent);
        }

        /// <summary>
        /// 写入整型值并尝试登记云同步。
        /// </summary>
        public static void SetCloudSyncedInt(this SettingComponent settingComponent, string settingKey, int value)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return;
            }

            TrySetCloudSyncedValue(settingComponent, settingKey, "int");
            settingComponent.SetInt(settingKey, value);
            MarkCloudSyncPendingUpload(settingComponent);
        }

        /// <summary>
        /// 写入浮点值并尝试登记云同步。
        /// </summary>
        public static void SetCloudSyncedFloat(this SettingComponent settingComponent, string settingKey, float value)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return;
            }

            TrySetCloudSyncedValue(settingComponent, settingKey, "float");
            settingComponent.SetFloat(settingKey, value);
            MarkCloudSyncPendingUpload(settingComponent);
        }

        /// <summary>
        /// 写入字符串并尝试登记云同步。
        /// </summary>
        public static void SetCloudSyncedString(this SettingComponent settingComponent, string settingKey, string value)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return;
            }

            TrySetCloudSyncedValue(settingComponent, settingKey, "string");
            settingComponent.SetString(settingKey, value ?? string.Empty);
            MarkCloudSyncPendingUpload(settingComponent);
        }

        /// <summary>
        /// 删除本地键并从云同步注册表中移除。
        /// </summary>
        public static bool RemoveCloudSyncedKey(this SettingComponent settingComponent, string settingKey)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return false;
            }

            bool removed = false;
            if (settingComponent.HasSetting(settingKey))
            {
                settingComponent.RemoveSetting(settingKey);
                removed = true;
            }

            CloudSyncRegistry registry = LoadRegistry(settingComponent);
            if (registry.Remove(settingKey))
            {
                SaveRegistry(settingComponent, registry);
                removed = true;
            }

            if (removed)
            {
                MarkCloudSyncPendingUpload(settingComponent);
            }

            return removed;
        }

        /// <summary>
        /// 导出当前可同步键为云端 JSON。
        /// </summary>
        public static string ExportCloudSyncJson(this SettingComponent settingComponent)
        {
            if (!s_RuntimeOptions.EnableCloudSync)
            {
                return "{\"version\":1,\"entries\":[]}";
            }

            if (settingComponent == null)
            {
                return "{\"version\":1,\"entries\":[]}";
            }

            CloudSyncRegistry registry = LoadRegistry(settingComponent);
            CloudSyncPayload payload = new CloudSyncPayload { version = CloudSyncVersion };
            bool registryChanged = false;

            for (int i = registry.entries.Count - 1; i >= 0; i--)
            {
                CloudSyncRegistryEntry item = registry.entries[i];
                if (string.IsNullOrEmpty(item.key))
                {
                    registry.entries.RemoveAt(i);
                    registryChanged = true;
                    continue;
                }

                if (!settingComponent.HasSetting(item.key))
                {
                    registry.entries.RemoveAt(i);
                    registryChanged = true;
                    continue;
                }

                if (!PassesCloudKeyFilter(item.key))
                {
                    continue;
                }

                string rawValue = ReadSettingAsRaw(settingComponent, item.key, item.type);
                payload.entries.Add(new CloudSyncPayloadEntry
                {
                    key = item.key,
                    type = item.type,
                    value = rawValue
                });
            }

            if (registryChanged)
            {
                SaveRegistry(settingComponent, registry);
            }

            return Utility.Json.ToJson(payload);
        }

        /// <summary>
        /// 本地是否存在尚未成功上传到云端的改动。
        /// </summary>
        public static bool HasPendingCloudSyncChanges(this SettingComponent settingComponent)
        {
            return settingComponent != null && settingComponent.GetBool(Constant.Setting.CloudSyncDirty, false);
        }

        /// <summary>
        /// 当本地可云同步数据发生变化时标记为待上传。
        /// </summary>
        public static void MarkCloudSyncPendingUpload(this SettingComponent settingComponent)
        {
            SetCloudSyncDirtyFlag(settingComponent, true);
        }

        /// <summary>
        /// 当云端上传成功后清理本地待上传标记。
        /// 服务器接入后可在上传成功回调中调用。
        /// </summary>
        public static void MarkCloudSyncUploadSucceeded(this SettingComponent settingComponent)
        {
            SetCloudSyncDirtyFlag(settingComponent, false);
        }

        /// <summary>
        /// 当前是否应优先保留本地数据而不是导入云端数据。
        /// </summary>
        public static bool ShouldPreferLocalCloudSyncData(this SettingComponent settingComponent)
        {
            return HasPendingCloudSyncChanges(settingComponent);
        }

        /// <summary>
        /// 导入云端 JSON 并按策略合并到本地设置。
        /// </summary>
        public static CloudSyncImportResult ImportCloudSyncJson(this SettingComponent settingComponent, string cloudJson, bool removeLocalKeysNotInCloud = true, bool forceImportWhenLocalDirty = false)
        {
            if (!s_RuntimeOptions.EnableCloudSync)
            {
                return new CloudSyncImportResult();
            }

            CloudSyncImportResult result = new CloudSyncImportResult();
            if (settingComponent == null || string.IsNullOrEmpty(cloudJson))
            {
                return result;
            }

            if (!forceImportWhenLocalDirty && ShouldPreferLocalCloudSyncData(settingComponent))
            {
                result.SkippedDueToPendingLocalChanges = true;
                return result;
            }

            CloudSyncPayload payload = null;
            try
            {
                payload = Utility.Json.ToObject<CloudSyncPayload>(cloudJson);
            }
            catch (Exception exception)
            {
                Log.Warning("Import cloud json failed, exception '{0}'.", exception);
                return result;
            }

            if (payload == null)
            {
                return result;
            }

            CloudSyncRegistry registry = LoadRegistry(settingComponent);
            HashSet<string> incomingKeys = new HashSet<string>(StringComparer.Ordinal);
            List<CloudSyncPayloadEntry> entries = payload.entries ?? new List<CloudSyncPayloadEntry>();
            for (int i = 0; i < entries.Count; i++)
            {
                CloudSyncPayloadEntry entry = entries[i];
                if (entry == null || string.IsNullOrEmpty(entry.key))
                {
                    continue;
                }

                if (!PassesCloudKeyFilter(entry.key))
                {
                    continue;
                }

                string normalizedType = CommonUtility.NormalizeSettingValueType(entry.type);
                string normalizedValue = entry.value ?? string.Empty;
                ApplyRawSetting(settingComponent, entry.key, normalizedType, normalizedValue);
                registry.Upsert(entry.key, normalizedType);

                incomingKeys.Add(entry.key);
                result.UpsertedCount++;
            }

            bool shouldRemoveLocalKeys = removeLocalKeysNotInCloud;
            if (!s_RuntimeOptions.ForceMethodImportBehavior)
            {
                shouldRemoveLocalKeys = s_RuntimeOptions.RemoveLocalKeysNotInCloudOnImport;
            }

            if (shouldRemoveLocalKeys)
            {
                for (int i = registry.entries.Count - 1; i >= 0; i--)
                {
                    CloudSyncRegistryEntry item = registry.entries[i];
                    if (string.IsNullOrEmpty(item.key) || incomingKeys.Contains(item.key))
                    {
                        continue;
                    }

                    if (settingComponent.HasSetting(item.key))
                    {
                        settingComponent.RemoveSetting(item.key);
                    }

                    registry.entries.RemoveAt(i);
                    result.RemovedCount++;
                }
            }

            SaveRegistry(settingComponent, registry);
            if (s_RuntimeOptions.SaveAfterImport)
            {
                settingComponent.Save();
            }

            MarkCloudSyncUploadSucceeded(settingComponent);
            return result;
        }

        /// <summary>
        /// 根据运行时配置登记一个可同步键。
        /// </summary>
        private static bool TrySetCloudSyncedValue(SettingComponent settingComponent, string settingKey, string valueType)
        {
            if (settingComponent == null || string.IsNullOrEmpty(settingKey))
            {
                return false;
            }

            if (!s_RuntimeOptions.EnableCloudSync || !s_RuntimeOptions.TrackOnLocalWrite)
            {
                return false;
            }

            if (!PassesCloudKeyFilter(settingKey))
            {
                return false;
            }

            CloudSyncRegistry registry = LoadRegistry(settingComponent);
            registry.Upsert(settingKey, CommonUtility.NormalizeSettingValueType(valueType));
            SaveRegistry(settingComponent, registry);
            return true;
        }

        /// <summary>
        /// 判断键是否通过前缀过滤规则。
        /// </summary>
        private static bool PassesCloudKeyFilter(string settingKey)
        {
            if (string.IsNullOrEmpty(settingKey))
            {
                return false;
            }

            if (s_RuntimeOptions.CloudKeyPrefixes == null || s_RuntimeOptions.CloudKeyPrefixes.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < s_RuntimeOptions.CloudKeyPrefixes.Length; i++)
            {
                string prefix = s_RuntimeOptions.CloudKeyPrefixes[i];
                if (string.IsNullOrEmpty(prefix))
                {
                    continue;
                }

                if (settingKey.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 按类型读取本地值并转换为字符串。
        /// </summary>
        private static string ReadSettingAsRaw(SettingComponent settingComponent, string settingKey, string valueType)
        {
            switch (CommonUtility.NormalizeSettingValueType(valueType))
            {
                case "bool":
                    return settingComponent.GetBool(settingKey, false) ? "1" : "0";
                case "int":
                    return settingComponent.GetInt(settingKey, 0).ToString(CultureInfo.InvariantCulture);
                case "float":
                    return settingComponent.GetFloat(settingKey, 0f).ToString("R", CultureInfo.InvariantCulture);
                default:
                    return settingComponent.GetString(settingKey, string.Empty) ?? string.Empty;
            }
        }

        /// <summary>
        /// 按类型将字符串值写回本地设置。
        /// </summary>
        private static void ApplyRawSetting(SettingComponent settingComponent, string settingKey, string valueType, string rawValue)
        {
            string normalizedType = CommonUtility.NormalizeSettingValueType(valueType);
            switch (normalizedType)
            {
                case "bool":
                    settingComponent.SetBool(settingKey, CommonUtility.ParseBoolString(rawValue));
                    break;
                case "int":
                    if (int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
                    {
                        settingComponent.SetInt(settingKey, intValue);
                    }
                    else
                    {
                        settingComponent.SetInt(settingKey, 0);
                    }
                    break;
                case "float":
                    if (float.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                    {
                        settingComponent.SetFloat(settingKey, floatValue);
                    }
                    else
                    {
                        settingComponent.SetFloat(settingKey, 0f);
                    }
                    break;
                default:
                    settingComponent.SetString(settingKey, rawValue ?? string.Empty);
                    break;
            }
        }

        /// <summary>
        /// 读取云同步注册表，失败时返回空表。
        /// </summary>
        private static CloudSyncRegistry LoadRegistry(SettingComponent settingComponent)
        {
            CloudSyncRegistry registry = new CloudSyncRegistry();
            string registryJson = settingComponent.GetString(Constant.Setting.CloudSyncRegistry, string.Empty);
            if (string.IsNullOrEmpty(registryJson))
            {
                return registry;
            }

            try
            {
                CloudSyncRegistry loaded = Utility.Json.ToObject<CloudSyncRegistry>(registryJson);
                return loaded ?? registry;
            }
            catch (Exception exception)
            {
                Log.Warning("Load cloud sync registry failed, exception '{0}'.", exception);
                return registry;
            }
        }

        /// <summary>
        /// 保存云同步注册表与版本号。
        /// </summary>
        private static void SaveRegistry(SettingComponent settingComponent, CloudSyncRegistry registry)
        {
            registry.version = CloudSyncVersion;
            settingComponent.SetString(Constant.Setting.CloudSyncRegistry, Utility.Json.ToJson(registry));
            settingComponent.SetInt(Constant.Setting.CloudSyncSchemaVersion, CloudSyncVersion);
        }

        /// <summary>
        /// 写入本地待上传状态，不参与云同步注册表。
        /// </summary>
        private static void SetCloudSyncDirtyFlag(SettingComponent settingComponent, bool isDirty)
        {
            if (settingComponent == null)
            {
                return;
            }

            settingComponent.SetBool(Constant.Setting.CloudSyncDirty, isDirty);
        }

        [Serializable]
        private sealed class CloudSyncPayload
        {
            public int version = CloudSyncVersion;
            public List<CloudSyncPayloadEntry> entries = new List<CloudSyncPayloadEntry>();
        }

        [Serializable]
        private sealed class CloudSyncPayloadEntry
        {
            public string key;
            public string type;
            public string value;
        }

        [Serializable]
        private sealed class CloudSyncRegistry
        {
            public int version = CloudSyncVersion;
            public List<CloudSyncRegistryEntry> entries = new List<CloudSyncRegistryEntry>();

            /// <summary>
            /// 新增或更新一个注册项。
            /// </summary>
            public void Upsert(string key, string valueType)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
                    {
                        entries[i].type = valueType;
                        return;
                    }
                }

                entries.Add(new CloudSyncRegistryEntry { key = key, type = valueType });
            }

            /// <summary>
            /// 按键删除注册项。
            /// </summary>
            public bool Remove(string key)
            {
                for (int i = entries.Count - 1; i >= 0; i--)
                {
                    if (string.Equals(entries[i].key, key, StringComparison.Ordinal))
                    {
                        entries.RemoveAt(i);
                        return true;
                    }
                }

                return false;
            }
        }

        [Serializable]
        private sealed class CloudSyncRegistryEntry
        {
            public string key;
            public string type = "string";
        }
    }

    public struct CloudSyncImportResult
    {
        public int UpsertedCount;
        public int RemovedCount;
        public bool SkippedDueToPendingLocalChanges;
    }

    [Serializable]
    public struct CloudSyncRuntimeOptions
    {
        public bool EnableCloudSync;
        public bool TrackOnLocalWrite;
        public bool RemoveLocalKeysNotInCloudOnImport;
        public bool SaveAfterImport;
        public bool ForceMethodImportBehavior;
        public string[] CloudKeyPrefixes;

        public static CloudSyncRuntimeOptions Default => new CloudSyncRuntimeOptions
        {
            EnableCloudSync = true,
            TrackOnLocalWrite = true,
            RemoveLocalKeysNotInCloudOnImport = true,
            SaveAfterImport = true,
            ForceMethodImportBehavior = true,
            CloudKeyPrefixes = Array.Empty<string>()
        };

        /// <summary>
        /// 规范化配置内容，补齐空数组默认值。
        /// </summary>
        public CloudSyncRuntimeOptions Normalize()
        {
            CloudSyncRuntimeOptions normalized = this;
            if (normalized.CloudKeyPrefixes == null)
            {
                normalized.CloudKeyPrefixes = Array.Empty<string>();
            }

            return normalized;
        }
    }

    #endregion


}
