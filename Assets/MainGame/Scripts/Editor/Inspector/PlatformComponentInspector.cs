using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace UnityGameFramework.Editor
{
    [CustomEditor(typeof(MainGame.PlatformComponent))]
    internal sealed class PlatformComponentInspector : GameFrameworkInspector
    {
        private const string CustomOptionName = "<Custom>";

        private SerializedProperty m_InitializeOnAwake;
        private SerializedProperty m_PlatformHelperTypeName;
        private SerializedProperty m_CustomPlatformHelper;

        private string[] m_PlatformHelperTypeNames;
        private int m_PlatformHelperTypeNameIndex;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.UpdateIfRequiredOrScript();

            if (!CacheSerializedProperties())
            {
                EditorGUILayout.HelpBox("Platform inspector initialization failed, fallback to default inspector.", MessageType.Warning);
                DrawDefaultInspector();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (m_PlatformHelperTypeNames == null || m_PlatformHelperTypeNames.Length == 0)
            {
                RefreshTypeNames();
            }

            MainGame.PlatformComponent t = (MainGame.PlatformComponent)target;

            EditorGUILayout.PropertyField(m_InitializeOnAwake);

            EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
            {
                DrawPlatformHelperPopup();
            }
            EditorGUI.EndDisabledGroup();

            if (EditorApplication.isPlaying && IsPrefabInHierarchy(t.gameObject))
            {
                EditorGUILayout.LabelField("Current Platform", t.CurrentPlatform.ToString());
                EditorGUILayout.LabelField("Current Service", string.IsNullOrEmpty(t.CurrentServiceName) ? "<Unknown>" : t.CurrentServiceName);
            }

            serializedObject.ApplyModifiedProperties();

            Repaint();
        }

        protected override void OnCompileComplete()
        {
            base.OnCompileComplete();
            RefreshTypeNames();
        }

        private void OnEnable()
        {
            CacheSerializedProperties();
            RefreshTypeNames();
        }

        private bool CacheSerializedProperties()
        {
            m_InitializeOnAwake ??= serializedObject.FindProperty("m_InitializeOnAwake");
            m_PlatformHelperTypeName ??= serializedObject.FindProperty("m_PlatformHelperTypeName");
            m_CustomPlatformHelper ??= serializedObject.FindProperty("m_CustomPlatformHelper");
            return m_InitializeOnAwake != null && m_PlatformHelperTypeName != null && m_CustomPlatformHelper != null;
        }

        private void DrawPlatformHelperPopup()
        {
            int selectedIndex = EditorGUILayout.Popup("Platform Helper", m_PlatformHelperTypeNameIndex, m_PlatformHelperTypeNames);
            if (selectedIndex != m_PlatformHelperTypeNameIndex)
            {
                m_PlatformHelperTypeNameIndex = selectedIndex;
                m_PlatformHelperTypeName.stringValue = selectedIndex <= 0 ? null : m_PlatformHelperTypeNames[selectedIndex];
            }

            if (m_PlatformHelperTypeNameIndex <= 0)
            {
                EditorGUILayout.PropertyField(m_CustomPlatformHelper);
                if (m_CustomPlatformHelper.objectReferenceValue == null)
                {
                    EditorGUILayout.HelpBox("You must set Custom Platform Helper.", MessageType.Error);
                }
            }
        }

        private void RefreshTypeNames()
        {
            List<string> typeNameList = new List<string>
            {
                CustomOptionName
            };

            typeNameList.AddRange(GetRuntimeTypeNames(typeof(MainGame.PlatformServiceBase)));
            m_PlatformHelperTypeNames = typeNameList.ToArray();

            m_PlatformHelperTypeNameIndex = 0;
            if (!string.IsNullOrEmpty(m_PlatformHelperTypeName.stringValue))
            {
                m_PlatformHelperTypeNameIndex = typeNameList.IndexOf(m_PlatformHelperTypeName.stringValue);
                if (m_PlatformHelperTypeNameIndex <= 0)
                {
                    m_PlatformHelperTypeNameIndex = 0;
                    m_PlatformHelperTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static string[] GetRuntimeTypeNames(Type typeBase)
        {
            List<string> typeNames = new List<string>();
            string[] assemblyNames =
            {
                "UnityGameFramework.Runtime",
                "Assembly-CSharp"
            };

            foreach (string assemblyName in assemblyNames)
            {
                Assembly assembly = null;
                try
                {
                    assembly = Assembly.Load(assemblyName);
                }
                catch
                {
                    continue;
                }

                if (assembly == null)
                {
                    continue;
                }

                Type[] types = assembly.GetTypes();
                foreach (Type type in types)
                {
                    if (type.IsClass && !type.IsAbstract && typeBase.IsAssignableFrom(type))
                    {
                        typeNames.Add(type.FullName);
                    }
                }
            }

            typeNames.Sort();
            return typeNames.ToArray();
        }
    }
}
