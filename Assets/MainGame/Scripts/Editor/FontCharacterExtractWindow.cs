#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace MainGame.Editor
{
    /// <summary>
    /// 从指定目录的 .txt 文件中提取去重字符，便于手动合并进字体字符表，避免直接覆盖丢失手写字符。
    /// </summary>
    public sealed class FontCharacterExtractWindow : EditorWindow
    {
        const string WindowTitle = "字体字符提取";
        const string PrefsPrefix = "MainGame.FontCharacterExtract.";
        const string DefaultRelativeDir = "Assets/MainGame/DataTables/Text";

        [SerializeField]
        string m_Directory = DefaultRelativeDir;

        [SerializeField]
        bool m_IncludeSubfolders = false;

        [SerializeField]
        string m_Result = string.Empty;

        [SerializeField]
        Vector2 m_Scroll;

        int m_FileCount;
        int m_CharCount;
        int m_CjkCount;
        int m_AsciiCount;
        string m_Status = "选择目录后点击「提取字符」。";

        [MenuItem("Tools/字体/字符提取工具", priority = 150)]
        static void Open()
        {
            FontCharacterExtractWindow window = GetWindow<FontCharacterExtractWindow>(false, WindowTitle, true);
            window.minSize = new Vector2(520f, 420f);
            window.Show();
        }

        void OnEnable()
        {
            m_Directory = EditorPrefs.GetString(PrefsPrefix + nameof(m_Directory), DefaultRelativeDir);
            m_IncludeSubfolders = EditorPrefs.GetBool(PrefsPrefix + nameof(m_IncludeSubfolders), false);
        }

        void OnDisable()
        {
            EditorPrefs.SetString(PrefsPrefix + nameof(m_Directory), m_Directory ?? string.Empty);
            EditorPrefs.SetBool(PrefsPrefix + nameof(m_IncludeSubfolders), m_IncludeSubfolders);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("从目录中的 .txt 提取去重字符", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "结果只显示在窗口中，不会写入字体字符表。可复制后自行合并到「常用汉字.txt」。",
                MessageType.Info);

            EditorGUILayout.Space(4f);
            DrawDirectoryRow();
            m_IncludeSubfolders = EditorGUILayout.ToggleLeft("包含子目录", m_IncludeSubfolders);

            EditorGUILayout.Space(6f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("提取字符", GUILayout.Height(28f)))
                {
                    ExtractCharacters();
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(m_Result)))
                {
                    if (GUILayout.Button("复制全部", GUILayout.Width(100f), GUILayout.Height(28f)))
                    {
                        EditorGUIUtility.systemCopyBuffer = m_Result;
                        m_Status = $"已复制 {m_CharCount} 个字符到剪贴板。";
                    }

                    if (GUILayout.Button("清空结果", GUILayout.Width(100f), GUILayout.Height(28f)))
                    {
                        ClearResult();
                    }
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(m_Status, EditorStyles.wordWrappedLabel);

            if (m_CharCount > 0)
            {
                EditorGUILayout.LabelField(
                    $"文件 {m_FileCount}  |  总字符 {m_CharCount}  |  汉字 {m_CjkCount}  |  ASCII {m_AsciiCount}  |  其他 {m_CharCount - m_CjkCount - m_AsciiCount}");
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("提取结果（可框选复制，也可点「复制全部」）", EditorStyles.boldLabel);
            m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);
            m_Result = EditorGUILayout.TextArea(m_Result ?? string.Empty, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void DrawDirectoryRow()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("目录");
                m_Directory = EditorGUILayout.TextField(m_Directory);

                if (GUILayout.Button("浏览…", GUILayout.Width(60f)))
                {
                    string start = ResolveAbsoluteDirectory(m_Directory);
                    if (string.IsNullOrEmpty(start) || !Directory.Exists(start))
                    {
                        start = Application.dataPath;
                    }

                    string picked = EditorUtility.OpenFolderPanel("选择包含 .txt 的目录", start, string.Empty);
                    if (!string.IsNullOrEmpty(picked))
                    {
                        m_Directory = ToProjectRelativePath(picked);
                        GUI.FocusControl(null);
                    }
                }

                if (GUILayout.Button("默认", GUILayout.Width(50f)))
                {
                    m_Directory = DefaultRelativeDir;
                    GUI.FocusControl(null);
                }
            }
        }

        void ClearResult()
        {
            m_Result = string.Empty;
            m_FileCount = 0;
            m_CharCount = 0;
            m_CjkCount = 0;
            m_AsciiCount = 0;
            m_Status = "结果已清空。";
        }

        void ExtractCharacters()
        {
            string absoluteDir = ResolveAbsoluteDirectory(m_Directory);
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir))
            {
                m_Status = "目录不存在，请重新选择。";
                return;
            }

            SearchOption searchOption = m_IncludeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            string[] files;
            try
            {
                files = Directory.GetFiles(absoluteDir, "*.txt", searchOption);
            }
            catch (Exception ex)
            {
                m_Status = "读取目录失败：" + ex.Message;
                return;
            }

            if (files.Length == 0)
            {
                ClearResult();
                m_Status = "该目录下没有找到 .txt 文件。";
                return;
            }

            var unique = new HashSet<char>();
            int readOk = 0;
            var errors = new List<string>();

            foreach (string file in files)
            {
                try
                {
                    string text = File.ReadAllText(file, Encoding.UTF8);
                    foreach (char c in text)
                    {
                        if (char.IsControl(c))
                        {
                            continue;
                        }

                        unique.Add(c);
                    }

                    readOk++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                }
            }

            char[] sorted = unique.OrderBy(c => (int)c).ToArray();
            m_Result = new string(sorted);
            m_FileCount = readOk;
            m_CharCount = sorted.Length;
            m_CjkCount = sorted.Count(IsCjk);
            m_AsciiCount = sorted.Count(c => c < 128);

            if (errors.Count > 0)
            {
                m_Status = $"已提取 {readOk}/{files.Length} 个文件；{errors.Count} 个失败。首个错误：{errors[0]}";
            }
            else
            {
                m_Status = $"已从 {readOk} 个 .txt 提取 {m_CharCount} 个去重字符。";
            }
        }

        static bool IsCjk(char c)
        {
            int code = c;
            return code >= 0x4E00 && code <= 0x9FFF;
        }

        static string ResolveAbsoluteDirectory(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            path = path.Trim().Replace('\\', '/');
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("Assets", StringComparison.OrdinalIgnoreCase))
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                return Path.GetFullPath(Path.Combine(projectRoot ?? string.Empty, path));
            }

            return Path.GetFullPath(path);
        }

        static string ToProjectRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
            {
                return absolutePath;
            }

            absolutePath = Path.GetFullPath(absolutePath).Replace('\\', '/');
            string dataPath = Application.dataPath.Replace('\\', '/');
            if (absolutePath.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets" + absolutePath.Substring(dataPath.Length);
            }

            return absolutePath;
        }
    }
}
#endif
