#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MainGame.Editor
{
    /// <summary>
    /// 捕获物体的本地空间位置、旋转与缩放，退出运行后写回预制体或场景物体。
    /// 编辑模式与 Play 模式均可捕获。
    /// </summary>
    public sealed class LocalTransformSnapshotWindow : EditorWindow
    {
        const string WindowTitle = "本地变换快照";
        const string PrefsPrefix = "MainGame.LocalTransformSnapshot.";

        [Serializable]
        sealed class SnapshotEntry
        {
            public string label = string.Empty;
            public string capturedSourcePath = string.Empty;
            public Vector3 localPosition;
            public Quaternion localRotation = Quaternion.identity;
            public Vector3 localScale = Vector3.one;
            public GameObject applyTarget;
        }

        [SerializeField]
        List<SnapshotEntry> m_Snapshots = new List<SnapshotEntry>();

        [SerializeField]
        Vector2 m_ScrollPosition;

        [SerializeField]
        bool m_ShowEulerRotation = true;

        [SerializeField]
        bool m_ApplyScale = true;

        [MenuItem("Tools/本地变换快照", priority = 130)]
        static void Open()
        {
            LocalTransformSnapshotWindow window = GetWindow<LocalTransformSnapshotWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(480f, 360f);
            window.Show();
        }

        [MenuItem("GameObject/捕获本地变换到快照窗口", false, 0)]
        static void CaptureFromGameObjectMenu()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                return;
            }

            LocalTransformSnapshotWindow window = GetWindow<LocalTransformSnapshotWindow>(false, WindowTitle, true);
            window.CaptureFromSelection(selected);
            window.Show();
        }

        [MenuItem("GameObject/捕获本地变换到快照窗口", true)]
        static bool ValidateCaptureFromGameObjectMenu()
        {
            return Selection.activeGameObject != null;
        }

        void OnEnable()
        {
            m_ShowEulerRotation = EditorPrefs.GetBool(PrefsPrefix + nameof(m_ShowEulerRotation), true);
            m_ApplyScale = EditorPrefs.GetBool(PrefsPrefix + nameof(m_ApplyScale), true);
        }

        void OnDisable()
        {
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_ShowEulerRotation), m_ShowEulerRotation);
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_ApplyScale), m_ApplyScale);
        }

        void OnSelectionChange()
        {
            Repaint();
        }

        void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("本地变换快照", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "在 Hierarchy 选中物体，点击「捕获当前选择」记录其 localPosition / localRotation / localScale（编辑模式与 Play 模式均可）。\n" +
                "可连续捕获多条。为每条指定「应用目标」（预制体子节点或场景物体），再点击「应用」（需在非 Play 模式下）。\n" +
                "「应用尺寸」开关关闭时，仅写入位置与旋转，保留目标原有 localScale。",
                MessageType.Info);

            DrawCaptureSection();
            EditorGUILayout.Space(8f);
            DrawSnapshotList();
            EditorGUILayout.Space(4f);
            DrawBulkActions();

            EditorGUILayout.EndScrollView();
        }

        void DrawCaptureSection()
        {
            EditorGUILayout.LabelField("捕获", EditorStyles.boldLabel);

            GameObject selected = Selection.activeGameObject;
            using (new EditorGUI.DisabledScope(selected == null))
            {
                EditorGUILayout.ObjectField("当前选择", selected, typeof(GameObject), true);
            }

            bool canCapture = selected != null;
            using (new EditorGUI.DisabledScope(!canCapture))
            {
                if (GUILayout.Button("捕获当前选择", GUILayout.Height(28f)))
                {
                    CaptureFromSelection(selected);
                }
            }

            if (selected == null)
            {
                EditorGUILayout.HelpBox("请先在 Hierarchy 中选中要捕获的物体。", MessageType.Warning);
            }

            m_ShowEulerRotation = EditorGUILayout.Toggle("列表中显示欧拉角", m_ShowEulerRotation);
            m_ApplyScale = EditorGUILayout.Toggle(
                new GUIContent("应用尺寸", "开启时应用快照的 localScale；关闭时仅应用位置与旋转"),
                m_ApplyScale);
        }

        void DrawSnapshotList()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"已保存 ({m_Snapshots.Count})", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            using (new EditorGUI.DisabledScope(m_Snapshots.Count == 0))
            {
                if (GUILayout.Button("清空全部", GUILayout.Width(72f)))
                {
                    if (EditorUtility.DisplayDialog("清空快照", "确定删除全部已保存的本地变换快照？", "清空", "取消"))
                    {
                        Undo.RecordObject(this, "Clear Local Transform Snapshots");
                        m_Snapshots.Clear();
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            if (m_Snapshots.Count == 0)
            {
                EditorGUILayout.HelpBox("尚无快照。选中物体并捕获后，数据会保留在窗口中。", MessageType.None);
                return;
            }

            for (int i = 0; i < m_Snapshots.Count; i++)
            {
                DrawSnapshotEntry(i);
                EditorGUILayout.Space(4f);
            }
        }

        void DrawSnapshotEntry(int index)
        {
            SnapshotEntry entry = m_Snapshots[index];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            entry.label = EditorGUILayout.TextField("名称", entry.label);

            if (GUILayout.Button("↑", GUILayout.Width(24f)) && index > 0)
            {
                SwapSnapshots(index, index - 1);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("↓", GUILayout.Width(24f)) && index < m_Snapshots.Count - 1)
            {
                SwapSnapshots(index, index + 1);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("×", GUILayout.Width(24f)))
            {
                Undo.RecordObject(this, "Remove Local Transform Snapshot");
                m_Snapshots.RemoveAt(index);
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();

            if (!string.IsNullOrEmpty(entry.capturedSourcePath))
            {
                EditorGUILayout.LabelField("捕获路径", entry.capturedSourcePath, EditorStyles.miniLabel);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Vector3Field("Local Position", entry.localPosition);
                if (m_ShowEulerRotation)
                {
                    EditorGUILayout.Vector3Field("Local Rotation (Euler)", entry.localRotation.eulerAngles);
                }
                else
                {
                    EditorGUILayout.Vector4Field("Local Rotation (Quat)", new Vector4(
                        entry.localRotation.x,
                        entry.localRotation.y,
                        entry.localRotation.z,
                        entry.localRotation.w));
                }

                EditorGUILayout.Vector3Field("Local Scale", entry.localScale);
            }

            EditorGUI.BeginChangeCheck();
            entry.applyTarget = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("应用目标", "退出 Play 后指定要写入的预制体子物体或场景物体"),
                entry.applyTarget,
                typeof(GameObject),
                true);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this, "Set Snapshot Apply Target");
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("应用目标 ← 当前选择", GUILayout.Height(22f)))
            {
                GameObject selected = Selection.activeGameObject;
                if (selected != null)
                {
                    Undo.RecordObject(this, "Set Snapshot Apply Target From Selection");
                    entry.applyTarget = selected;
                }
            }

            using (new EditorGUI.DisabledScope(entry.applyTarget == null || Application.isPlaying))
            {
                if (GUILayout.Button("应用到目标", GUILayout.Height(22f)))
                {
                    ApplySnapshot(entry);
                }
            }
            EditorGUILayout.EndHorizontal();

            if (Application.isPlaying && entry.applyTarget != null)
            {
                EditorGUILayout.HelpBox("请在退出 Play 模式后再应用到预制体。", MessageType.None);
            }

            EditorGUILayout.EndVertical();
        }

        void DrawBulkActions()
        {
            using (new EditorGUI.DisabledScope(Application.isPlaying || m_Snapshots.Count == 0))
            {
                if (GUILayout.Button("应用全部（已指定目标的条目）", GUILayout.Height(28f)))
                {
                    ApplyAllWithTargets();
                }
            }
        }

        void CaptureFromSelection(GameObject source)
        {
            Transform transform = source.transform;
            SnapshotEntry entry = new SnapshotEntry
            {
                label = source.name,
                capturedSourcePath = GetHierarchyPath(source.transform),
                localPosition = transform.localPosition,
                localRotation = transform.localRotation,
                localScale = transform.localScale
            };

            Undo.RecordObject(this, "Capture Local Transform Snapshot");
            m_Snapshots.Add(entry);
            Debug.Log(
                $"[本地变换快照] 已捕获「{entry.label}」 pos={entry.localPosition} rot={entry.localRotation.eulerAngles} scale={entry.localScale}");
        }

        void ApplySnapshot(SnapshotEntry entry)
        {
            if (entry.applyTarget == null)
            {
                EditorUtility.DisplayDialog("应用失败", "请先指定应用目标。", "确定");
                return;
            }

            Transform targetTransform = entry.applyTarget.transform;
            if (!TryBeginApply(targetTransform, out string error))
            {
                EditorUtility.DisplayDialog("应用失败", error, "确定");
                return;
            }

            WriteTransform(targetTransform, entry);
            FinalizeApply(targetTransform.gameObject, entry.label);
        }

        void ApplyAllWithTargets()
        {
            int applied = 0;
            int skipped = 0;

            for (int i = 0; i < m_Snapshots.Count; i++)
            {
                SnapshotEntry entry = m_Snapshots[i];
                if (entry.applyTarget == null)
                {
                    skipped++;
                    continue;
                }

                Transform targetTransform = entry.applyTarget.transform;
                if (!TryBeginApply(targetTransform, out string error))
                {
                    Debug.LogWarning($"[本地变换快照] 跳过「{entry.label}」：{error}");
                    skipped++;
                    continue;
                }

                WriteTransform(targetTransform, entry);
                FinalizeApply(targetTransform.gameObject, entry.label, log: false);
                applied++;
            }

            string summary = $"已应用 {applied} 条";
            if (skipped > 0)
            {
                summary += $"，跳过 {skipped} 条";
            }

            if (!m_ApplyScale)
            {
                summary += "（未写入尺寸）";
            }

            Debug.Log($"[本地变换快照] {summary}");
            EditorUtility.DisplayDialog("批量应用完成", summary, "确定");
        }

        void WriteTransform(Transform targetTransform, SnapshotEntry entry)
        {
            Undo.RecordObject(targetTransform, "Apply Local Transform Snapshot");
            targetTransform.localPosition = entry.localPosition;
            targetTransform.localRotation = entry.localRotation;
            if (m_ApplyScale)
            {
                targetTransform.localScale = entry.localScale;
            }
        }

        static bool TryBeginApply(Transform targetTransform, out string error)
        {
            error = null;
            if (targetTransform == null)
            {
                error = "目标 Transform 无效。";
                return false;
            }

            GameObject targetObject = targetTransform.gameObject;
            if (EditorUtility.IsPersistent(targetObject) && !PrefabUtility.IsPartOfPrefabAsset(targetObject))
            {
                error = "目标不是可编辑的场景物体或预制体资源。";
                return false;
            }

            return true;
        }

        static void FinalizeApply(GameObject targetObject, string label, bool log = true)
        {
            Transform targetTransform = targetObject.transform;
            EditorUtility.SetDirty(targetTransform);

            if (PrefabUtility.IsPartOfPrefabInstance(targetObject))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(targetTransform);
            }
            else if (PrefabUtility.IsPartOfPrefabAsset(targetObject))
            {
                GameObject assetRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(targetObject) ?? targetObject;
                while (assetRoot.transform.parent != null)
                {
                    assetRoot = assetRoot.transform.parent.gameObject;
                }

                EditorUtility.SetDirty(assetRoot);
            }

            UnityEditor.SceneManagement.PrefabStage prefabStage =
                UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
            }

            if (log)
            {
                Debug.Log($"[本地变换快照] 已应用到「{label}」→ {GetHierarchyPath(targetTransform)}");
            }
        }

        void SwapSnapshots(int a, int b)
        {
            Undo.RecordObject(this, "Reorder Local Transform Snapshots");
            SnapshotEntry temp = m_Snapshots[a];
            m_Snapshots[a] = m_Snapshots[b];
            m_Snapshots[b] = temp;
        }

        static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return string.Empty;
            }

            string path = transform.name;
            Transform parent = transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }
    }
}
#endif
