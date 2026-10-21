#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace MainGame.Editor
{
    public sealed class FbxAnimationBatchProcessorWindow : EditorWindow
    {
        const string WindowTitle = "FBX动画批处理";
        const string DefaultOutputFolder = "Assets/MainGame/Animation/ExtractedAnimations";
        static readonly Regex FloatRegex = new Regex(@"(?<![\w.])-?\d+\.\d+(?:[eE][+-]?\d+)?(?![\w.])", RegexOptions.Compiled);
        static readonly Regex HighQualityCurveRegex = new Regex(@"(^\s*m_UseHighQualityCurve:\s*)\d+", RegexOptions.Multiline | RegexOptions.Compiled);

        readonly List<UnityEngine.Object> m_FbxObjects = new List<UnityEngine.Object>();
        readonly List<UnityEngine.Object> m_AnimObjects = new List<UnityEngine.Object>();
        Vector2 m_WindowScrollPosition;
        Vector2 m_FbxScrollPosition;
        Vector2 m_AnimScrollPosition;

        DefaultAsset m_OutputFolderAsset;
        bool m_SetImporterCompressionOptimal = true;
        bool m_DisableHighQualityCurve = true;
        bool m_TrimFloatDecimals = true;
        int m_KeepDecimalDigits = 4;
        bool m_CreateSubFolderByFbx = true;
        bool m_AnimSaveAsNewFile;
        bool m_DeleteFbxAfterExport;

        [MenuItem("Tools/FBX动画批处理", priority = 120)]
        static void Open()
        {
            var window = GetWindow<FbxAnimationBatchProcessorWindow>(WindowTitle);
            window.minSize = new Vector2(620f, 460f);
            window.Show();
        }

        void OnEnable()
        {
            if (m_OutputFolderAsset != null)
                return;

            if (!AssetDatabase.IsValidFolder(DefaultOutputFolder))
                EnsureFolderExists(DefaultOutputFolder);

            m_OutputFolderAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultOutputFolder);
        }

        void OnGUI()
        {
            m_WindowScrollPosition = EditorGUILayout.BeginScrollView(m_WindowScrollPosition);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("FBX 动画提取与压缩", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("拖入一个或多个 FBX。处理时会先将 FBX 的动画压缩设置为 Optimal，然后导出独立 .anim 并按选项优化文本。", MessageType.Info);

            DrawFbxInputArea();
            EditorGUILayout.Space(6f);
            DrawAnimInputArea();
            EditorGUILayout.Space(6f);
            DrawOptions();
            EditorGUILayout.Space(8f);
            DrawProcessButtons();
            EditorGUILayout.EndScrollView();
        }

        void DrawFbxInputArea()
        {
            EditorGUILayout.LabelField("FBX 列表", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从当前选择添加", GUILayout.Width(130f)))
                    AddSelectedFbx();

                if (GUILayout.Button("清空列表", GUILayout.Width(90f)))
                    m_FbxObjects.Clear();
            }

            EditorGUILayout.Space(4f);
            var dropRect = GUILayoutUtility.GetRect(0f, 54f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "将 FBX 从 Project 面板拖拽到这里");
            HandleFbxDrag(dropRect);

            EditorGUILayout.Space(4f);
            m_FbxScrollPosition = EditorGUILayout.BeginScrollView(m_FbxScrollPosition, GUILayout.Height(120f));
            int removeIndex = -1;
            for (int i = 0; i < m_FbxObjects.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_FbxObjects[i] = EditorGUILayout.ObjectField(m_FbxObjects[i], typeof(UnityEngine.Object), false);
                    if (GUILayout.Button("移除", GUILayout.Width(56f)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0)
                m_FbxObjects.RemoveAt(removeIndex);

            EditorGUILayout.EndScrollView();
        }

        void DrawAnimInputArea()
        {
            EditorGUILayout.LabelField("ANIM 列表（直接原地处理）", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从当前选择添加", GUILayout.Width(130f)))
                    AddSelectedAnim();

                if (GUILayout.Button("清空列表", GUILayout.Width(90f)))
                    m_AnimObjects.Clear();
            }

            EditorGUILayout.Space(4f);
            var dropRect = GUILayoutUtility.GetRect(0f, 54f, GUILayout.ExpandWidth(true));
            GUI.Box(dropRect, "将 ANIM 从 Project 面板拖拽到这里");
            HandleAnimDrag(dropRect);

            EditorGUILayout.Space(4f);
            m_AnimScrollPosition = EditorGUILayout.BeginScrollView(m_AnimScrollPosition, GUILayout.Height(120f));
            int removeIndex = -1;
            for (int i = 0; i < m_AnimObjects.Count; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    m_AnimObjects[i] = EditorGUILayout.ObjectField(m_AnimObjects[i], typeof(UnityEngine.Object), false);
                    if (GUILayout.Button("移除", GUILayout.Width(56f)))
                        removeIndex = i;
                }
            }

            if (removeIndex >= 0)
                m_AnimObjects.RemoveAt(removeIndex);

            EditorGUILayout.EndScrollView();
        }

        void DrawOptions()
        {
            EditorGUILayout.LabelField("处理选项", EditorStyles.boldLabel);
            m_OutputFolderAsset = (DefaultAsset)EditorGUILayout.ObjectField("输出目录", m_OutputFolderAsset, typeof(DefaultAsset), false);
            m_CreateSubFolderByFbx = EditorGUILayout.ToggleLeft("每个 FBX 使用独立子目录", m_CreateSubFolderByFbx);
            m_AnimSaveAsNewFile = EditorGUILayout.ToggleLeft("ANIM 另存为新文件到输出目录（否则原地覆盖）", m_AnimSaveAsNewFile);
            m_SetImporterCompressionOptimal = EditorGUILayout.ToggleLeft("将 FBX 动画压缩设置为 Optimal", m_SetImporterCompressionOptimal);
            m_DisableHighQualityCurve = EditorGUILayout.ToggleLeft("导出后关闭 m_UseHighQualityCurve", m_DisableHighQualityCurve);
            m_TrimFloatDecimals = EditorGUILayout.ToggleLeft("裁剪动画文本中的小数位", m_TrimFloatDecimals);
            using (new EditorGUI.DisabledScope(!m_TrimFloatDecimals))
            {
                m_KeepDecimalDigits = EditorGUILayout.IntSlider("保留小数位", m_KeepDecimalDigits, 0, 8);
            }

            m_DeleteFbxAfterExport = EditorGUILayout.ToggleLeft("提取后删除源 FBX（谨慎）", m_DeleteFbxAfterExport);
        }

        void DrawProcessButtons()
        {
            using (new EditorGUI.DisabledScope(!CanProcessFbx()))
            {
                if (GUILayout.Button("开始批处理", GUILayout.Height(34f)))
                    ProcessAll();
            }

            using (new EditorGUI.DisabledScope(!CanProcessAnimOnly()))
            {
                string animButtonText = m_AnimSaveAsNewFile
                    ? "处理 ANIM（另存到输出目录）"
                    : "处理 ANIM（原地覆盖）";
                if (GUILayout.Button(animButtonText, GUILayout.Height(30f)))
                    ProcessAnimOnly();
            }

            if (!CanProcessFbx() && !CanProcessAnimOnly())
                EditorGUILayout.HelpBox("请添加 FBX（用于提取）或 ANIM（用于直接处理）。", MessageType.Warning);
        }

        bool CanProcessFbx()
        {
            return m_FbxObjects.Any(o => IsValidFbxPath(AssetDatabase.GetAssetPath(o))) && GetOutputFolderPath() != null;
        }

        bool CanProcessAnimOnly()
        {
            if (!m_AnimObjects.Any(o => IsValidAnimPath(AssetDatabase.GetAssetPath(o))))
                return false;

            if (!m_AnimSaveAsNewFile)
                return true;

            return GetOutputFolderPath() != null;
        }

        void AddSelectedFbx()
        {
            foreach (var obj in Selection.objects)
                TryAddFbxObject(obj);
        }

        void AddSelectedAnim()
        {
            foreach (var obj in Selection.objects)
                TryAddAnimObject(obj);
        }

        void HandleFbxDrag(Rect dropRect)
        {
            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition))
                return;

            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            bool hasFbx = DragAndDrop.objectReferences.Any(o => IsValidFbxPath(AssetDatabase.GetAssetPath(o)));
            DragAndDrop.visualMode = hasFbx ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    TryAddFbxObject(obj);
            }

            evt.Use();
        }

        void HandleAnimDrag(Rect dropRect)
        {
            Event evt = Event.current;
            if (!dropRect.Contains(evt.mousePosition))
                return;

            if (evt.type != EventType.DragUpdated && evt.type != EventType.DragPerform)
                return;

            bool hasAnim = DragAndDrop.objectReferences.Any(o => IsValidAnimPath(AssetDatabase.GetAssetPath(o)));
            DragAndDrop.visualMode = hasAnim ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var obj in DragAndDrop.objectReferences)
                    TryAddAnimObject(obj);
            }

            evt.Use();
        }

        void TryAddFbxObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            string path = AssetDatabase.GetAssetPath(obj);
            if (!IsValidFbxPath(path))
                return;

            if (!m_FbxObjects.Contains(obj))
                m_FbxObjects.Add(obj);
        }

        void TryAddAnimObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            string path = AssetDatabase.GetAssetPath(obj);
            if (!IsValidAnimPath(path))
                return;

            if (!m_AnimObjects.Contains(obj))
                m_AnimObjects.Add(obj);
        }

        void ProcessAll()
        {
            string outputRoot = GetOutputFolderPath();
            if (string.IsNullOrEmpty(outputRoot))
            {
                Debug.LogError("[FbxAnimationBatchProcessor] 输出目录无效。");
                return;
            }

            var fbxPaths = m_FbxObjects
                .Select(AssetDatabase.GetAssetPath)
                .Where(IsValidFbxPath)
                .Distinct()
                .ToList();

            if (fbxPaths.Count == 0)
            {
                Debug.LogWarning("[FbxAnimationBatchProcessor] 没有可处理的 FBX。");
                return;
            }

            int exportedCount = 0;
            int processedCount = 0;
            int deletedCount = 0;
            try
            {
                for (int i = 0; i < fbxPaths.Count; i++)
                {
                    string fbxPath = fbxPaths[i];
                    EditorUtility.DisplayProgressBar(WindowTitle, $"处理 {i + 1}/{fbxPaths.Count}: {fbxPath}", (i + 1f) / fbxPaths.Count);
                    ProcessSingleFbx(fbxPath, outputRoot, ref exportedCount, ref processedCount, ref deletedCount);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[FbxAnimationBatchProcessor] 完成。FBX: {fbxPaths.Count}，导出动画: {exportedCount}，文本处理: {processedCount}，删除FBX: {deletedCount}");
        }

        void ProcessAnimOnly()
        {
            var animPaths = m_AnimObjects
                .Select(AssetDatabase.GetAssetPath)
                .Where(IsValidAnimPath)
                .Distinct()
                .ToList();

            if (animPaths.Count == 0)
            {
                Debug.LogWarning("[FbxAnimationBatchProcessor] 没有可处理的 ANIM。");
                return;
            }

            string outputRoot = null;
            if (m_AnimSaveAsNewFile)
            {
                outputRoot = GetOutputFolderPath();
                if (string.IsNullOrEmpty(outputRoot))
                {
                    Debug.LogError("[FbxAnimationBatchProcessor] ANIM 另存模式下输出目录无效。");
                    return;
                }
            }

            int processedCount = 0;
            int createdCount = 0;
            try
            {
                for (int i = 0; i < animPaths.Count; i++)
                {
                    string sourceAnimPath = animPaths[i];
                    string targetAnimPath = sourceAnimPath;
                    if (m_AnimSaveAsNewFile)
                    {
                        string animName = SanitizeFileName(Path.GetFileNameWithoutExtension(sourceAnimPath));
                        targetAnimPath = AssetDatabase.GenerateUniqueAssetPath($"{outputRoot}/{animName}.anim");
                        if (!AssetDatabase.CopyAsset(sourceAnimPath, targetAnimPath))
                        {
                            Debug.LogWarning($"[FbxAnimationBatchProcessor] 复制 ANIM 失败，已跳过: {sourceAnimPath}");
                            continue;
                        }

                        createdCount++;
                    }

                    EditorUtility.DisplayProgressBar(WindowTitle, $"处理 ANIM {i + 1}/{animPaths.Count}: {targetAnimPath}", (i + 1f) / animPaths.Count);
                    if (!PostProcessAnimFile(targetAnimPath))
                        continue;

                    processedCount++;
                    AssetDatabase.ImportAsset(targetAnimPath, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[FbxAnimationBatchProcessor] ANIM处理完成。源文件总数: {animPaths.Count}，新建文件: {createdCount}，发生变更: {processedCount}");
        }

        void ProcessSingleFbx(string fbxPath, string outputRoot, ref int exportedCount, ref int processedCount, ref int deletedCount)
        {
            if (m_SetImporterCompressionOptimal)
                EnsureFbxOptimalCompression(fbxPath);

            string targetFolder = outputRoot;
            if (m_CreateSubFolderByFbx)
            {
                string subFolderName = SanitizeFileName(Path.GetFileNameWithoutExtension(fbxPath));
                targetFolder = $"{outputRoot}/{subFolderName}";
                EnsureFolderExists(targetFolder);
            }

            var clips = AssetDatabase
                .LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .Where(IsExportableClip)
                .ToList();

            if (clips.Count == 0)
            {
                Debug.LogWarning($"[FbxAnimationBatchProcessor] FBX 未找到可导出动画: {fbxPath}");
                return;
            }

            foreach (var clip in clips)
            {
                string clipName = SanitizeFileName(clip.name);
                string animPath = AssetDatabase.GenerateUniqueAssetPath($"{targetFolder}/{clipName}.anim");

                var copiedClip = Instantiate(clip);
                copiedClip.name = clip.name;
                AssetDatabase.CreateAsset(copiedClip, animPath);
                exportedCount++;

                if (PostProcessAnimFile(animPath))
                {
                    processedCount++;
                    AssetDatabase.ImportAsset(animPath, ImportAssetOptions.ForceUpdate);
                }
            }

            if (m_DeleteFbxAfterExport && AssetDatabase.DeleteAsset(fbxPath))
                deletedCount++;
        }

        static bool IsExportableClip(AnimationClip clip)
        {
            if (clip == null)
                return false;

            if (clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        void EnsureFbxOptimalCompression(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FbxAnimationBatchProcessor] 非 ModelImporter，无法设置压缩: {fbxPath}");
                return;
            }

            if (importer.animationCompression == ModelImporterAnimationCompression.Optimal)
                return;

            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.SaveAndReimport();
        }

        bool PostProcessAnimFile(string animAssetPath)
        {
            string fullPath = GetAbsolutePath(animAssetPath);
            if (!File.Exists(fullPath))
                return false;

            string content = File.ReadAllText(fullPath);
            string newContent = content;

            if (m_DisableHighQualityCurve)
            {
                newContent = HighQualityCurveRegex.Replace(newContent, match => $"{match.Groups[1].Value}0");
            }

            if (m_TrimFloatDecimals)
            {
                newContent = FloatRegex.Replace(newContent, match => RoundFloatString(match.Value, m_KeepDecimalDigits));
            }

            if (string.Equals(content, newContent, StringComparison.Ordinal))
                return false;

            File.WriteAllText(fullPath, newContent);
            return true;
        }

        static string RoundFloatString(string value, int digits)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                return value;

            double rounded = Math.Round(number, digits, MidpointRounding.AwayFromZero);
            string formatted = rounded.ToString($"F{Mathf.Clamp(digits, 0, 8)}", CultureInfo.InvariantCulture);
            formatted = formatted.TrimEnd('0').TrimEnd('.');
            if (!formatted.Contains("."))
                formatted += ".0";
            return formatted;
        }

        string GetOutputFolderPath()
        {
            if (m_OutputFolderAsset == null)
                return null;

            string path = AssetDatabase.GetAssetPath(m_OutputFolderAsset);
            if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                return null;

            return path;
        }

        static bool IsValidFbxPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase);
        }

        static bool IsValidAnimPath(string path)
        {
            return !string.IsNullOrEmpty(path) && path.EndsWith(".anim", StringComparison.OrdinalIgnoreCase);
        }

        static string GetAbsolutePath(string assetPath)
        {
            string projectRoot = Directory.GetCurrentDirectory();
            return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
        }

        static string SanitizeFileName(string fileName)
        {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(fileName.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        }

        static void EnsureFolderExists(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
                return;

            string normalized = assetFolderPath.Replace("\\", "/");
            if (!normalized.StartsWith("Assets", StringComparison.Ordinal))
                throw new InvalidOperationException($"输出目录必须位于 Assets 下: {assetFolderPath}");

            string[] parts = normalized.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
