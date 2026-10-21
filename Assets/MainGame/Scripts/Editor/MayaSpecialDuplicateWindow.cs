#if UNITY_EDITOR
using MainGame;
using UnityEditor;
using UnityEngine;

namespace MainGame.Editor
{
    /// <summary>
    /// 类似 Maya「特殊复制」：按位移/旋转增量复制，新对象与指定父物体同级挂载；
    /// 连续复制时以「上一次复制结果」为基准累加偏移。
    /// </summary>
    public sealed class MayaSpecialDuplicateWindow : EditorWindow
    {
        const string WindowTitle = "特殊复制";
        const string PrefsPrefix = "MainGame.MayaSpecialDuplicate.";

        Transform m_Source;
        Transform m_Parent;
        Transform m_ReferenceObject;
        Transform m_LastDuplicate;

        Vector3 m_TranslateIncrement = Vector3.zero;
        Vector3 m_RotateIncrementEuler = Vector3.zero;
        Vector3 m_ScaleIncrement = Vector3.one;

        bool m_UseLocalSpace = true;
        bool m_ApplyScaleIncrement;
        int m_BatchCopyCount = 1;
        bool m_ResetBaseAfterBatch = true;

        Vector2 m_ScrollPosition;

        [MenuItem("Tools/特殊复制 (Maya)", priority = 125)]
        static void Open()
        {
            var window = GetWindow<MayaSpecialDuplicateWindow>(WindowTitle);
            window.minSize = new Vector2(420f, 380f);
            window.Show();
        }

        void OnEnable()
        {
            m_TranslateIncrement = LoadVector3(nameof(m_TranslateIncrement), Vector3.zero);
            m_RotateIncrementEuler = LoadVector3(nameof(m_RotateIncrementEuler), Vector3.zero);
            m_ScaleIncrement = LoadVector3(nameof(m_ScaleIncrement), Vector3.one);
            m_UseLocalSpace = EditorPrefs.GetBool(PrefsPrefix + nameof(m_UseLocalSpace), true);
            m_ApplyScaleIncrement = EditorPrefs.GetBool(PrefsPrefix + nameof(m_ApplyScaleIncrement), false);
            m_BatchCopyCount = EditorPrefs.GetInt(PrefsPrefix + nameof(m_BatchCopyCount), 1);
            m_ResetBaseAfterBatch = EditorPrefs.GetBool(PrefsPrefix + nameof(m_ResetBaseAfterBatch), true);

            SyncFromSelection();
        }

