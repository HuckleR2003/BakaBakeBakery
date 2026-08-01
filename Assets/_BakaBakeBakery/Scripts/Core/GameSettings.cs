using UnityEngine;

namespace BakaBakeBakery.Core
{
    public static class GameSettings
    {
        private const string KeyPrefix = "BakaBakeBakery.Settings.";
        private const string MasterVolumeKey = KeyPrefix + "MasterVolume";
        private const string FullscreenKey = KeyPrefix + "Fullscreen";
        private const string ReduceMotionKey = KeyPrefix + "ReduceMotion";

        public const float DefaultMasterVolume = 0.8f;

        public static float MasterVolume { get; private set; } = DefaultMasterVolume;
        public static bool Fullscreen { get; private set; }
        public static bool ReduceMotion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeBeforeFirstScene()
        {
            Reload();
        }

        public static void Reload()
        {
            MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume));
            Fullscreen = PlayerPrefs.GetInt(FullscreenKey, 0) == 1;
            ReduceMotion = PlayerPrefs.GetInt(ReduceMotionKey, 0) == 1;
            ApplyRuntimeValues();
        }

        public static void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.Save();
            AudioListener.volume = MasterVolume;
        }

        public static void SetFullscreen(bool value)
        {
            Fullscreen = value;
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
            ApplyDisplayMode();
        }

        public static void SetReduceMotion(bool value)
        {
            ReduceMotion = value;
            PlayerPrefs.SetInt(ReduceMotionKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        private static void ApplyRuntimeValues()
        {
            AudioListener.volume = MasterVolume;
            ApplyDisplayMode();
        }

        private static void ApplyDisplayMode()
        {
            if (Application.isEditor || Application.isBatchMode)
            {
                return;
            }

            Screen.fullScreenMode = Fullscreen
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
        }
    }
}
