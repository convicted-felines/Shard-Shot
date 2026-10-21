//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using System.Linq;


namespace MainGame.Editor.DataTableTools
{
    public sealed class DataTableGeneratorMenu
    {
        private const string ExcelFolderPath = "Assets/MainGame/DataTables/Excel";
        private const string TextFolderPath = "Assets/MainGame/DataTables/Text";

        [MenuItem("DataTables/C.持久数据编辑工具")]
        private static void OpenPersistentDataTools()
        {
            PersistentDataToolsWindow.Open();
        }

        [MenuItem("DataTables/B.生成数据表代码和byte文件")]
        public static void GenerateDataTables()
        {
            foreach (string dataTableName in ProcedurePreload.DataTableNames)
            {
                DataTableProcessor dataTableProcessor = DataTableGenerator.CreateDataTableProcessor(dataTableName);
                if (!DataTableGenerator.CheckRawData(dataTableProcessor, dataTableName))
                {
                    Debug.LogError(Utility.Text.Format("Check raw data failure. DataTableName='{0}'", dataTableName));
                    break;
                }

                DataTableGenerator.GenerateDataFile(dataTableProcessor, dataTableName);
                DataTableGenerator.GenerateCodeFile(dataTableProcessor, dataTableName);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("DataTables/A.Excel导出数据表文本")]
        private static void ExportExcelToText()
        {
            ExportExcelToTextInternal();
        }

        /// <summary>
        /// 一键：先 Excel→txt，再 txt→byte/DR 代码。
        /// </summary>
        [MenuItem("DataTables/A+B.一键导出并生成")]
        private static void ExportExcelAndGenerateDataTables()
        {
            if (!ExportExcelToTextInternal())
            {
                Debug.LogWarning("一键导出并生成已中止：Excel 导出未成功，跳过生成代码和 byte 文件。");
                return;
            }

            GenerateDataTables();
            Debug.Log("一键导出并生成完成。");
        }

        /// <summary>
        /// 从 Excel 导出数据表文本到 Text 目录。
        /// </summary>
        /// <returns>导出流程可继续时返回 true；文件夹不存在或没有 Excel 时返回 false。</returns>
        private static bool ExportExcelToTextInternal()
        {
            string excelFolder = Path.GetFullPath(ExcelFolderPath);
            string textFolder = Path.GetFullPath(TextFolderPath);

            if (!Directory.Exists(excelFolder))
            {
                Debug.LogError($"Excel 文件夹不存在: {excelFolder}");
                return false;
            }

            if (!Directory.Exists(textFolder))
            {
                Directory.CreateDirectory(textFolder);
            }

            // 收集所有 Excel 文件（.xlsx 和 .xlsm），排除 Excel 打开时生成的 ~$ 锁文件
            string[] excelFiles = Directory.GetFiles(excelFolder)
                .Where(f =>
                {
                    string name = Path.GetFileName(f);
                    if (name.StartsWith("~$", StringComparison.Ordinal))
                        return false;
                    return f.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                           f.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase);
                })
                .ToArray();

            if (excelFiles.Length == 0)
            {
                Debug.LogWarning("未找到任何 Excel 文件: " + excelFolder);
                return false;
            }

            // 第一遍：收集所有工作表名称，检测重名
            var sheetNameToFiles = new Dictionary<string, List<string>>();

            foreach (string excelFile in excelFiles)
            {
                try
                {
                    List<string> sheetNames = DataTableGenerator.GetSheetNamesFromExcel(excelFile);
                    foreach (string sheetName in sheetNames)
                    {
                        if (!sheetNameToFiles.ContainsKey(sheetName))
                            sheetNameToFiles[sheetName] = new List<string>();
                        sheetNameToFiles[sheetName].Add(excelFile);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"读取工作表名称失败 {Path.GetFileName(excelFile)}: {e.Message}");
                }
            }

            // 找出重名的工作表
            var duplicateSheetNames = new HashSet<string>(
                sheetNameToFiles.Where(kv => kv.Value.Count > 1).Select(kv => kv.Key)
            );

            if (duplicateSheetNames.Count > 0)
            {
                Debug.Log($"检测到以下工作表在多个 Excel 文件中重名，将添加文件名后缀: {string.Join(", ", duplicateSheetNames)}");
            }

            // 第二遍：导出所有工作表
            int exportedCount = 0;

            foreach (string excelFile in excelFiles)
            {
                try
                {
                    exportedCount += DataTableGenerator.ExportSheetsFromExcel(excelFile, textFolder, duplicateSheetNames);
                }
                catch (Exception e)
                {
                    Debug.LogError($"导出失败 {Path.GetFileName(excelFile)}: {e.Message}\n{e.StackTrace}");
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"Excel 导出完成！共导出 {exportedCount} 个工作表到 {TextFolderPath}");
            return true;
        }
    }

