using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BakaBakeBakery.Core
{
    public static class SceneFlow
    {
        public const string StudioIntroScene = "StudioIntro";
        public const string MainMenuScene = "MainMenu";
        public const string MainBakeryScene = "MainBakery";

        public static bool IsTransitioning { get; private set; }

        public static bool TryLoad(string sceneName)
        {
            if (IsTransitioning)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[Baka Bake Bakery] Scene is unavailable in the build: '{sceneName}'.");
                return false;
            }

            try
            {
                IsTransitioning = true;
                var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
                if (operation == null)
                {
                    IsTransitioning = false;
                    Debug.LogError($"[Baka Bake Bakery] Unity did not create a load operation for '{sceneName}'.");
                    return false;
                }

                operation.completed += _ => IsTransitioning = false;
                return true;
            }
            catch (Exception exception)
            {
                IsTransitioning = false;
                Debug.LogException(exception);
                return false;
            }
        }
    }
}
