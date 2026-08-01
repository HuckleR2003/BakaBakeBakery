using BakaBakeBakery.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BakaBakeBakery.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;

        private Button startButton;
        private Button settingsButton;
        private Button quitButton;
        private Button closeSettingsButton;
        private VisualElement settingsPanel;
        private Toggle fullscreenToggle;
        private Toggle reduceMotionToggle;
        private Slider volumeSlider;
        private Label volumeValue;
        private float smokeTimer;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            startButton = root.Q<Button>("start-button");
            settingsButton = root.Q<Button>("settings-button");
            quitButton = root.Q<Button>("quit-button");
            closeSettingsButton = root.Q<Button>("close-settings-button");
            settingsPanel = root.Q<VisualElement>("settings-panel");
            fullscreenToggle = root.Q<Toggle>("fullscreen-toggle");
            reduceMotionToggle = root.Q<Toggle>("reduce-motion-toggle");
            volumeSlider = root.Q<Slider>("volume-slider");
            volumeValue = root.Q<Label>("volume-value");

            if (startButton == null || settingsButton == null || quitButton == null || settingsPanel == null)
            {
                Debug.LogError("[Baka Bake Bakery] Main Menu UI is incomplete.");
                enabled = false;
                return;
            }

            startButton.clicked += StartGame;
            settingsButton.clicked += OpenSettings;
            quitButton.clicked += QuitGame;
            if (closeSettingsButton != null)
            {
                closeSettingsButton.clicked += CloseSettings;
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetValueWithoutNotify(GameSettings.Fullscreen);
                fullscreenToggle.RegisterValueChangedCallback(OnFullscreenChanged);
            }

            if (reduceMotionToggle != null)
            {
                reduceMotionToggle.SetValueWithoutNotify(GameSettings.ReduceMotion);
                reduceMotionToggle.RegisterValueChangedCallback(OnReduceMotionChanged);
            }

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume * 100f);
                volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
                UpdateVolumeLabel(volumeSlider.value);
            }

            settingsPanel.AddToClassList("settings-panel--closed");
            smokeTimer = 0f;
            startButton.schedule.Execute(startButton.Focus).StartingIn(120);
        }

        private void OnDisable()
        {
            if (startButton != null)
            {
                startButton.clicked -= StartGame;
            }

            if (settingsButton != null)
            {
                settingsButton.clicked -= OpenSettings;
            }

            if (quitButton != null)
            {
                quitButton.clicked -= QuitGame;
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.clicked -= CloseSettings;
            }

            fullscreenToggle?.UnregisterValueChangedCallback(OnFullscreenChanged);
            reduceMotionToggle?.UnregisterValueChangedCallback(OnReduceMotionChanged);
            volumeSlider?.UnregisterValueChangedCallback(OnVolumeChanged);
        }

        private void Update()
        {
            if (BuildSmokeProbe.IsSmokeTest)
            {
                smokeTimer += Time.unscaledDeltaTime;
                if (smokeTimer >= 0.15f)
                {
                    StartGame();
                }

                return;
            }

            if (Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame
                && settingsPanel != null
                && !settingsPanel.ClassListContains("settings-panel--closed"))
            {
                CloseSettings();
            }
        }

        private static void StartGame()
        {
            SceneFlow.TryLoad(SceneFlow.MainBakeryScene);
        }

        private void OpenSettings()
        {
            settingsPanel.RemoveFromClassList("settings-panel--closed");
            closeSettingsButton?.Focus();
        }

        public void ShowSettingsForDiagnostics()
        {
            OpenSettings();
        }

        private void CloseSettings()
        {
            settingsPanel.AddToClassList("settings-panel--closed");
            settingsButton?.Focus();
        }

        private static void QuitGame()
        {
            if (Application.isEditor)
            {
                Debug.Log("[Baka Bake Bakery] Quit requested in the Editor.");
                return;
            }

            Application.Quit(0);
        }

        private static void OnFullscreenChanged(ChangeEvent<bool> evt)
        {
            GameSettings.SetFullscreen(evt.newValue);
        }

        private static void OnReduceMotionChanged(ChangeEvent<bool> evt)
        {
            GameSettings.SetReduceMotion(evt.newValue);
        }

        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            GameSettings.SetMasterVolume(evt.newValue / 100f);
            UpdateVolumeLabel(evt.newValue);
        }

        private void UpdateVolumeLabel(float value)
        {
            if (volumeValue != null)
            {
                volumeValue.text = $"{Mathf.RoundToInt(value)}%";
            }
        }
    }
}