    internal sealed class PersistentDataToolsWindow : EditorWindow
    {
        private BuiltinDataComponent m_BuiltinDataComponent;
        private string m_StatusMessage = "请先指定或刷新 BuiltinDataComponent。";
        private MessageType m_StatusType = MessageType.Info;
        private string m_ManualPersistentKey = string.Empty;
        private string m_ManualPersistentValue = string.Empty;
        private string m_KeyPrefixFilter = string.Empty;

        public static void Open()
        {
            PersistentDataToolsWindow window = GetWindow<PersistentDataToolsWindow>("Persistent Data Tools");
            window.minSize = new Vector2(480f, 360f);
            window.RefreshBuiltinDataComponent();
            window.Show();
        }

        private void OnEnable()
        {
            RefreshBuiltinDataComponent();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("持久数据工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("用于手动写入键值、按键前缀批量删除、删除全部持久数据、导出当前数据为 JSON，以及从 JSON 强制覆盖当前数据。导出会自动保存到预设目录并追加时间后缀，导入时可手动选择任意 JSON 文件。", MessageType.Info);

            EditorGUI.BeginChangeCheck();
            m_BuiltinDataComponent = (BuiltinDataComponent)EditorGUILayout.ObjectField("Builtin Data", m_BuiltinDataComponent, typeof(BuiltinDataComponent), true);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateStatusByCurrentSelection();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("刷新目标"))
            {
                RefreshBuiltinDataComponent();
            }

            if (GUILayout.Button("定位到组件") && m_BuiltinDataComponent != null)
            {
                Selection.activeObject = m_BuiltinDataComponent.gameObject;
                EditorGUIUtility.PingObject(m_BuiltinDataComponent.gameObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(m_StatusMessage, m_StatusType);
            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(m_BuiltinDataComponent == null))
            {
                EditorGUILayout.LabelField("手动写入一条（字符串）", EditorStyles.boldLabel);
                m_ManualPersistentKey = EditorGUILayout.TextField("键", m_ManualPersistentKey);
                m_ManualPersistentValue = EditorGUILayout.TextField("值", m_ManualPersistentValue);
                if (GUILayout.Button("写入并保存到持久数据", GUILayout.Height(28f)))
                {
                    ScheduleWriteManualPersistentPair();
                }

                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("按键前缀批量删除", EditorStyles.boldLabel);
                m_KeyPrefixFilter = EditorGUILayout.TextField("键前缀", m_KeyPrefixFilter);
                EditorGUILayout.HelpBox("匹配并删除所有键名以该前缀开头的持久数据，例如「Setting.MiniGameSpot」会删除 Setting.MiniGameSpot.1.Consumed 等条目。", MessageType.None);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("预览匹配项", GUILayout.Height(28f)))
                {
                    SchedulePreviewPersistentDataByPrefix();
                }

                if (GUILayout.Button("删除匹配项", GUILayout.Height(28f)))
                {
                    ScheduleDeletePersistentDataByPrefix();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(10f);

                if (GUILayout.Button("删除全部持久数据", GUILayout.Height(32f)))
                {
                    ScheduleClearAllPersistentData();
                }

                if (GUILayout.Button("导出全部持久数据为 JSON", GUILayout.Height(32f)))
                {
                    ScheduleExportAllPersistentData();
                }

                if (GUILayout.Button("从 JSON 强制覆盖全部持久数据", GUILayout.Height(32f)))
                {
                    ScheduleImportAllPersistentData();
                }
            }
        }

        private void RefreshBuiltinDataComponent()
        {
            if (Selection.activeGameObject != null)
            {
                m_BuiltinDataComponent = Selection.activeGameObject.GetComponentInParent<BuiltinDataComponent>();
            }

            if (m_BuiltinDataComponent == null)
            {
                m_BuiltinDataComponent = FindObjectOfType<BuiltinDataComponent>();
            }

            UpdateStatusByCurrentSelection();
        }

        private void UpdateStatusByCurrentSelection()
        {
            if (m_BuiltinDataComponent == null)
            {
                m_StatusMessage = "场景中未找到 BuiltinDataComponent，请手动指定。";
                m_StatusType = MessageType.Warning;
                return;
            }

            m_StatusMessage = $"当前目标：{m_BuiltinDataComponent.name}";
            m_StatusType = MessageType.None;
        }

        /// <summary>
        /// 不得在 OnGUI 内同步调用 <see cref="EditorUtility.DisplayDialog"/> 或大量文件 IO，否则会与 IMGUI 重入冲突导致编辑器假死。
        /// </summary>
        private void ScheduleWriteManualPersistentPair()
        {
            if (m_BuiltinDataComponent == null)
            {
                return;
            }

            BuiltinDataComponent target = m_BuiltinDataComponent;
            string key = m_ManualPersistentKey;
            string value = m_ManualPersistentValue;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!target.EditorTrySetPersistentString(key, value, out string errorMessage))
                {
                    ShowOperationResult("写入失败", errorMessage, MessageType.Error);
                    return;
                }

                ShowOperationResult("写入成功", $"已保存：键「{key}」", MessageType.Info);
            };
        }

