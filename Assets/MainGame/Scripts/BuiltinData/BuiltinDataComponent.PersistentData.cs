//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public partial class BuiltinDataComponent : GameFrameworkComponent
    {
        #region 游戏持久数据实用功能

        #region 通用持久数据操作

        public void ClearAllData()
        {
            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                Log.Warning("Setting is invalid, ClearAllData skipped.");
                return;
            }

            if (Application.isPlaying)
            {
                setting.RemoveAllSettings();
                setting.Save();
                return;
            }



#if UNITY_EDITOR
            if (!TryClearAllSettingsInEditor(setting, out string errorMessage))
            {
                Log.Warning("Clear all settings in editor failed, error message '{0}'.", errorMessage);
            }
#endif
        }

        private SettingComponent GetAvailableSettingComponent()
        {
            if (GameEntry.Setting != null)
            {
                return GameEntry.Setting;
            }

#if UNITY_EDITOR
            SettingComponent setting = GetComponent<SettingComponent>();
            if (setting != null)
            {
                return setting;
            }

            setting = GetComponentInParent<SettingComponent>();
            if (setting != null)
            {
                return setting;
            }

            return FindObjectOfType<SettingComponent>();
#else
            return null;
#endif
        }

        #endregion

#if UNITY_EDITOR
        #region Editor 持久数据快照工具

        public bool EditorTryClearAllPersistentData(out string errorMessage)
        {
            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            if (Application.isPlaying)
            {
                setting.RemoveAllSettings();
                setting.Save();
                errorMessage = null;
                return true;
            }

            return TryClearAllSettingsInEditor(setting, out errorMessage);
        }

        /// <summary>
        /// 写入一条字符串持久化配置（运行时使用 SettingComponent；编辑器非运行态按 Helper 类型写文件或 PlayerPrefs）。
        /// </summary>
        public bool EditorTrySetPersistentString(string key, string value, out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                errorMessage = "键不能为空。";
                return false;
            }

            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            string valueToStore = value ?? string.Empty;

            if (Application.isPlaying)
            {
                setting.SetString(key, valueToStore);
                setting.Save();
                errorMessage = null;
                return true;
            }

            switch (GetEditorSettingStorageMode(setting))
            {
                case EditorSettingStorageMode.DefaultFile:
                    if (!TryLoadAllSettingsInEditor(setting, out SortedDictionary<string, string> settings, out errorMessage))
                    {
                        return false;
                    }

                    settings[key] = valueToStore;
                    if (!TrySaveAllSettingsToDefaultFile(settings, out errorMessage))
                    {
                        return false;
                    }

                    errorMessage = null;
                    return true;

                case EditorSettingStorageMode.PlayerPrefs:
                    PlayerPrefs.SetString(key, valueToStore);
                    PlayerPrefs.Save();
                    errorMessage = null;
                    return true;

                default:
                    errorMessage = "当前 Setting Helper 不支持编辑器态直接写入单条持久数据。";
                    return false;
            }
        }

        /// <summary>
        /// 枚举所有键名以指定前缀开头的持久数据项。
        /// </summary>
        public bool EditorTryGetPersistentDataKeysByPrefix(string keyPrefix, out List<string> matchedKeys, out string errorMessage)
        {
            matchedKeys = new List<string>();
            if (string.IsNullOrWhiteSpace(keyPrefix))
            {
                errorMessage = "键前缀不能为空。";
                return false;
            }

            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            if (Application.isPlaying)
            {
                if (setting.Count < 0)
                {
                    errorMessage = "当前 Setting Helper 不支持枚举全部键。";
                    return false;
                }

                string[] settingNames = setting.GetAllSettingNames();
                if (settingNames == null)
                {
                    errorMessage = "当前 Setting Helper 不支持枚举全部键。";
                    return false;
                }

                CollectSettingNamesByPrefix(settingNames, keyPrefix, matchedKeys);
                errorMessage = null;
                return true;
            }

            if (!TryLoadAllSettingsInEditor(setting, out SortedDictionary<string, string> settings, out errorMessage))
            {
                return false;
            }

            foreach (string settingKey in settings.Keys)
            {
                if (settingKey.StartsWith(keyPrefix, StringComparison.Ordinal))
                {
                    matchedKeys.Add(settingKey);
                }
            }

            errorMessage = null;
            return true;
        }

        /// <summary>
        /// 删除所有键名以指定前缀开头的持久数据项。
        /// </summary>
        public bool EditorTryDeletePersistentDataByKeyPrefix(string keyPrefix, out int deletedCount, out string errorMessage)
        {
            deletedCount = 0;
            if (!EditorTryGetPersistentDataKeysByPrefix(keyPrefix, out List<string> matchedKeys, out errorMessage))
            {
                return false;
            }

            if (matchedKeys.Count == 0)
            {
                errorMessage = null;
                return true;
            }

            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            if (Application.isPlaying)
            {
                for (int i = 0; i < matchedKeys.Count; i++)
                {
                    setting.RemoveSetting(matchedKeys[i]);
                }

                setting.Save();
                deletedCount = matchedKeys.Count;
                errorMessage = null;
                return true;
            }

            switch (GetEditorSettingStorageMode(setting))
            {
                case EditorSettingStorageMode.DefaultFile:
                    if (!TryLoadAllSettingsInEditor(setting, out SortedDictionary<string, string> settings, out errorMessage))
                    {
                        return false;
                    }

                    for (int i = 0; i < matchedKeys.Count; i++)
                    {
                        settings.Remove(matchedKeys[i]);
                    }

                    if (!TrySaveAllSettingsToDefaultFile(settings, out errorMessage))
                    {
                        return false;
                    }

                    deletedCount = matchedKeys.Count;
                    errorMessage = null;
                    return true;

                default:
                    errorMessage = "当前 Setting Helper 不支持编辑器态按前缀删除持久数据。";
                    return false;
            }
        }

        private static void CollectSettingNamesByPrefix(string[] settingNames, string keyPrefix, List<string> matchedKeys)
        {
            for (int i = 0; i < settingNames.Length; i++)
            {
                string settingName = settingNames[i];
                if (!string.IsNullOrEmpty(settingName) && settingName.StartsWith(keyPrefix, StringComparison.Ordinal))
                {
                    matchedKeys.Add(settingName);
                }
            }
        }

        public bool EditorTryExportAllPersistentDataJson(out string json, out int exportedCount, out string errorMessage)
        {
            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                json = null;
                exportedCount = 0;
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            if (Application.isPlaying)
            {
                if (setting.Count < 0)
                {
                    json = null;
                    exportedCount = 0;
                    errorMessage = "当前 Setting Helper 不支持完整快照导出。";
                    return false;
                }

                string[] settingNames = setting.GetAllSettingNames();
                if (settingNames == null)
                {
                    json = null;
                    exportedCount = 0;
                    errorMessage = "当前 Setting Helper 不支持枚举全部键。";
                    return false;
                }

                json = setting.ExportAllSettingsJson();
                exportedCount = settingNames.Length;
                errorMessage = null;
                return true;
            }

            return TryExportAllSettingsJsonInEditor(setting, out json, out exportedCount, out errorMessage);
        }

        public bool EditorTryImportAllPersistentDataJson(string json, out SettingSnapshotImportResult result, out string errorMessage)
        {
            SettingComponent setting = GetAvailableSettingComponent();
            if (setting == null)
            {
                result = new SettingSnapshotImportResult();
                errorMessage = "未找到 SettingComponent，请确认场景中已挂载该组件。";
                return false;
            }

            if (Application.isPlaying)
            {
                if (setting.Count < 0)
                {
                    result = new SettingSnapshotImportResult();
                    errorMessage = "当前 Setting Helper 不支持完整快照导入。";
                    return false;
                }

                result = setting.ImportAllSettingsJson(json, true);
                if (!result.Succeeded)
                {
                    errorMessage = "JSON 格式无效，或无法解析为设置快照。";
                    return false;
                }

                setting.Save();
                errorMessage = null;
                return true;
            }

            return TryImportAllSettingsJsonInEditor(setting, json, out result, out errorMessage);
        }

        public bool EditorTryExportAllPersistentDataJsonToDefaultFile(out string filePath, out int exportedCount, out string errorMessage)
        {
            filePath = GetPersistentDataJsonFullPath();
            if (!EnsurePersistentDataJsonDirectory(out errorMessage))
            {
                exportedCount = 0;
                return false;
            }

            if (!EditorTryExportAllPersistentDataJson(out string exportJson, out exportedCount, out errorMessage))
            {
                return false;
            }

            try
            {
                File.WriteAllText(filePath, exportJson);
                AssetDatabase.Refresh();
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        public bool EditorTryExportAllPersistentDataJsonToNewFile(out string filePath, out int exportedCount, out string errorMessage)
        {
            if (!EnsurePersistentDataJsonDirectory(out errorMessage))
            {
                filePath = null;
                exportedCount = 0;
                return false;
            }

            filePath = GetUniquePersistentDataJsonFullPath();
            if (!EditorTryExportAllPersistentDataJson(out string exportJson, out exportedCount, out errorMessage))
            {
                return false;
            }

            try
            {
                File.WriteAllText(filePath, exportJson);
                AssetDatabase.Refresh();
                errorMessage = null;
                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        public bool EditorTryImportAllPersistentDataJsonFromDefaultFile(out SettingSnapshotImportResult result, out string filePath, out string errorMessage)
        {
            filePath = GetPersistentDataJsonFullPath();
            result = new SettingSnapshotImportResult();
            if (!File.Exists(filePath))
            {
                errorMessage = $"未找到 JSON 文件：{GetPersistentDataJsonAssetPath()}";
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return EditorTryImportAllPersistentDataJson(json, out result, out errorMessage);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        public bool EditorTryImportAllPersistentDataJsonFromFile(string filePath, out SettingSnapshotImportResult result, out string errorMessage)
        {
            result = new SettingSnapshotImportResult();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "JSON 文件路径不能为空。";
                return false;
            }

            filePath = Utility.Path.GetRegularPath(Path.GetFullPath(filePath));
            if (!File.Exists(filePath))
            {
                errorMessage = $"未找到 JSON 文件：{filePath}";
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return EditorTryImportAllPersistentDataJson(json, out result, out errorMessage);
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        public static string GetPersistentDataJsonAssetPath()
        {
            return Utility.Path.GetRegularPath(Path.Combine(PersistentDataJsonFolderAssetPath, PersistentDataJsonFileName));
        }

        public static string GetPersistentDataJsonDirectoryAssetPath()
        {
            return PersistentDataJsonFolderAssetPath;
        }

        [ContextMenu("删除全部持久数据")]
        private void EditorClearAllPersistentData()
        {
            if (!EditorUtility.DisplayDialog("确认删除", "确定要删除全部持久数据吗？该操作会立即保存到本地。", "删除全部", "取消"))
            {
                return;
            }

            if (!EditorTryClearAllPersistentData(out string errorMessage))
            {
                EditorUtility.DisplayDialog("删除失败", errorMessage, "确定");
                return;
            }

            Debug.Log("已删除全部持久数据。");
        }

        [ContextMenu("导出全部持久数据为 JSON")]
        private void EditorExportAllPersistentDataToJson()
        {
            if (!EditorTryExportAllPersistentDataJsonToNewFile(out string filePath, out int exportedCount, out string errorMessage))
            {
                EditorUtility.DisplayDialog("导出失败", errorMessage, "确定");
                return;
            }

            EditorUtility.RevealInFinder(filePath);
            EditorUtility.DisplayDialog("导出成功", string.Format("已导出 {0} 条持久数据到 {1}。", exportedCount, filePath), "确定");
        }

        [ContextMenu("从 JSON 强制覆盖全部持久数据")]
        private void EditorImportAllPersistentDataFromJson()
        {
            if (!EditorUtility.DisplayDialog("确认覆盖导入", "导入会先清空当前全部持久数据，再使用 JSON 中的数据强制覆盖，是否继续？", "继续导入", "取消"))
            {
                return;
            }

            string filePath = EditorUtility.OpenFilePanel("选择要导入的持久数据 JSON", GetPersistentDataJsonDirectoryFullPath(), "json");
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (!EditorTryImportAllPersistentDataJsonFromFile(filePath, out SettingSnapshotImportResult result, out string errorMessage))
            {
                EditorUtility.DisplayDialog("导入失败", errorMessage, "确定");
                return;
            }

            EditorUtility.DisplayDialog("导入成功", string.Format("已从 {0} 导入，清空 {1} 条旧数据，并导入 {2} 条新数据。", filePath, result.RemovedCount, result.ImportedCount), "确定");
        }

        private bool TryClearAllSettingsInEditor(SettingComponent setting, out string errorMessage)
        {
            errorMessage = null;
            switch (GetEditorSettingStorageMode(setting))
            {
                case EditorSettingStorageMode.DefaultFile:
                    string filePath = GetDefaultSettingFilePath();
                    try
                    {
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }

                        return true;
                    }
                    catch (Exception exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }

                case EditorSettingStorageMode.PlayerPrefs:
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    return true;

                default:
                    errorMessage = "当前 Setting Helper 不支持编辑器态直接清空。";
                    return false;
            }
        }

        private bool TryExportAllSettingsJsonInEditor(SettingComponent setting, out string json, out int settingCount, out string errorMessage)
        {
            json = null;
            settingCount = 0;
            errorMessage = null;

            if (!TryLoadAllSettingsInEditor(setting, out SortedDictionary<string, string> settings, out errorMessage))
            {
                return false;
            }

            json = SettingPersistenceExtension.ExportSettingsSnapshotJson(settings);
            settingCount = settings.Count;
            return true;
        }

        private bool TryImportAllSettingsJsonInEditor(SettingComponent setting, string json, out SettingSnapshotImportResult result, out string errorMessage)
        {
            result = new SettingSnapshotImportResult();
            errorMessage = null;

            SortedDictionary<string, string> importedSettings = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (!SettingPersistenceExtension.TryParseSettingsSnapshotJson(json, importedSettings))
            {
                errorMessage = "JSON 格式无效，或无法解析为设置快照。";
                return false;
            }

            switch (GetEditorSettingStorageMode(setting))
            {
                case EditorSettingStorageMode.DefaultFile:
                    if (!TryLoadAllSettingsInEditor(setting, out SortedDictionary<string, string> existingSettings, out errorMessage))
                    {
                        return false;
                    }

                    result.RemovedCount = existingSettings.Count;
                    if (!TrySaveAllSettingsToDefaultFile(importedSettings, out errorMessage))
                    {
                        return false;
                    }

                    result.ImportedCount = importedSettings.Count;
                    result.Succeeded = true;
                    return true;

                default:
                    errorMessage = "当前 Setting Helper 不支持编辑器态完整快照导入。";
                    return false;
            }
        }

        private bool TryLoadAllSettingsInEditor(SettingComponent setting, out SortedDictionary<string, string> settings, out string errorMessage)
        {
            settings = new SortedDictionary<string, string>(StringComparer.Ordinal);
            errorMessage = null;

            switch (GetEditorSettingStorageMode(setting))
            {
                case EditorSettingStorageMode.DefaultFile:
                    string filePath = GetDefaultSettingFilePath();
                    if (!File.Exists(filePath))
                    {
                        return true;
                    }

                    try
                    {
                        DefaultSetting defaultSetting = new DefaultSetting();
                        DefaultSettingSerializer serializer = CreateDefaultSettingSerializer(defaultSetting);
                        using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            serializer.Deserialize(fileStream);
                        }

                        string[] settingNames = defaultSetting.GetAllSettingNames();
                        for (int i = 0; i < settingNames.Length; i++)
                        {
                            string settingName = settingNames[i];
                            if (string.IsNullOrEmpty(settingName))
                            {
                                continue;
                            }

                            settings[settingName] = defaultSetting.GetString(settingName, string.Empty) ?? string.Empty;
                        }

                        return true;
                    }
                    catch (Exception exception)
                    {
                        errorMessage = exception.Message;
                        return false;
                    }

                case EditorSettingStorageMode.PlayerPrefs:
                    errorMessage = "当前 Setting Helper 使用 PlayerPrefs，编辑器态无法枚举全部键，因此不支持导出完整快照。";
                    return false;

                default:
                    errorMessage = "当前 Setting Helper 不支持编辑器态直接读取全部持久数据。";
                    return false;
            }
        }

        private bool TrySaveAllSettingsToDefaultFile(IDictionary<string, string> settings, out string errorMessage)
        {
            errorMessage = null;
            try
            {
                string filePath = GetDefaultSettingFilePath();
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                DefaultSetting defaultSetting = new DefaultSetting();
                DefaultSettingSerializer serializer = CreateDefaultSettingSerializer(defaultSetting);
                if (settings != null)
                {
                    foreach (KeyValuePair<string, string> pair in settings)
                    {
                        if (string.IsNullOrEmpty(pair.Key))
                        {
                            continue;
                        }

                        defaultSetting.SetString(pair.Key, pair.Value ?? string.Empty);
                    }
                }

                using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                {
                    serializer.Serialize(fileStream, defaultSetting);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private EditorSettingStorageMode GetEditorSettingStorageMode(SettingComponent setting)
        {
            if (setting == null)
            {
                return EditorSettingStorageMode.Unsupported;
            }

            string helperTypeName = GetSettingHelperTypeName(setting);
            if (string.Equals(helperTypeName, DefaultSettingHelperTypeName, StringComparison.Ordinal))
            {
                return EditorSettingStorageMode.DefaultFile;
            }

            if (string.Equals(helperTypeName, PlayerPrefsSettingHelperTypeName, StringComparison.Ordinal))
            {
                return EditorSettingStorageMode.PlayerPrefs;
            }

            return EditorSettingStorageMode.Unsupported;
        }

        private string GetSettingHelperTypeName(SettingComponent setting)
        {
            FieldInfo customHelperField = typeof(SettingComponent).GetField("m_CustomSettingHelper", EditorPrivateInstanceFlags);
            if (customHelperField != null)
            {
                SettingHelperBase customHelper = customHelperField.GetValue(setting) as SettingHelperBase;
                if (customHelper != null)
                {
                    return customHelper.GetType().FullName;
                }
            }

            FieldInfo helperTypeNameField = typeof(SettingComponent).GetField("m_SettingHelperTypeName", EditorPrivateInstanceFlags);
            if (helperTypeNameField != null)
            {
                return helperTypeNameField.GetValue(setting) as string;
            }

            return null;
        }

        private string GetDefaultSettingFilePath()
        {
            return Utility.Path.GetRegularPath(Path.Combine(Application.persistentDataPath, DefaultSettingFileName));
        }

        private DefaultSettingSerializer CreateDefaultSettingSerializer(DefaultSetting defaultSetting)
        {
            DefaultSettingSerializer serializer = new DefaultSettingSerializer();
            serializer.RegisterSerializeCallback(0, (stream, settingData) =>
            {
                defaultSetting.Serialize(stream);
                return true;
            });
            serializer.RegisterDeserializeCallback(0, stream =>
            {
                defaultSetting.Deserialize(stream);
                return defaultSetting;
            });

            return serializer;
        }

        private string GetPersistentDataJsonFullPath()
        {
            return Utility.Path.GetRegularPath(Path.GetFullPath(GetPersistentDataJsonAssetPath()));
        }

        private string GetPersistentDataJsonDirectoryFullPath()
        {
            return Utility.Path.GetRegularPath(Path.GetFullPath(PersistentDataJsonFolderAssetPath));
        }

        private string GetUniquePersistentDataJsonFullPath()
        {
            string directoryPath = GetPersistentDataJsonDirectoryFullPath();
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(PersistentDataJsonFileName);
            string extension = Path.GetExtension(PersistentDataJsonFileName);
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            string candidatePath = Utility.Path.GetRegularPath(Path.Combine(directoryPath, $"{fileNameWithoutExtension}_{timestamp}{extension}"));
            int suffixIndex = 1;

            while (File.Exists(candidatePath))
            {
                candidatePath = Utility.Path.GetRegularPath(Path.Combine(directoryPath, $"{fileNameWithoutExtension}_{timestamp}_{suffixIndex}{extension}"));
                suffixIndex++;
            }

            return candidatePath;
        }

        private bool EnsurePersistentDataJsonDirectory(out string errorMessage)
        {
            errorMessage = null;
            try
            {
                string directoryPath = Path.GetDirectoryName(GetPersistentDataJsonFullPath());
                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                return true;
            }
            catch (Exception exception)
            {
                errorMessage = exception.Message;
                return false;
            }
        }

        private enum EditorSettingStorageMode
        {
            Unsupported = 0,
            DefaultFile,
            PlayerPrefs
        }

        #endregion
#endif

        #endregion
    }
}
