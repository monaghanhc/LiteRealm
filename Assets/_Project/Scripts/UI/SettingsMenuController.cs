using System;
using UnityEngine;
using UnityEngine.UI;

namespace LiteRealm.UI
{
    public class SettingsMenuController : MonoBehaviour
    {
        [Header("Volume")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;

        [Header("Graphics")]
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Actions")]
        [SerializeField] private Button backButton;

        public event Action OnBackClicked;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClickedInternal);
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            }
        }

        private void Start()
        {
            PopulateQualityDropdown();
        }

        public void RefreshFromSettings()
        {
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            }

            PopulateQualityDropdown();
            if (qualityDropdown != null && qualityDropdown.options.Count > 0)
            {
                int level = Mathf.Clamp(GameSettings.QualityLevel, 0, qualityDropdown.options.Count - 1);
                qualityDropdown.SetValueWithoutNotify(level);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
            }
        }

        private void PopulateQualityDropdown()
        {
            if (qualityDropdown == null) return;
            string[] names = QualitySettings.names;
            if (names == null || names.Length == 0) return;
            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(names));
        }

        private void OnBackClickedInternal()
        {
            GameSettings.Save();
            OnBackClicked?.Invoke();
        }

        private void OnMasterVolumeChanged(float value)
        {
            GameSettings.MasterVolume = value;
        }

        private void OnMusicVolumeChanged(float value)
        {
            GameSettings.MusicVolume = value;
        }

        private void OnSfxVolumeChanged(float value)
        {
            GameSettings.SfxVolume = value;
        }

        private void OnQualityChanged(int index)
        {
            GameSettings.QualityLevel = index;
        }

        private void OnFullscreenChanged(bool value)
        {
            GameSettings.Fullscreen = value;
        }
    }
}
