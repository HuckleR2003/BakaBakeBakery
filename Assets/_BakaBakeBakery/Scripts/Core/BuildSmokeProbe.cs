using System;
using System.Collections;
using System.IO;
using BakaBakeBakery.Gameplay;
using BakaBakeBakery.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BakaBakeBakery.Core
{
    public sealed class BuildSmokeProbe : MonoBehaviour
    {
        private const string SmokeArgument = "-bakaSmokeTest";
        private const string CaptureArgument = "-bakaCaptureRoot";

        public static bool IsSmokeTest { get; } = Array.Exists(
            Environment.GetCommandLineArgs(),
            argument => string.Equals(argument, SmokeArgument, StringComparison.Ordinal));

        public static bool IsVisualCapture => !string.IsNullOrWhiteSpace(CaptureRoot);

        private static string CaptureRoot { get; } = GetArgumentValue(CaptureArgument);

        private IEnumerator Start()
        {
            if (IsVisualCapture)
            {
                Application.runInBackground = true;
            }

            if (IsSmokeTest)
            {
                yield return null;
                var smokeSceneName = SceneManager.GetActiveScene().name;
                if (smokeSceneName == SceneFlow.MainBakeryScene)
                {
                    yield return ExerciseBakeryLoop();
                }

                Debug.Log($"[Baka Bake Bakery] BUILD_SMOKE_READY {smokeSceneName} loaded successfully.");
                yield break;
            }

            var sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrWhiteSpace(CaptureRoot))
            {
                yield break;
            }

            Directory.CreateDirectory(CaptureRoot);
            if (sceneName == SceneFlow.StudioIntroScene)
            {
                yield return new WaitForSecondsRealtime(1.7f);
                yield return Capture("05-hck-labs-ident.png");
                yield return new WaitForSecondsRealtime(1.95f);
                yield return Capture("06-hck-labs-break.png");
                yield return new WaitForSecondsRealtime(1.12f);
                yield return Capture("09-transition-wipe.png");
            }
            else if (sceneName == SceneFlow.MainMenuScene)
            {
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Capture("07-main-menu.png");
                var menu = FindAnyObjectByType<MainMenuController>();
                menu?.ShowSettingsForDiagnostics();
                yield return new WaitForSecondsRealtime(0.25f);
                yield return Capture("08-settings.png");
                yield return new WaitForSecondsRealtime(0.25f);
                SceneFlow.TryLoad(SceneFlow.MainBakeryScene);
            }
            else if (sceneName == SceneFlow.MainBakeryScene)
            {
                BakeryGameController game = null;
                var timeout = Time.realtimeSinceStartup + 3f;
                while (Time.realtimeSinceStartup < timeout)
                {
                    game = FindAnyObjectByType<BakeryGameController>();
                    if (game != null && game.IsReady)
                    {
                        break;
                    }

                    yield return null;
                }

                if (game == null || !game.IsReady)
                {
                    Debug.LogError("[Baka Bake Bakery] Visual capture could not find ready gameplay.");
                    Application.Quit(1);
                    yield break;
                }

                yield return new WaitForSecondsRealtime(1.15f);
                yield return Capture("10-gameplay-home.png");
                game.RequestBakerAction();
                yield return new WaitForSecondsRealtime(0.42f);
                yield return Capture("11-baker-moving.png");
                yield return new WaitForSecondsRealtime(1.2f);
                yield return Capture("17-prep-board.png");
                yield return WaitForPhase(game, BakeryWorkPhase.WaitingForOven, 4f);
                game.RequestBakerAction();
                yield return new WaitForSecondsRealtime(2.1f);
                yield return Capture("12-oven-rhythm.png");
                yield return WaitForPhase(game, BakeryWorkPhase.WaitingForCounter, 8f);
                yield return new WaitForSecondsRealtime(0.12f);
                yield return Capture("16-oven-baked.png");
                game.RequestBakerAction();
                yield return WaitForPhase(game, BakeryWorkPhase.WaitingForDough, 2f);
                yield return new WaitForSecondsRealtime(0.12f);
                yield return Capture("14-counter-stocked.png");
                timeout = Time.realtimeSinceStartup + 4f;
                while (game.CurrentSnapshot.TotalItemsSold < 1 && Time.realtimeSinceStartup < timeout)
                {
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(0.35f);
                yield return Capture("13-first-sale.png");
                yield return new WaitForSecondsRealtime(0.2f);
                Application.Quit(game.CurrentSnapshot.TotalItemsSold >= 1 ? 0 : 1);
            }
        }

        private static IEnumerator ExerciseBakeryLoop()
        {
            BakeryGameController game = null;
            var timeout = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < timeout)
            {
                game = FindAnyObjectByType<BakeryGameController>();
                if (game != null && game.IsReady)
                {
                    break;
                }

                yield return null;
            }

            if (game == null || !game.IsReady)
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED controller did not become ready.");
                Application.Quit(1);
                yield break;
            }

            if (game.WorldView == null
                || game.WorkerView == null
                || game.WorldView.VisibleCounterItems != 0
                || !game.WorldView.RawIngredientsVisible)
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED initial world state was not an empty, prepared counter.");
                Application.Quit(1);
                yield break;
            }

            if (!game.RequestBakerAction())
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED dough action was rejected.");
                Application.Quit(1);
                yield break;
            }

            yield return WaitForPhase(game, BakeryWorkPhase.WaitingForOven, 4f);
            if (game.CurrentSnapshot.Phase != BakeryWorkPhase.WaitingForOven
                || !game.WorkerView.IsCarryingRaw
                || !game.RequestBakerAction())
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED oven action was not reached.");
                Application.Quit(1);
                yield break;
            }

            yield return WaitForPhase(game, BakeryWorkPhase.WaitingForCounter, 8f);
            if (game.CurrentSnapshot.Phase != BakeryWorkPhase.WaitingForCounter
                || !game.WorldView.OvenContentsVisible
                || !game.RequestBakerAction())
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED counter action was not reached.");
                Application.Quit(1);
                yield break;
            }

            yield return WaitForPhase(game, BakeryWorkPhase.WaitingForDough, 2f);
            yield return null;
            if (game.CurrentSnapshot.CounterStock < 1 || game.WorldView.VisibleCounterItems < 1)
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED finished bake never became visible on the counter.");
                Application.Quit(1);
                yield break;
            }

            timeout = Time.realtimeSinceStartup + 4f;
            while (game.CurrentSnapshot.TotalItemsSold < 1 && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            if (game.CurrentSnapshot.TotalItemsSold < 1)
            {
                Debug.LogError("[Baka Bake Bakery] GAMEPLAY_SMOKE_FAILED first sale did not complete.");
                Application.Quit(1);
                yield break;
            }

            Debug.Log("[Baka Bake Bakery] GAMEPLAY_SMOKE_READY first manual loaf completed and sold.");
            Application.Quit(0);
        }

        private static IEnumerator WaitForPhase(
            BakeryGameController game,
            BakeryWorkPhase expectedPhase,
            float timeoutSeconds)
        {
            var timeout = Time.realtimeSinceStartup + timeoutSeconds;
            while (game.CurrentSnapshot.Phase != expectedPhase && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }
        }

        private static IEnumerator Capture(string fileName)
        {
            var path = Path.Combine(CaptureRoot, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            ScreenCapture.CaptureScreenshot(path);
            var timeout = Time.realtimeSinceStartup + 1f;
            while (!File.Exists(path) && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            if (File.Exists(path))
            {
                Debug.Log($"[Baka Bake Bakery] VISUAL_CAPTURE {path}");
            }
            else
            {
                Debug.LogError($"[Baka Bake Bakery] Visual capture was not written: {path}");
            }
        }

        private static string GetArgumentValue(string argumentName)
        {
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index < arguments.Length - 1; index++)
            {
                if (string.Equals(arguments[index], argumentName, StringComparison.Ordinal))
                {
                    return arguments[index + 1];
                }
            }

            return string.Empty;
        }
    }
}
