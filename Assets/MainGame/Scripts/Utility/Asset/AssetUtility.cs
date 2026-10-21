//------------------------------------------------------------
// Game Framework
// Copyright © 2013-2021 Jiang Yin. All rights reserved.
// Homepage: https://gameframework.cn/
// Feedback: mailto:ellan@gameframework.cn
//------------------------------------------------------------

using GameFramework;

namespace MainGame
{
    /// <summary>
    /// MainGame 资源路径约定。业务与扩展方法应通过此类获取路径，禁止硬编码 Assets/ 前缀。
    /// </summary>
    public static class AssetUtility
    {
        public static string GetConfigAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/MainGame/DataTables/Text/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetDataTableAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/MainGame/DataTables/Text/{0}.{1}", assetName, fromBytes ? "bytes" : "txt");
        }

        public static string GetByteDataTableAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/DataTables/Bytes/{0}.bytes", assetName);
        }

        public static string GetDictionaryAsset(string assetName, bool fromBytes)
        {
            return Utility.Text.Format("Assets/MainGame/Localization/{0}/Dictionaries/{1}.{2}", GameEntry.Localization.Language, assetName, fromBytes ? "bytes" : "xml");
        }

        public static string GetFontAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Fonts/{0}", assetName);
        }

        public static string GetSpriteAtlasAsset(string spriteAtlasName)
        {
            return Utility.Text.Format("Assets/MainGame/UI/UISprite/{0}.spriteatlasv2", spriteAtlasName);
        }

        public static string GetSceneAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Scenes/{0}.unity", assetName);
        }

        public static string GetMusicAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Music/{0}.mp3", assetName);
        }

        public static string GetSoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Sounds/{0}.wav", assetName);
        }

        public static string GetEntityAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Entities/{0}.prefab", assetName);
        }

        public static string GetWorldUIAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/Entities/WorldUI/{0}.prefab", assetName);
        }

        public static string GetEmptyEntity()
        {
            return "Assets/MainGame/Entities/Other/EmptyEntity.prefab";
        }

        public static string GetUIFormAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/UI/UIForms/{0}.prefab", assetName);
        }

        public static string GetUISoundAsset(string assetName)
        {
            return Utility.Text.Format("Assets/MainGame/UI/UISounds/{0}.wav", assetName);
        }

        /// <summary>
        /// Input System 动作资源。默认 <c>GameControls.inputactions</c>，位于 <c>Configs/</c>。
        /// </summary>
        public static string GetInputActionsAsset(string assetName = "GameControls")
        {
            return Utility.Text.Format("Assets/MainGame/Configs/{0}.inputactions", assetName);
        }
    }
}
