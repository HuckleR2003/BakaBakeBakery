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
            safe.discoveryMask = Math.Clamp(safe.discoveryMask, 0, 0b111);
            safe.dayNumber = Math.Clamp(safe.dayNumber, 1, 9999);
            safe.dayPhase = Enum.IsDefined(typeof(BakeryDayPhase), safe.dayPhase)
                ? safe.dayPhase
                : (int)BakeryDayPhase.MorningPreparation;
            safe.daySecondsRemaining = Math.Clamp(safe.daySecondsRemaining, 0f, BakeryDayCycle.DayDurationSeconds);
            safe.dailyCosts = Math.Clamp(safe.dailyCosts, 0, 1000000000);
            safe.dailyRevenue = Math.Clamp(safe.dailyRevenue, 0, 1000000000);
            safe.dailyItemsSold = Math.Clamp(safe.dailyItemsSold, 0, 100000000);
            safe.tutorialStep = Math.Clamp(safe.tutorialStep, 0, BakeryTutorial.FinalStep);
            safe.flour = Math.Clamp(safe.flour, 0, 999);
            safe.milk = Math.Clamp(safe.milk, 0, 999);
            safe.puffPastry = Math.Clamp(safe.puffPastry, 0, 999);
            safe.jam = Math.Clamp(safe.jam, 0, 999);
            safe.chocolate = Math.Clamp(safe.chocolate, 0, 999);

            if (!Enum.IsDefined(typeof(RecipeId), safe.selectedRecipe))
            {
                safe.selectedRecipe = (int)RecipeId.CountryBread;
            }

            return safe;
        }
    }
}
