using System;
using BakaBakeBakery.Data;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    public static class BakeryProgressStore
    {
        private const string ProgressKey = "BakaBakeBakery.Progress.v1";

        public static bool HasProgress => PlayerPrefs.HasKey(ProgressKey);

        public static BakeryProgressData Load()
        {
            if (!PlayerPrefs.HasKey(ProgressKey))
            {
                return Sanitize(null);
            }

            try
            {
                var json = PlayerPrefs.GetString(ProgressKey, string.Empty);
                return Sanitize(JsonUtility.FromJson<BakeryProgressData>(json));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Baka Bake Bakery] Progress was unreadable and has been reset safely: {exception.Message}");
                return Sanitize(null);
            }
        }

        public static void Save(BakeryProgressData progress)
        {
            var safeProgress = Sanitize(progress);
            PlayerPrefs.SetString(ProgressKey, JsonUtility.ToJson(safeProgress));
            PlayerPrefs.Save();
        }

        public static BakeryProgressData Sanitize(BakeryProgressData progress)
        {
            var safe = progress ?? new BakeryProgressData();
            safe.version = BakeryProgressData.CurrentVersion;
            safe.coins = Math.Clamp(safe.coins, 0, 1000000000);
            safe.countryBreadSold = Math.Clamp(safe.countryBreadSold, 0, 100000000);
            safe.totalItemsSold = Math.Clamp(safe.totalItemsSold, safe.countryBreadSold, 100000000);
            safe.warmth = Math.Clamp(safe.warmth, 0, BakeryLoop.WarmthGoal - 1);
            safe.bakeryLevel = Math.Clamp(safe.bakeryLevel, 1, 2);

            if (!Enum.IsDefined(typeof(RecipeId), safe.selectedRecipe))
            {
                safe.selectedRecipe = (int)RecipeId.CountryBread;
            }

            return safe;
        }
    }
}
