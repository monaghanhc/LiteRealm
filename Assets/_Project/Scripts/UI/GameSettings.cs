using UnityEngine;

namespace LiteRealm.UI
{
    /// <summary>
    /// Persists and applies game settings (volume, quality, fullscreen) via PlayerPrefs.
    /// </summary>
    public static class GameSettings
    {
        private const string KeyMasterVolume = "LiteRealm.MasterVolume";
        private const string KeyMusicVolume = "LiteRealm.MusicVolume";
        private const string KeySfxVolume = "LiteRealm.SfxVolume";
        private const string KeyQualityLevel = "LiteRealm.QualityLevel";
        private const string KeyFullscreen = "LiteRealm.Fullscreen";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
            set
            {
                value = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(KeyMasterVolume, value);
                ApplyMasterVolume();
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusicVolume, 1f);
            set => PlayerPrefs.SetFloat(KeyMusicVolume, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfxVolume, 1f);
            set => PlayerPrefs.SetFloat(KeySfxVolume, Mathf.Clamp01(value));
        }

        public static int QualityLevel
        {
            get => PlayerPrefs.GetInt(KeyQualityLevel, QualitySettings.GetQualityLevel());
            set
            {
                int level = Mathf.Clamp(value, 0, QualitySettings.names.Length - 1);
                PlayerPrefs.SetInt(KeyQualityLevel, level);
                QualitySettings.SetQualityLevel(level);
            }
        }

        public static bool Fullscreen
        {
            get => PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) != 0;
            set
            {
                PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedSettings()
        {
            ApplyMasterVolume();
            int level = Mathf.Clamp(QualityLevel, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(level);
            Screen.fullScreen = Fullscreen;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ReapplyVolumeAfterSceneLoad()
        {
            ApplyMasterVolume();
        }

        private static void ApplyMasterVolume()
        {
            float vol = Mathf.Clamp01(MasterVolume);
            if (AudioListener.volume != vol)
            {
                AudioListener.volume = vol;
            }
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
