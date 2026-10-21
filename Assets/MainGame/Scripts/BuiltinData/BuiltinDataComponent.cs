//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityGameFramework.Runtime;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace MainGame
{
    /// <summary>
    /// 内置数据组件
    /// </summary>
    public partial class BuiltinDataComponent : GameFrameworkComponent
    {
#if UNITY_EDITOR
        private const string DefaultSettingFileName = "GameFrameworkSetting.dat";
        private const string PersistentDataJsonFolderAssetPath = "Assets/MainGame/DataTables/Json";
        private const string PersistentDataJsonFileName = "PersistentDataSnapshot.json";
        private static readonly string DefaultSettingHelperTypeName = typeof(DefaultSettingHelper).FullName;
        private static readonly string PlayerPrefsSettingHelperTypeName = typeof(PlayerPrefsSettingHelper).FullName;
        private static readonly BindingFlags EditorPrivateInstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
#endif

        [SerializeField]
        private TextAsset m_BuildInfoTextAsset = null;
        [SerializeField]
        private TextAsset m_DefaultDictionaryTextAsset = null;
        [SerializeField]
        private SpriteAtlas m_UISpriteAtlas = null;
        [SerializeField]
        private UpdateResourceForm m_UpdateResourceFormTemplate;

        private Dictionary<string, object> m_GlobalTemporatyData = new Dictionary<string, object>();

        #region 序列化字段与属性

        private BuildInfo m_BuildInfo = null;

        public BuildInfo BuildInfo
        {
            get
            {
                return m_BuildInfo;
            }
        }

        public UpdateResourceForm UpdateResourceFormTemplate
        {
            get
            {
                return m_UpdateResourceFormTemplate;
            }
        }

        public SpriteAtlas UISpriteAtlas
        {
            get
            {
                return m_UISpriteAtlas;
            }
            set
            {
                m_UISpriteAtlas = value;
            }
        }

        #endregion

        #region 初始化与全局临时数据

        public void InitBuildInfo()
        {
            if (m_BuildInfoTextAsset == null || string.IsNullOrEmpty(m_BuildInfoTextAsset.text))
            {
                Log.Info("Build info can not be found or empty.");
                return;
            }

            m_BuildInfo = Utility.Json.ToObject<BuildInfo>(m_BuildInfoTextAsset.text);
            if (m_BuildInfo == null)
            {
                Log.Warning("Parse build info failure.");
            }
        }

        public void InitDefaultDictionary()
        {
            if (m_DefaultDictionaryTextAsset == null || string.IsNullOrEmpty(m_DefaultDictionaryTextAsset.text))
            {
                Log.Info("Default dictionary can not be found or empty.");
                return;
            }

            if (!GameEntry.Localization.ParseData(m_DefaultDictionaryTextAsset.text))
            {
                Log.Warning("Parse default dictionary failure.");
            }
        }

        public void SetGlobalTemporaryData(string key, object value)
        {
            m_GlobalTemporatyData[key] = value;
        }

        public object GetGlobalTemporaryData(string key)
        {
            return m_GlobalTemporatyData.TryGetValue(key, out object value) ? value : null;
        }

        #endregion
    }
}