        private void SchedulePreviewPersistentDataByPrefix()
        {
            if (m_BuiltinDataComponent == null)
            {
                return;
            }

            BuiltinDataComponent target = m_BuiltinDataComponent;
            string keyPrefix = m_KeyPrefixFilter;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!target.EditorTryGetPersistentDataKeysByPrefix(keyPrefix, out List<string> matchedKeys, out string errorMessage))
                {
                    Debug.LogError($"[Persistent Data] 预览失败：{errorMessage}");
                    ShowOperationResult("预览失败", errorMessage, MessageType.Error);
                    return;
                }

                if (matchedKeys.Count == 0)
                {
                    Debug.Log($"[Persistent Data] 未找到以「{keyPrefix}」开头的持久数据。");
                    ShowOperationResult("预览结果", $"未找到以「{keyPrefix}」开头的持久数据。", MessageType.Info);
                    return;
                }

                StringBuilder consoleReport = new StringBuilder();
                consoleReport.AppendLine($"[Persistent Data] 键前缀「{keyPrefix}」共匹配 {matchedKeys.Count} 条：");
                for (int i = 0; i < matchedKeys.Count; i++)
                {
                    consoleReport.AppendLine($"  {matchedKeys[i]}");
                }

                Debug.Log(consoleReport.ToString());

                const int previewLimit = 20;
                string previewKeys = string.Join("\n", matchedKeys.Take(previewLimit));
                if (matchedKeys.Count > previewLimit)
                {
                    previewKeys += $"\n... 另有 {matchedKeys.Count - previewLimit} 条未显示（完整列表见 Console）";
                }

