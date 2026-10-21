using System.Collections.Generic;
using GameFramework.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

namespace MainGame
{
    public class SettingForm : UGuiForm
    {
        [Header("音量设置")]
        [SerializeField] private Slider m_MusicVolumeSlider = null;
        [SerializeField] private Slider m_SoundVolumeSlider = null;

        [Header("语言设置")]
        [Tooltip("与以前一样，在此配置可选语言种类；列表中的语言均会生成选项。")]
        [SerializeField] private Language[] m_SelectableLanguages =
        {
            Language.ChineseSimplified,
            Language.ChineseTraditional,
            Language.English,
            Language.Korean,
        };

        [SerializeField] private RectTransform m_LanguageToggleContainer = null;

        [Tooltip("作为样式的语言选项模板（CommonButton），运行时复制到布局中；请保持未激活。")]
        [SerializeField] private CommonButton m_LanguageOptionTemplate = null;

        [SerializeField] private TMP_Text m_LanguageChangeHintText = null;

        private readonly List<LanguageOptionEntry> m_LanguageOptionEntries = new List<LanguageOptionEntry>();

        private bool m_IsRefreshingView = false;
        private Language m_OriginalLanguage = Language.Unspecified;
        private bool m_NeedRestartForLanguage = false;

        private struct LanguageOptionEntry
        {
            public Language Language;
            public CommonButton Button;
            public GameObject Checkmark;
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);

            m_OriginalLanguage = GameEntry.Localization.Language;
            RebuildLanguageOptions();
            RefreshView();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);

