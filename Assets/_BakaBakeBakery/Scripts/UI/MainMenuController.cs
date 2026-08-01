using BakaBakeBakery.Core;
using BakaBakeBakery.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BakaBakeBakery.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;

        private const float ConfirmWindowSeconds = 4f;

        private Button startButton;
        private Button newGameButton;
        private Button settingsButton;
        private Button quitButton;
        private Button closeSettingsButton;
        private Label newGameNote;
        private VisualElement settingsPanel;
        private IVisualElementScheduledItem disarmSchedule;
        private bool newGameArmed;
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
            newGameButton = root.Q<Button>("new-game-button");
            newGameNote = root.Q<Label>("new-game-note");
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
            if (newGameButton != null)
            {
                newGameButton.clicked += OnNewGameClicked;
            }

            RefreshSaveState();
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
            newGameArmed = false;
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

            if (newGameButton != null)
            {
                newGameButton.clicked -= OnNewGameClicked;
            }

            disarmSchedule?.Pause();
            disarmSchedule = null;

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

        /// <summary>
        /// Erasing a bakery is not undoable, so the first click only arms the button and the second
        /// one inside a short window actually clears the save.
        /// </summary>
        private void OnNewGameClicked()
        {
            if (!newGameArmed)
            {
                ArmNewGame();
                return;
            }

            newGameArmed = false;
            disarmSchedule?.Pause();
            BakeryProgressStore.Clear();
            RefreshSaveState();
            StartGame();
        }

        private void ArmNewGame()
        {
            if (!BakeryProgressStore.HasProgress)
            {
                StartGame();
                return;
            }

            newGameArmed = true;
            newGameButton.text = "ERASE AND START OVER?";
            newGameButton.AddToClassList("menu-button--arming");
            SetNote("Click again to lose the saved bakery for good.");
            disarmSchedule?.Pause();
            disarmSchedule = newGameButton.schedule
                .Execute(DisarmNewGame)
                .StartingIn(Mathf.RoundToInt(ConfirmWindowSeconds * 1000f));
        }

        private void DisarmNewGame()
        {
            newGameArmed = false;
            RefreshSaveState();
        }

        private void RefreshSaveState()
        {
            var hasProgress = BakeryProgressStore.HasProgress;
            startButton.text = hasProgress ? "CONTINUE BAKING" : "START BAKING";
            if (newGameButton == null)
            {
                return;
            }

            newGameButton.RemoveFromClassList("menu-button--arming");
            newGameButton.text = hasProgress ? "NEW BAKERY" : "NEW BAKERY  ·  READY";
            newGameButton.SetEnabled(true);
            SetNote(hasProgress
                ? "Starts the first morning again and clears the saved bakery."
                : "Nothing is saved yet, so this is the same as starting.");
        }

        private void SetNote(string text)
        {
            if (newGameNote != null)
            {
                newGameNote.text = text;
            }
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