        void OnDisable()
        {
            SaveVector3(nameof(m_TranslateIncrement), m_TranslateIncrement);
            SaveVector3(nameof(m_RotateIncrementEuler), m_RotateIncrementEuler);
            SaveVector3(nameof(m_ScaleIncrement), m_ScaleIncrement);
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_UseLocalSpace), m_UseLocalSpace);
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_ApplyScaleIncrement), m_ApplyScaleIncrement);
            EditorPrefs.SetInt(PrefsPrefix + nameof(m_BatchCopyCount), m_BatchCopyCount);
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_ResetBaseAfterBatch), m_ResetBaseAfterBatch);
        }

        void OnSelectionChange()
        {
            if (Selection.activeTransform == null)
                return;

            SyncFromSelection();
            Repaint();
        }

        void OnGUI()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("特殊复制 (Maya Duplicate Special)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "在场景中选中源物体后复制。新物体挂在「父物体」下，与「指定物体」同级。\n" +
                "单次复制：相对「基准物体」累加位移/旋转；再次复制时基准为上一次的复制结果。\n" +
                "批量复制：从源物体起连续生成多份，每份相对源物体累加 N 倍增量。",
                MessageType.Info);

            DrawObjectFields();
            EditorGUILayout.Space(6f);
            DrawIncrementFields();
            EditorGUILayout.Space(6f);
            DrawBatchFields();
            EditorGUILayout.Space(8f);
            DrawActionButtons();
            EditorGUILayout.EndScrollView();
        }

        void DrawObjectFields()
        {
            EditorGUILayout.LabelField("对象", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("从选择读取", GUILayout.Width(100f)))
                    SyncFromSelection();
            }

            m_Source = EditorGUILayout.ObjectField("源物体", m_Source, typeof(Transform), true) as Transform;
            m_Parent = EditorGUILayout.ObjectField("父物体 (共享)", m_Parent, typeof(Transform), true) as Transform;
            m_ReferenceObject = EditorGUILayout.ObjectField("指定物体 (可选)", m_ReferenceObject, typeof(Transform), true) as Transform;

            using (new EditorGUI.DisabledScope(m_LastDuplicate == null))
            {
                EditorGUILayout.ObjectField("上次复制 (基准)", m_LastDuplicate, typeof(Transform), true);
            }

            if (m_Parent != null && m_ReferenceObject != null && m_ReferenceObject.parent != m_Parent)
            {
                EditorGUILayout.HelpBox("指定物体当前不在父物体下。可使用下方按钮将其移到同一父级。", MessageType.Warning);
                if (GUILayout.Button("将指定物体挂到父物体"))
                    ReparentUnderSharedParent(m_ReferenceObject);
            }
        }

        void DrawIncrementFields()
        {
            EditorGUILayout.LabelField("增量 (每步)", EditorStyles.boldLabel);
            m_TranslateIncrement = EditorGUILayout.Vector3Field("位移", m_TranslateIncrement);
            m_RotateIncrementEuler = EditorGUILayout.Vector3Field("旋转 (欧拉°)", m_RotateIncrementEuler);
            m_UseLocalSpace = EditorGUILayout.Toggle("相对基准局部空间", m_UseLocalSpace);

            m_ApplyScaleIncrement = EditorGUILayout.Toggle("应用缩放增量", m_ApplyScaleIncrement);
            if (m_ApplyScaleIncrement)
                m_ScaleIncrement = EditorGUILayout.Vector3Field("缩放倍率 (每步)", m_ScaleIncrement);
        }

        void DrawBatchFields()
        {
            EditorGUILayout.LabelField("批量", EditorStyles.boldLabel);
            m_BatchCopyCount = EditorGUILayout.IntSlider("复制数量", Mathf.Max(1, m_BatchCopyCount), 1, 100);
            m_ResetBaseAfterBatch = EditorGUILayout.Toggle("批量完成后清除「上次复制」", m_ResetBaseAfterBatch);
        }

        void DrawActionButtons()
        {
            using (new EditorGUI.DisabledScope(m_Source == null))
            {
                if (GUILayout.Button("特殊复制一次", GUILayout.Height(28f)))
                    DuplicateOnce();
            }

            using (new EditorGUI.DisabledScope(m_Source == null || m_BatchCopyCount < 1))
            {
                if (GUILayout.Button($"批量特殊复制 × {Mathf.Max(1, m_BatchCopyCount)}", GUILayout.Height(28f)))
                    DuplicateBatch();
            }

            EditorGUILayout.Space(4f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("基准 → 源物体"))
                    m_LastDuplicate = null;

                if (GUILayout.Button("基准 → 上次复制", GUILayout.Width(130f)))
                {
                    if (m_LastDuplicate == null)
                        EditorUtility.DisplayDialog(WindowTitle, "尚无上次复制结果。", "确定");
                }
            }

            if (m_Source != null && m_Parent == null && m_Source.parent != null && GUILayout.Button("父物体 ← 源物体的当前父级"))
                m_Parent = m_Source.parent;
        }

        void SyncFromSelection()
        {
            if (Selection.activeTransform == null)
                return;

            m_Source = Selection.activeTransform;

            if (m_Parent == null && m_Source.parent != null)
                m_Parent = m_Source.parent;
        }

        Transform ResolveParent()
        {
            if (m_Parent != null)
                return m_Parent;

            if (m_Source != null && m_Source.parent != null)
                return m_Source.parent;

            return null;
        }

        Transform GetBaseTransform()
        {
            if (m_LastDuplicate != null)
                return m_LastDuplicate;

            return m_Source;
        }

        void DuplicateOnce()
        {
            if (!ValidateSource())
                return;

            Transform parent = ResolveParent();
            if (parent == null)
            {
                if (!EditorUtility.DisplayDialog(
                        WindowTitle,
                        "未指定父物体，复制将挂在场景根节点。是否继续？",
                        "继续",
                        "取消"))
                    return;
            }

            Transform created = CreateDuplicateAtStep(GetBaseTransform(), m_Source, parent, 1);
            if (created == null)
                return;

            m_LastDuplicate = created;
            Selection.activeGameObject = created.gameObject;
            EditorGUIUtility.PingObject(created.gameObject);
        }

        void DuplicateBatch()
        {
            if (!ValidateSource())
                return;

            Transform parent = ResolveParent();
            if (parent == null)
            {
                if (!EditorUtility.DisplayDialog(
                        WindowTitle,
                        "未指定父物体，复制将挂在场景根节点。是否继续？",
                        "继续",
                        "取消"))
                    return;
            }

            int count = Mathf.Max(1, m_BatchCopyCount);
            Transform last = null;
            var createdObjects = new GameObject[count];

            for (int step = 1; step <= count; step++)
            {
                Transform created = CreateDuplicateAtStep(m_Source, m_Source, parent, step);
                if (created == null)
                    return;

                last = created;
                createdObjects[step - 1] = created.gameObject;
            }

            if (last != null && !m_ResetBaseAfterBatch)
                m_LastDuplicate = last;
            else if (m_ResetBaseAfterBatch)
                m_LastDuplicate = null;

            if (createdObjects.Length > 0)
                Selection.objects = createdObjects;
        }

        bool ValidateSource()
        {
            if (m_Source == null)
            {
                EditorUtility.DisplayDialog(WindowTitle, "请先指定或选中源物体。", "确定");
                return false;
            }

            if (m_Parent != null && m_Source == m_Parent)
            {
                EditorUtility.DisplayDialog(WindowTitle, "源物体不能与父物体相同。", "确定");
                return false;
            }

            return true;
        }

        Transform CreateDuplicateAtStep(Transform basis, Transform duplicateFrom, Transform parent, int stepMultiplier)
        {
            GameObject copy = Instantiate(duplicateFrom.gameObject);
            copy.name = duplicateFrom.gameObject.name;
            Undo.RegisterCreatedObjectUndo(copy, "Maya Special Duplicate");

            Transform copyTransform = copy.transform;
            SpecialDuplicateUtility.ApplyTransformToDuplicate(
                copyTransform,
                basis,
                duplicateFrom,
                stepMultiplier,
                BuildDuplicateSettings());

            if (parent != null)
            {
                copyTransform.SetParent(parent, true);
                EnsureReferenceUnderParent(parent);
            }

            return copyTransform;
        }

        SpecialDuplicateStepSettings BuildDuplicateSettings()
        {
            return new SpecialDuplicateStepSettings
            {
                translateIncrement = m_TranslateIncrement,
                rotateIncrementEuler = m_RotateIncrementEuler,
                scaleIncrement = m_ScaleIncrement,
                useLocalSpace = m_UseLocalSpace,
                applyScaleIncrement = m_ApplyScaleIncrement,
            };
        }

        void EnsureReferenceUnderParent(Transform parent)
        {
            if (m_ReferenceObject == null || m_ReferenceObject.parent == parent)
                return;

            ReparentUnderSharedParent(m_ReferenceObject, parent);
        }

        void ReparentUnderSharedParent(Transform target, Transform parentOverride = null)
        {
            Transform parent = parentOverride ?? ResolveParent();
            if (target == null || parent == null)
                return;

            Undo.SetTransformParent(target, parent, "Reparent To Shared Parent");
        }

        static void SaveVector3(string key, Vector3 value)
        {
            EditorPrefs.SetFloat(PrefsPrefix + key + ".x", value.x);
            EditorPrefs.SetFloat(PrefsPrefix + key + ".y", value.y);
            EditorPrefs.SetFloat(PrefsPrefix + key + ".z", value.z);
        }

        static Vector3 LoadVector3(string key, Vector3 defaultValue)
        {
            if (!EditorPrefs.HasKey(PrefsPrefix + key + ".x"))
                return defaultValue;

            return new Vector3(
                EditorPrefs.GetFloat(PrefsPrefix + key + ".x", defaultValue.x),
                EditorPrefs.GetFloat(PrefsPrefix + key + ".y", defaultValue.y),
                EditorPrefs.GetFloat(PrefsPrefix + key + ".z", defaultValue.z));
        }
    }
}
#endif