            m_IsRefreshingView = false;
            m_OriginalLanguage = Language.Unspecified;
            m_NeedRestartForLanguage = false;
            ClearLanguageOptionEntries();
        }

        /// <summary>
        /// 「作者的话」按钮：打开提示弹窗并显示对应翻译文本。
        /// </summary>
        public void OnClickAuthorMessage()
        {
            // TODO: 配置表恢复后，改回 UITranslationEnum.作者的话
            GameEntry.UI.OpenDialog(new DialogParams
            {
                Mode = 1,
                Title = string.Empty,
                Message = string.Empty,
            });
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (m_IsRefreshingView)
            {
                return;
            }

            GameEntry.Sound.SetVolume("Music", value);
        }

        private void OnSoundVolumeChanged(float value)
        {
            if (m_IsRefreshingView)
            {
                return;
            }

            GameEntry.Sound.SetVolume("Sound", value);
        }

        private void RebuildLanguageOptions()
        {
            ClearLanguageOptionEntries();

            if (m_LanguageToggleContainer == null || m_LanguageOptionTemplate == null
                || m_SelectableLanguages == null || m_SelectableLanguages.Length == 0)
            {
                return;
            }

            Transform templateTransform = m_LanguageOptionTemplate.transform;
            for (int i = m_LanguageToggleContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = m_LanguageToggleContainer.GetChild(i);
                if (child != templateTransform)
                {
                    Destroy(child.gameObject);
                }
            }

            m_LanguageOptionTemplate.gameObject.SetActive(false);

            for (int i = 0; i < m_SelectableLanguages.Length; i++)
            {
                Language language = m_SelectableLanguages[i];
                CommonButton button = Instantiate(m_LanguageOptionTemplate, m_LanguageToggleContainer);
                button.gameObject.SetActive(true);
                button.m_OnClick.RemoveAllListeners();

                TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = GetLanguageDisplayName(language);
                }

                Language captured = language;
                button.m_OnClick.AddListener(() => OnLanguageOptionClicked(captured));

                m_LanguageOptionEntries.Add(new LanguageOptionEntry
                {
                    Language = language,
                    Button = button,
                    Checkmark = FindCheckmark(button.transform),
                });
            }
        }

        private void ClearLanguageOptionEntries()
        {
            for (int i = 0; i < m_LanguageOptionEntries.Count; i++)
            {
                CommonButton button = m_LanguageOptionEntries[i].Button;
                if (button != null)
                {
                    button.m_OnClick.RemoveAllListeners();
                }
            }

            m_LanguageOptionEntries.Clear();
        }

        private void OnLanguageOptionClicked(Language language)
        {
            if (m_IsRefreshingView)
            {
                return;
            }

            if (language == Language.Unspecified)
            {
                return;
            }

            if (language == GameEntry.Localization.Language)
            {
                RefreshLanguageOptionSelection(language);
                UpdateLanguageHint();
                return;
            }

            GameEntry.Localization.Language = language;
            Log.Info(language.ToString());
            GameEntry.Setting.SetCloudSyncedString(Constant.Setting.Language, language.ToString());
            GameEntry.Setting.Save();
            m_NeedRestartForLanguage = language != m_OriginalLanguage;
            RefreshLanguageOptionSelection(language);
            UpdateLanguageHint();
        }

        private void RefreshLanguageOptionSelection(Language selectedLanguage)
        {
            for (int i = 0; i < m_LanguageOptionEntries.Count; i++)
            {
                LanguageOptionEntry entry = m_LanguageOptionEntries[i];
                bool selected = entry.Language == selectedLanguage;

                if (entry.Checkmark != null)
                {
                    entry.Checkmark.SetActive(selected);
                }
            }
        }

        private void RefreshView()
        {
            m_IsRefreshingView = true;

            if (m_MusicVolumeSlider != null)
            {
                m_MusicVolumeSlider.SetValueWithoutNotify(GameEntry.Sound.GetVolume("Music"));
                m_MusicVolumeSlider.onValueChanged.RemoveAllListeners();
                m_MusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (m_SoundVolumeSlider != null)
            {
                m_SoundVolumeSlider.SetValueWithoutNotify(GameEntry.Sound.GetVolume("Sound"));
                m_SoundVolumeSlider.onValueChanged.RemoveAllListeners();
                m_SoundVolumeSlider.onValueChanged.AddListener(OnSoundVolumeChanged);
            }

            Language currentLanguage = GameEntry.Localization.AdaptLanguage;
            Language fallbackLanguage = GetFirstSelectableLanguage();
            if (!ContainsSelectableLanguage(currentLanguage) && fallbackLanguage != Language.Unspecified)
            {
                currentLanguage = fallbackLanguage;
            }

            RefreshLanguageOptionSelection(currentLanguage);

            m_NeedRestartForLanguage = currentLanguage != m_OriginalLanguage;
            UpdateLanguageHint();

            m_IsRefreshingView = false;
        }

        private void UpdateLanguageHint()
        {
            if (m_LanguageChangeHintText == null)
            {
                return;
            }

            // TODO: 配置表恢复后，改回 UITranslationEnum.语言切换提示
            m_LanguageChangeHintText.gameObject.SetActive(m_NeedRestartForLanguage);
        }

        private bool ContainsSelectableLanguage(Language language)
        {
            if (m_SelectableLanguages == null)
            {
                return false;
            }

            for (int i = 0; i < m_SelectableLanguages.Length; i++)
            {
                if (m_SelectableLanguages[i] == language)
                {
                    return true;
                }
            }

            return false;
        }

        private Language GetFirstSelectableLanguage()
        {
            if (m_SelectableLanguages == null || m_SelectableLanguages.Length == 0)
            {
                return Language.Unspecified;
            }

            return m_SelectableLanguages[0];
        }

        private static GameObject FindCheckmark(Transform root)
        {
            if (root == null)
            {
                return null;
            }

            Transform checkmark = root.Find("Background/Checkmark");
            if (checkmark != null)
            {
                return checkmark.gameObject;
            }

            checkmark = root.Find("Checkmark");
            return checkmark != null ? checkmark.gameObject : null;
        }

        private static string GetLanguageDisplayName(Language language)
        {
            switch (language)
            {
                case Language.English:
                    return "English";
                case Language.ChineseSimplified:
                    return "简体中文";
                case Language.ChineseTraditional:
                    return "繁體中文";
                case Language.Korean:
                    return "한국어";
                default:
                    return language.ToString();
            }
        }
    }
}
