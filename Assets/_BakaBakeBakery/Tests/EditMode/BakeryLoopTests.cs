using System.Collections.Generic;
using BakaBakeBakery.Data;
using BakaBakeBakery.Gameplay;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class BakeryLoopTests
    {
        [Test]
        public void FirstLoafRequiresThreeDeliberateActions()
        {
            var loop = CreateLoop();

            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.WaitingForDough));
            Assert.That(loop.RequestAction(), Is.True);
            Assert.That(loop.RequestAction(), Is.False, "Busy phases must reject click spam.");

            loop.Tick(2.5f);
            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.WaitingForOven));
            Assert.That(loop.RequestAction(), Is.True);

            loop.Tick(1f);
            loop.Tick(4.1f);
            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.WaitingForCounter));
            Assert.That(loop.RequestAction(), Is.True);

            loop.Tick(1f);
            Assert.That(loop.CounterStock, Is.EqualTo(1), "A fresh loaf must remain visible before the customer takes it.");
            Assert.That(loop.TotalItemsSold, Is.Zero);

            loop.Tick(1.3f);
            Assert.That(loop.TotalItemsSold, Is.EqualTo(1));
            Assert.That(loop.Coins, Is.EqualTo(6));
        }

        [Test]
        public void ManagerUsesSameSafeLoopAfterTenBreadSales()
        {
            var progress = new BakeryProgressData
            {
                coins = 60,
                countryBreadSold = 10,
                totalItemsSold = 10
            };
            var loop = CreateLoop(progress);

            Assert.That(loop.ManagerUnlocked, Is.True);
            for (var index = 0; index < 100; index++)
            {
                loop.Tick(0.25f);
            }

            Assert.That(loop.TotalItemsSold, Is.GreaterThan(10));
            Assert.That(loop.CounterStock, Is.LessThanOrEqualTo(loop.CounterCapacity));
        }

        [Test]
        public void KaiserAndSecondOvenRespectBreadAndCoinMilestones()
        {
            var loop = CreateLoop(new BakeryProgressData
            {
                coins = 119,
                countryBreadSold = 30,
                totalItemsSold = 30
            });

            Assert.That(loop.KaiserUnlocked, Is.True);
            Assert.That(loop.IsRecipeUnlocked(RecipeId.KaiserRoll), Is.True);
            Assert.That(loop.TryPurchaseSecondOven(), Is.False);

            var affordable = CreateLoop(new BakeryProgressData
            {
                coins = 120,
                countryBreadSold = 30,
                totalItemsSold = 30
            });

            Assert.That(affordable.TryPurchaseSecondOven(), Is.True);
            Assert.That(affordable.Coins, Is.Zero);
            Assert.That(affordable.CounterCapacity, Is.EqualTo(12));
            Assert.That(affordable.TryPurchaseSecondOven(), Is.False);
        }

        [Test]
        public void RecipeCannotChangeWhileFreshStockIsWaiting()
        {
            var loop = CreateLoop(new BakeryProgressData
            {
                countryBreadSold = 30,
                totalItemsSold = 30
            });

            Assert.That(loop.TrySelectRecipe(RecipeId.KaiserRoll), Is.True);
            loop.RequestAction();
            loop.Tick(2.5f);
            loop.RequestAction();
            loop.Tick(1f);
            loop.Tick(5f);
            loop.Tick(1.1f);
            loop.RequestAction();
            loop.Tick(1f);

            Assert.That(loop.CounterStock, Is.GreaterThan(0));
            Assert.That(loop.TrySelectRecipe(RecipeId.CountryBread), Is.False);
        }

        [Test]
        public void InvalidDeltaTimesCannotCorruptTheLoop()
        {
            var loop = CreateLoop();

            loop.Tick(float.NaN);
            loop.Tick(float.PositiveInfinity);
            loop.Tick(-2f);

            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.WaitingForDough));
            Assert.That(loop.Coins, Is.Zero);
            Assert.That(loop.CounterStock, Is.Zero);
        }

        [Test]
        public void CorruptedProgressIsClampedToSafeValues()
        {
            var progress = BakeryProgressStore.Sanitize(new BakeryProgressData
            {
                coins = -20,
                countryBreadSold = -5,
                totalItemsSold = -10,
                warmth = 999,
                bakeryLevel = 99,
                selectedRecipe = 999
            });

            Assert.That(progress.coins, Is.Zero);
            Assert.That(progress.countryBreadSold, Is.Zero);
            Assert.That(progress.totalItemsSold, Is.Zero);
            Assert.That(progress.warmth, Is.EqualTo(BakeryLoop.WarmthGoal - 1));
            Assert.That(progress.bakeryLevel, Is.EqualTo(2));
            Assert.That(progress.selectedRecipe, Is.EqualTo((int)RecipeId.CountryBread));
        }

        [Test]
        public void WoodenBakeryRequiresBothMilestoneAndExactPrice()
        {
            var tooEarly = CreateLoop(new BakeryProgressData
            {
                coins = BakeryLoop.BakeryUpgradeCost,
                countryBreadSold = BakeryLoop.BakeryUpgradeSales - 1,
                totalItemsSold = BakeryLoop.BakeryUpgradeSales - 1
            });

            Assert.That(tooEarly.BakeryUpgradeAvailable, Is.False);
            Assert.That(tooEarly.TryPurchaseBakeryUpgrade(), Is.False);

            var ready = CreateLoop(new BakeryProgressData
            {
                coins = BakeryLoop.BakeryUpgradeCost,
                countryBreadSold = BakeryLoop.BakeryUpgradeSales,
                totalItemsSold = BakeryLoop.BakeryUpgradeSales
            });

            Assert.That(ready.TryPurchaseBakeryUpgrade(), Is.True);
            Assert.That(ready.BakeryLevel, Is.EqualTo(2));
            Assert.That(ready.CounterCapacity, Is.EqualTo(16));
            Assert.That(ready.Coins, Is.Zero);
            Assert.That(ready.TryPurchaseBakeryUpgrade(), Is.False);
        }

        [Test]
        public void ProgressRoundTripRestoresOnlyUnlockedRecipeState()
        {
            var original = CreateLoop(new BakeryProgressData
            {
                coins = 240,
                countryBreadSold = 45,
                totalItemsSold = 45,
                secondOvenPurchased = true,
                selectedRecipe = (int)RecipeId.ButterCroissant
            });
            var restored = CreateLoop(original.ExportProgress());

            Assert.That(restored.SecondOvenPurchased, Is.True);
            Assert.That(restored.SelectedRecipe, Is.EqualTo(RecipeId.ButterCroissant));
            Assert.That(restored.Coins, Is.EqualTo(240));

            var lockedSelection = CreateLoop(new BakeryProgressData
            {
                countryBreadSold = 30,
                totalItemsSold = 100,
                bakeryLevel = 1,
                selectedRecipe = (int)RecipeId.Finezja
            });

            Assert.That(lockedSelection.SelectedRecipe, Is.EqualTo(RecipeId.CountryBread));
        }

        [Test]
        public void CraftedRecipeUnlockPersistsAndCannotUnlockTwice()
        {
            var loop = CreateLoop();

            Assert.That(loop.IsRecipeUnlocked(RecipeId.ChocolateMuffin), Is.False);
            Assert.That(loop.UnlockCraftedRecipe(RecipeId.ChocolateMuffin), Is.True);
            Assert.That(loop.UnlockCraftedRecipe(RecipeId.ChocolateMuffin), Is.False);

            var restored = CreateLoop(loop.ExportProgress());
            Assert.That(restored.IsRecipeUnlocked(RecipeId.ChocolateMuffin), Is.True);
        }

        [Test]
        public void ClosingShiftCancelsIncompleteMotionAndClearsCustomerQueue()
        {
            var loop = CreateLoop();
            Assert.That(loop.RequestAction(), Is.True);
            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.FetchingDough));

            loop.CloseShift();

            Assert.That(loop.Phase, Is.EqualTo(BakeryWorkPhase.WaitingForDough));
            Assert.That(loop.WaitingCustomers, Is.Zero);
            Assert.That(loop.CounterStock, Is.Zero);
        }

        private static BakeryLoop CreateLoop(BakeryProgressData progress = null)
        {
            return new BakeryLoop(new List<BakeryRecipeSpec>
            {
                new(RecipeId.CountryBread, "Country Bread", 4f, 1, 6, 0, 1),
                new(RecipeId.KaiserRoll, "Basic Kaiser Roll", 6f, 3, 3, 30, 1),
                new(RecipeId.ButterCroissant, "Butter Croissant", 8f, 2, 8, 45, 1),
                new(RecipeId.CinnamonSwirl, "Cinnamon Swirl", 9f, 3, 7, 75, 2),
                new(RecipeId.Finezja, "Finezja", 14f, 2, 11, 100, 2),
                new(RecipeId.CinnamonMonocle, "Cinnamon Monocle", 10f, 3, 9, 125, 2),
                new(RecipeId.ChocolateMuffin, "Chocolate Muffin", 10f, 3, 8, 0, 1),
                new(RecipeId.JamTurnover, "Village Jam Turnover", 9f, 2, 10, 0, 1),
                new(RecipeId.ChocolatePillow, "Chocolate Pillow", 11f, 2, 12, 0, 1)
            }, progress);
        }
    }
}
