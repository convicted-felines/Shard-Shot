using System.Collections.Generic;
using GameFramework;
using GameFramework.DataTable;
using GameFramework.Localization;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public static class LocationExtension
    {
        /// <summary>
        /// 翻译键（与 <see cref="DRUITranslation.Name"/> 一致）到配置表 Id；由 <see cref="BuildUiTranslationKeyToIdMap"/> 在 UITranslation 表加载成功后构建。
        /// </summary>
        private static Dictionary<string, int> s_UiTranslationKeyToId;

        /// <summary>
        /// 从已加载的 <see cref="DRUITranslation"/> 表构建键到 Id 映射。应在预加载流程中 UITranslation 数据表加载成功回调里调用。
        /// </summary>
        public static void BuildUiTranslationKeyToIdMap()
        {
            IDataTable<DRUITranslation> table = GameEntry.DataTable.GetDataTable<DRUITranslation>();
            if (table == null)
            {
                Log.Error("BuildUiTranslationKeyToIdMap: DRUITranslation data table is not loaded.");
                s_UiTranslationKeyToId = null;
                return;
            }

            var map = new Dictionary<string, int>();
            foreach (DRUITranslation row in table.GetAllDataRows())
            {
                if (row == null || string.IsNullOrEmpty(row.Name))
                {
                    continue;
                }

                map[row.Name] = row.Id;
            }

            s_UiTranslationKeyToId = map;
        }

        /// <summary>
        /// 判断 key 是否为 UI 翻译表中的键（<see cref="DRUITranslation.Name"/>），并解析出配置表 Id。
        /// </summary>
        public static bool TryGetUiTranslationTableId(this LocalizationComponent localizationComponent, string key, out int id)
        {
            if (string.IsNullOrEmpty(key))
            {
                id = -1;
                return false;
            }

            if (s_UiTranslationKeyToId == null)
            {
                id = -1;
                return false;
            }

            return s_UiTranslationKeyToId.TryGetValue(key, out id);
        }

        private static bool TryPickUiTranslationField(DRUITranslation row, Language language, out string text, string keyForLog)
        {
            switch (language)
            {
                case Language.ChineseSimplified:
                    text = row.ChineseSimplified;
                    return true;
                case Language.English:
                    text = row.English;
                    return true;
                case Language.ChineseTraditional:
                    text = row.ChineseTraditional;
                    return true;
                case Language.Korean:
                    text = row.Korean;
                    return true;
                default:
                    Log.Warning(
                        "Can not get location string with key '{0}' for unknown language '{1}'.",
                        keyForLog,
                        language.ToString());
                    text = null;
                    return false;
            }
        }

        /// <summary>
        /// 按配置表 Id 取当前语言的 UI 文本；无行时返回 null；未知语言时打日志并返回 <paramref name="fallbackKey"/>。
        /// </summary>
        public static string GetUITranslationStringByTableId(this LocalizationComponent localizationComponent, int tableId, string fallbackKey)
        {
            DRUITranslation row = GameEntry.DataTable.GetDataRow<DRUITranslation>(tableId);
            if (row == null)
            {
                return null;
            }

            Language language = GameEntry.Localization.AdaptLanguage;
            if (TryPickUiTranslationField(row, language, out string text, fallbackKey))
            {
                return text;
            }

            return fallbackKey;
        }
        public static string GetUITranslationStringByTableId(this LocalizationComponent localizationComponent, int tableId)
        {
            DRUITranslation row = GameEntry.DataTable.GetDataRow<DRUITranslation>(tableId);
            if (row == null)
            {
                return null;
            }

            Language language = GameEntry.Localization.AdaptLanguage;
            if (TryPickUiTranslationField(row, language, out string text,Utility.Text.Format("TranslationId:{0}",tableId) ))
            {
                return text;
            }

            return "";
        }

        /// <summary>
        /// 按 <see cref="UITranslationEnum"/> 取当前语言的 UI 文本；无行时返回 null；未知语言时打日志并返回 <paramref name="fallbackKey"/>。
        /// </summary>
        public static string GetUITranslationStringByTableId(this LocalizationComponent localizationComponent, UITranslationEnum translationEnum, string fallbackKey)
        {
            return localizationComponent.GetUITranslationStringByTableId((int)translationEnum, fallbackKey);
        }

        /// <summary>
        /// 按 <see cref="UITranslationEnum"/> 取当前语言的 UI 文本；无行时返回 null；未知语言时返回空字符串。
        /// </summary>
        public static string GetUITranslationStringByTableId(this LocalizationComponent localizationComponent, UITranslationEnum translationEnum)
        {
            return localizationComponent.GetUITranslationStringByTableId((int)translationEnum);
        }

        /// <summary>
        /// 按翻译键取当前语言文本；键不在翻译表或表行缺失时返回 <paramref name="key"/> 本身（与原先 <c>UGuiForm.UITextTranslation</c> 一致）。
        /// </summary>
        public static string GetUITranslationString(this LocalizationComponent localizationComponent, string key)
        {
            if (!localizationComponent.TryGetUiTranslationTableId(key, out int id))
            {
                return key;
            }

            string resolved = localizationComponent.GetUITranslationStringByTableId(id, key);
            return resolved ?? key;
        }

        public static string GetLoactionString(this LocalizationComponent localizationComponent, string key)
        {
            if (!localizationComponent.TryGetUiTranslationTableId(key, out int stringId))
            {
                Log.Warning("Can not get string id with key '{0}'.", key);
                return null;
            }

            DRUITranslation row = GameEntry.DataTable.GetDataRow<DRUITranslation>(stringId);
            if (row == null)
            {
                Log.Warning("Can not get data row with id '{0}'.", stringId);
                return null;
            }

            if (TryPickUiTranslationField(row, GameEntry.Localization.AdaptLanguage, out string text, key))
            {
                return text;
            }

            return null;
        }
    }
}
