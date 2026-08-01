using System;
using System.Collections;
using System.IO;
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
            if (IsSmokeTest)
            {
                yield return null;
                var smokeSceneName = SceneManager.GetActiveScene().name;
                Debug.Log($"[Baka Bake Bakery] BUILD_SMOKE_READY {smokeSceneName} loaded successfully.");
                if (smokeSceneName == SceneFlow.MainBakeryScene)
                {
                    Application.Quit(0);
                }

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
                yield return new WaitForSecondsRealtime(0.82f);
                yield return Capture("09-transition-wipe.png");
            }
            else if (sceneName == SceneFlow.MainMenuScene)
            {
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Capture("07-main-menu.png");
                var menu = FindFirstObjectByType<MainMenuController>();
                menu?.ShowSettingsForDiagnostics();
                yield return new WaitForSecondsRealtime(0.25f);
                yield return Capture("08-settings.png");
                yield return new WaitForSecondsRealtime(0.25f);
                Application.Quit(0);
            }
        }

        private static IEnumerator Capture(string fileName)
        {
            var path = Path.Combine(CaptureRoot, fileName);
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
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