                ShowOperationResult(
                    "预览结果",
                    $"共找到 {matchedKeys.Count} 条匹配项（完整列表已输出到 Console）：\n{previewKeys}",
                    MessageType.Info);
            };
        }

        private void ScheduleDeletePersistentDataByPrefix()
        {
            if (m_BuiltinDataComponent == null)
            {
                return;
            }

            BuiltinDataComponent target = m_BuiltinDataComponent;
            string keyPrefix = m_KeyPrefixFilter;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!target.EditorTryGetPersistentDataKeysByPrefix(keyPrefix, out List<string> matchedKeys, out string errorMessage))
                {
                    ShowOperationResult("删除失败", errorMessage, MessageType.Error);
                    return;
                }

                if (matchedKeys.Count == 0)
                {
                    ShowOperationResult("删除完成", $"未找到以「{keyPrefix}」开头的持久数据。", MessageType.Info);
                    return;
                }

                const int previewLimit = 10;
                string previewKeys = string.Join("\n", matchedKeys.Take(previewLimit));
                if (matchedKeys.Count > previewLimit)
                {
                    previewKeys += $"\n... 另有 {matchedKeys.Count - previewLimit} 条";
                }

                string confirmMessage = $"将删除 {matchedKeys.Count} 条以「{keyPrefix}」开头的持久数据：\n{previewKeys}\n\n该操作会立即保存到本地。";
                if (!EditorUtility.DisplayDialog("确认按前缀删除", confirmMessage, "删除", "取消"))
                {
                    return;
                }

                if (!target.EditorTryDeletePersistentDataByKeyPrefix(keyPrefix, out int deletedCount, out errorMessage))
                {
                    ShowOperationResult("删除失败", errorMessage, MessageType.Error);
                    return;
                }

                ShowOperationResult("删除成功", $"已删除 {deletedCount} 条以「{keyPrefix}」开头的持久数据。", MessageType.Info);
            };
        }

        private void ScheduleClearAllPersistentData()
        {
            BuiltinDataComponent target = m_BuiltinDataComponent;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!EditorUtility.DisplayDialog("确认删除", "确定要删除全部持久数据吗？该操作会立即保存到本地。", "删除全部", "取消"))
                {
                    return;
                }

                if (!target.EditorTryClearAllPersistentData(out string errorMessage))
                {
                    ShowOperationResult("删除失败", errorMessage, MessageType.Error);
                    return;
                }

                ShowOperationResult("删除成功", "已删除全部持久数据。", MessageType.Info);
            };
        }

        private void ScheduleExportAllPersistentData()
        {
            BuiltinDataComponent target = m_BuiltinDataComponent;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!target.EditorTryExportAllPersistentDataJsonToNewFile(out string filePath, out int exportedCount, out string errorMessage))
                {
                    ShowOperationResult("导出失败", errorMessage, MessageType.Error);
                    return;
                }

                EditorUtility.RevealInFinder(filePath);
                ShowOperationResult("导出成功", $"已导出 {exportedCount} 条持久数据到 {filePath}。", MessageType.Info);
            };
        }

        private void ScheduleImportAllPersistentData()
        {
            BuiltinDataComponent target = m_BuiltinDataComponent;
            EditorApplication.delayCall += () =>
            {
                if (target == null)
                {
                    return;
                }

                if (!EditorUtility.DisplayDialog("确认覆盖导入", "导入会先清空当前全部持久数据，再使用 JSON 中的数据强制覆盖，是否继续？", "继续导入", "取消"))
                {
                    return;
                }

                string defaultDirectory = Path.GetFullPath(BuiltinDataComponent.GetPersistentDataJsonDirectoryAssetPath());
                string filePath = EditorUtility.OpenFilePanel("选择要导入的持久数据 JSON", defaultDirectory, "json");
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return;
                }

                if (!target.EditorTryImportAllPersistentDataJsonFromFile(filePath, out SettingSnapshotImportResult result, out string errorMessage))
                {
                    ShowOperationResult("导入失败", errorMessage, MessageType.Error);
                    return;
                }

                ShowOperationResult("导入成功", $"已从 {filePath} 导入，清空 {result.RemovedCount} 条旧数据，并导入 {result.ImportedCount} 条新数据。", MessageType.Info);
            };
        }

        private void ShowOperationResult(string dialogTitle, string message, MessageType messageType)
        {
            m_StatusMessage = message;
            m_StatusType = messageType;
            Repaint();
            string titleCopy = dialogTitle;
            string messageCopy = message;
            EditorApplication.delayCall += () =>
            {
                EditorUtility.DisplayDialog(titleCopy, messageCopy, "确定");
            };
        }
    }
}
