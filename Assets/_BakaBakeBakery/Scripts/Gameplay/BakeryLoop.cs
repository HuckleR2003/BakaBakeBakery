using System;
using System.Collections.Generic;
using BakaBakeBakery.Data;

namespace BakaBakeBakery.Gameplay
{
    public enum BakeryWorkPhase
    {
        WaitingForDough,
        FetchingDough,
        WaitingForOven,
        LoadingOven,
        Baking,
        WaitingForCounter,
        Serving
    }

    public enum BakeryLoopEventType
    {
        WorkStarted,
        BatchCompleted,
        SaleCompleted,
        CustomerArrived,
        ManagerUnlocked,
        RecipeUnlocked,
        SecondOvenPurchased,
        BakeryUpgraded,
        GoldenMinuteStarted,
        GoldenMinuteEnded
    }

    public readonly struct BakeryRecipeSpec
    {
        public BakeryRecipeSpec(
            RecipeId id,
            string displayName,
            float bakeSeconds,
            int batchYield,
            int salePrice,
            int unlockAtSales,
            int requiredBakeryLevel)
        {
            Id = id;
            DisplayName = displayName;
            BakeSeconds = Math.Max(0.1f, bakeSeconds);
            BatchYield = Math.Max(1, batchYield);
            SalePrice = Math.Max(1, salePrice);
            UnlockAtSales = Math.Max(0, unlockAtSales);
            RequiredBakeryLevel = Math.Max(1, requiredBakeryLevel);
        }

        public RecipeId Id { get; }
        public string DisplayName { get; }
        public float BakeSeconds { get; }
        public int BatchYield { get; }
        public int SalePrice { get; }
        public int UnlockAtSales { get; }
        public int RequiredBakeryLevel { get; }
    }

    public readonly struct BakeryLoopEvent
    {
        public BakeryLoopEvent(BakeryLoopEventType type, RecipeId recipeId, int amount = 0)
        {
            Type = type;
            RecipeId = recipeId;
            Amount = amount;
        }

        public BakeryLoopEventType Type { get; }
        public RecipeId RecipeId { get; }
        public int Amount { get; }
    }

    public readonly struct BakerySnapshot
    {
        public BakerySnapshot(BakeryLoop loop)
        {
            Coins = loop.Coins;
            CountryBreadSold = loop.CountryBreadSold;
            TotalItemsSold = loop.TotalItemsSold;
            CounterStock = loop.CounterStock;
            CounterCapacity = loop.CounterCapacity;
            WaitingCustomers = loop.WaitingCustomers;
            Warmth = loop.Warmth;
            WarmthGoal = BakeryLoop.WarmthGoal;
            BakeryLevel = loop.BakeryLevel;
            SelectedRecipe = loop.SelectedRecipe;
            StockRecipe = loop.StockRecipe;
            Phase = loop.Phase;
            PhaseProgress = loop.PhaseProgress;
            PhaseRemaining = loop.PhaseRemaining;
            ManagerUnlocked = loop.ManagerUnlocked;
            KaiserUnlocked = loop.KaiserUnlocked;
            SecondOvenPurchased = loop.SecondOvenPurchased;
            BakeryUpgradeAvailable = loop.BakeryUpgradeAvailable;
            GoldenMinuteRemaining = loop.GoldenMinuteRemaining;
            CanRequestAction = loop.CanRequestAction;
        }

        public int Coins { get; }
        public int CountryBreadSold { get; }
        public int TotalItemsSold { get; }
        public int CounterStock { get; }
        public int CounterCapacity { get; }
        public int WaitingCustomers { get; }
        public int Warmth { get; }
        public int WarmthGoal { get; }
        public int BakeryLevel { get; }
        public RecipeId SelectedRecipe { get; }
        public RecipeId StockRecipe { get; }
        public BakeryWorkPhase Phase { get; }
        public float PhaseProgress { get; }
        public float PhaseRemaining { get; }
        public bool ManagerUnlocked { get; }
        public bool KaiserUnlocked { get; }
        public bool SecondOvenPurchased { get; }
        public bool BakeryUpgradeAvailable { get; }
        public float GoldenMinuteRemaining { get; }
        public bool GoldenMinuteActive => GoldenMinuteRemaining > 0f;
        public bool CanRequestAction { get; }
    }

    [Serializable]
    public sealed class BakeryProgressData
    {
        public const int CurrentVersion = 2;

        public int version = CurrentVersion;
        public int coins;
        public int countryBreadSold;
        public int totalItemsSold;
        public int warmth;
        public int bakeryLevel = 1;
        public bool secondOvenPurchased;
        public int selectedRecipe;
        public int discoveryMask;
        public int dayNumber = 1;
        public int dayPhase;
        public float daySecondsRemaining;
        public int dailyCosts;
        public int dailyRevenue;
        public int tutorialStep;
        public int flour;
        public int milk;
        public int puffPastry;
        public int jam;
        public int chocolate;
    }

    public sealed class BakeryLoop
    {
        public const int ManagerUnlockBreadSales = 10;
        public const int KaiserUnlockBreadSales = 30;
        public const int BakeryUpgradeSales = 55;
        public const int SecondOvenCost = 120;
        public const int BakeryUpgradeCost = 200;
        public const int WarmthGoal = 12;
        public const float GoldenMinuteDuration = 18f;

        private const float FetchDuration = 2.4f;
        private const float LoadDuration = 0.78f;
        private const float ServeDuration = 0.82f;
        private const float ManagerActionDelay = 0.72f;
        private const float SaleInterval = 0.58f;
        private const float FreshCounterDisplayTime = 1.25f;
        private const int MaximumWaitingCustomers = 2;

        private readonly Dictionary<RecipeId, BakeryRecipeSpec> recipes = new();
        private readonly Queue<BakeryLoopEvent> events = new();

        private float phaseDuration;
        private float managerActionRemaining = ManagerActionDelay;
        private float customerArrivalRemaining = 2.8f;
        private float saleCooldown;

        public BakeryLoop(IEnumerable<BakeryRecipeSpec> recipeSpecs, BakeryProgressData progress = null)
        {
            if (recipeSpecs == null)
            {
                throw new ArgumentNullException(nameof(recipeSpecs));
            }

            foreach (var recipe in recipeSpecs)
            {
                recipes[recipe.Id] = recipe;
            }

            if (!recipes.ContainsKey(RecipeId.CountryBread))
            {
                throw new ArgumentException("The bakery loop requires a Country Bread recipe.", nameof(recipeSpecs));
            }

            var safeProgress = BakeryProgressStore.Sanitize(progress);
            Coins = safeProgress.coins;
            CountryBreadSold = safeProgress.countryBreadSold;
            TotalItemsSold = safeProgress.totalItemsSold;
            Warmth = safeProgress.warmth;
            BakeryLevel = safeProgress.bakeryLevel;
            SecondOvenPurchased = safeProgress.secondOvenPurchased;
            DiscoveryMask = safeProgress.discoveryMask;

            var restoredRecipe = (RecipeId)safeProgress.selectedRecipe;
            SelectedRecipe = recipes.ContainsKey(restoredRecipe) && IsRecipeUnlocked(restoredRecipe)
                ? restoredRecipe
                : RecipeId.CountryBread;
            StockRecipe = SelectedRecipe;
            Phase = BakeryWorkPhase.WaitingForDough;
            WaitingCustomers = 1;
        }

        public int Coins { get; private set; }
        public int CountryBreadSold { get; private set; }
        public int TotalItemsSold { get; private set; }
        public int CounterStock { get; private set; }
        public int WaitingCustomers { get; private set; }
        public int Warmth { get; private set; }
        public int BakeryLevel { get; private set; }
        public RecipeId SelectedRecipe { get; private set; }
        public RecipeId StockRecipe { get; private set; }
        public BakeryWorkPhase Phase { get; private set; }
        public float PhaseRemaining { get; private set; }
        public float GoldenMinuteRemaining { get; private set; }
        public bool SecondOvenPurchased { get; private set; }
        public int DiscoveryMask { get; private set; }

        public bool ManagerUnlocked => CountryBreadSold >= ManagerUnlockBreadSales;
        public bool KaiserUnlocked => CountryBreadSold >= KaiserUnlockBreadSales;
        public bool BakeryUpgradeAvailable => BakeryLevel == 1 && TotalItemsSold >= BakeryUpgradeSales;
        public int CounterCapacity => BakeryLevel >= 2 ? 16 : SecondOvenPurchased ? 12 : 8;
        public float PhaseProgress => phaseDuration <= 0f
            ? IsWaitingPhase(Phase) ? 0f : 1f
            : Math.Clamp(1f - PhaseRemaining / phaseDuration, 0f, 1f);
        public bool CanRequestAction => IsActionPhase(Phase)
            && (Phase != BakeryWorkPhase.WaitingForDough || HasCounterRoomForSelectedBatch());
        public BakerySnapshot Snapshot => new(this);

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0f)
            {
                return;
            }

            var safeDelta = Math.Min(deltaTime, 5f);
            TickGoldenMinute(safeDelta);
            TickCustomers(safeDelta);
            TickSales(safeDelta);
            TickWork(safeDelta);
            TickManager(safeDelta);
        }

        public bool RequestAction()
        {
            if (!CanRequestAction)
            {
                return false;
            }

            switch (Phase)
            {
                case BakeryWorkPhase.WaitingForDough:
                    BeginPhase(BakeryWorkPhase.FetchingDough, FetchDuration);
                    break;
                case BakeryWorkPhase.WaitingForOven:
                    BeginPhase(BakeryWorkPhase.LoadingOven, LoadDuration);
                    break;
                case BakeryWorkPhase.WaitingForCounter:
                    BeginPhase(BakeryWorkPhase.Serving, ServeDuration);
                    break;
                default:
                    return false;
            }

            managerActionRemaining = ManagerActionDelay;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.WorkStarted, SelectedRecipe));
            return true;
        }

        public bool TrySelectRecipe(RecipeId recipeId)
        {
            if (!recipes.ContainsKey(recipeId)
                || !IsRecipeUnlocked(recipeId)
                || Phase != BakeryWorkPhase.WaitingForDough
                || CounterStock > 0)
            {
                return false;
            }

            SelectedRecipe = recipeId;
            StockRecipe = recipeId;
            return true;
        }

        public bool TryPurchaseSecondOven()
        {
            if (SecondOvenPurchased || !KaiserUnlocked || Coins < SecondOvenCost)
            {
                return false;
            }

            Coins -= SecondOvenCost;
            SecondOvenPurchased = true;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.SecondOvenPurchased, SelectedRecipe));
            return true;
        }

        public bool TryPurchaseBakeryUpgrade()
        {
            if (!BakeryUpgradeAvailable || Coins < BakeryUpgradeCost)
            {
                return false;
            }

            Coins -= BakeryUpgradeCost;
            BakeryLevel = 2;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.BakeryUpgraded, SelectedRecipe));
            return true;
        }

        public bool TrySpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount)
            {
                return false;
            }

            Coins -= amount;
            return true;
        }

        public void CloseShift()
        {
            if (Phase != BakeryWorkPhase.WaitingForDough)
            {
                EnterWaitingPhase(BakeryWorkPhase.WaitingForDough);
            }

            WaitingCustomers = 0;
            managerActionRemaining = ManagerActionDelay;
        }

        public bool IsRecipeUnlocked(RecipeId recipeId)
        {
            return recipes.TryGetValue(recipeId, out var recipe)
                && IsRecipeUnlocked(recipe, TotalItemsSold, CountryBreadSold, BakeryLevel, DiscoveryMask);
        }

        public bool UnlockCraftedRecipe(RecipeId recipeId)
        {
            var bit = DiscoveryBit(recipeId);
            if (bit == 0 || (DiscoveryMask & bit) != 0 || !recipes.ContainsKey(recipeId))
            {
                return false;
            }

            DiscoveryMask |= bit;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.RecipeUnlocked, recipeId));
            return true;
        }

        public BakeryRecipeSpec GetRecipe(RecipeId recipeId)
        {
            return recipes.TryGetValue(recipeId, out var recipe)
                ? recipe
                : recipes[RecipeId.CountryBread];
        }

        public bool TryDequeueEvent(out BakeryLoopEvent loopEvent)
        {
            if (events.Count == 0)
            {
                loopEvent = default;
                return false;
            }

            loopEvent = events.Dequeue();
            return true;
        }

        public BakeryProgressData ExportProgress()
        {
            return new BakeryProgressData
            {
                version = BakeryProgressData.CurrentVersion,
                coins = Coins,
                countryBreadSold = CountryBreadSold,
                totalItemsSold = TotalItemsSold,
                warmth = Warmth,
                bakeryLevel = BakeryLevel,
                secondOvenPurchased = SecondOvenPurchased,
                selectedRecipe = (int)SelectedRecipe,
                discoveryMask = DiscoveryMask
            };
        }

        private void TickWork(float deltaTime)
        {
            if (!IsTimedPhase(Phase))
            {
                return;
            }

            PhaseRemaining = Math.Max(0f, PhaseRemaining - deltaTime);
            if (PhaseRemaining > 0f)
            {
                return;
            }

            switch (Phase)
            {
                case BakeryWorkPhase.FetchingDough:
                    EnterWaitingPhase(BakeryWorkPhase.WaitingForOven);
                    break;
                case BakeryWorkPhase.LoadingOven:
                    var recipe = recipes[SelectedRecipe];
                    var ovenSpeed = SecondOvenPurchased ? 1.4f : 1f;
                    BeginPhase(BakeryWorkPhase.Baking, recipe.BakeSeconds / ovenSpeed);
                    break;
                case BakeryWorkPhase.Baking:
                    EnterWaitingPhase(BakeryWorkPhase.WaitingForCounter);
                    break;
                case BakeryWorkPhase.Serving:
                    CompleteBatch();
                    EnterWaitingPhase(BakeryWorkPhase.WaitingForDough);
                    break;
            }
        }

        private void TickManager(float deltaTime)
        {
            if (!ManagerUnlocked || !CanRequestAction)
            {
                managerActionRemaining = ManagerActionDelay;
                return;
            }

            managerActionRemaining -= deltaTime;
            if (managerActionRemaining <= 0f)
            {
                RequestAction();
            }
        }

        private void TickCustomers(float deltaTime)
        {
            customerArrivalRemaining -= deltaTime;
            if (customerArrivalRemaining > 0f)
            {
                return;
            }

            customerArrivalRemaining = NextCustomerDelay();
            if (WaitingCustomers >= MaximumWaitingCustomers)
            {
                return;
            }

            WaitingCustomers++;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.CustomerArrived, SelectedRecipe));
        }

        private void TickSales(float deltaTime)
        {
            saleCooldown -= deltaTime;
            var guard = 0;
            while (CounterStock > 0 && WaitingCustomers > 0 && saleCooldown <= 0f && guard++ < 8)
            {
                CompleteSale();
                saleCooldown += SaleInterval;
            }
        }

        private void TickGoldenMinute(float deltaTime)
        {
            if (GoldenMinuteRemaining <= 0f)
            {
                return;
            }

            GoldenMinuteRemaining = Math.Max(0f, GoldenMinuteRemaining - deltaTime);
            if (GoldenMinuteRemaining <= 0f)
            {
                events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.GoldenMinuteEnded, SelectedRecipe));
            }
        }

        private void CompleteBatch()
        {
            var recipe = recipes[SelectedRecipe];
            StockRecipe = SelectedRecipe;
            CounterStock = Math.Min(CounterCapacity, CounterStock + recipe.BatchYield);
            saleCooldown = Math.Max(saleCooldown, FreshCounterDisplayTime);
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.BatchCompleted, recipe.Id, recipe.BatchYield));
        }

        private void CompleteSale()
        {
            var recipe = recipes[StockRecipe];
            var beforeTotal = TotalItemsSold;
            var beforeBread = CountryBreadSold;
            var beforeManager = ManagerUnlocked;

            CounterStock--;
            WaitingCustomers--;
            TotalItemsSold++;
            if (StockRecipe == RecipeId.CountryBread)
            {
                CountryBreadSold++;
            }

            var multiplier = GoldenMinuteRemaining > 0f ? 2 : 1;
            var revenue = recipe.SalePrice * multiplier;
            Coins += revenue;
            events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.SaleCompleted, StockRecipe, revenue));

            if (!beforeManager && ManagerUnlocked)
            {
                events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.ManagerUnlocked, StockRecipe));
            }

            foreach (var candidate in recipes.Values)
            {
                var wasUnlocked = IsRecipeUnlocked(candidate, beforeTotal, beforeBread, BakeryLevel, DiscoveryMask);
                var isUnlocked = IsRecipeUnlocked(candidate, TotalItemsSold, CountryBreadSold, BakeryLevel, DiscoveryMask);
                if (!wasUnlocked && isUnlocked)
                {
                    events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.RecipeUnlocked, candidate.Id));
                }
            }

            if (GoldenMinuteRemaining <= 0f)
            {
                Warmth++;
                if (Warmth >= WarmthGoal)
                {
                    Warmth = 0;
                    GoldenMinuteRemaining = GoldenMinuteDuration;
                    events.Enqueue(new BakeryLoopEvent(BakeryLoopEventType.GoldenMinuteStarted, StockRecipe));
                }
            }
        }

        private bool HasCounterRoomForSelectedBatch()
        {
            return CounterStock + recipes[SelectedRecipe].BatchYield <= CounterCapacity;
        }

        private void BeginPhase(BakeryWorkPhase phase, float duration)
        {
            Phase = phase;
            phaseDuration = Math.Max(0.01f, duration);
            PhaseRemaining = phaseDuration;
        }

        private void EnterWaitingPhase(BakeryWorkPhase phase)
        {
            Phase = phase;
            phaseDuration = 0f;
            PhaseRemaining = 0f;
        }

        private float NextCustomerDelay()
        {
            return 2.7f + TotalItemsSold % 4 * 0.48f;
        }

        private static bool IsRecipeUnlocked(
            BakeryRecipeSpec recipe,
            int totalItemsSold,
            int countryBreadSold,
            int bakeryLevel,
            int discoveryMask)
        {
            if (recipe.Id == RecipeId.CountryBread)
            {
                return true;
            }

            if (recipe.Id == RecipeId.KaiserRoll)
            {
                return countryBreadSold >= KaiserUnlockBreadSales;
            }

            var discoveryBit = DiscoveryBit(recipe.Id);
            if (discoveryBit != 0)
            {
                return (discoveryMask & discoveryBit) != 0;
            }

            return totalItemsSold >= recipe.UnlockAtSales && bakeryLevel >= recipe.RequiredBakeryLevel;
        }

        private static int DiscoveryBit(RecipeId recipeId)
        {
            return recipeId switch
            {
                RecipeId.ChocolateMuffin => 1 << 0,
                RecipeId.JamTurnover => 1 << 1,
                RecipeId.ChocolatePillow => 1 << 2,
                _ => 0
            };
        }

        private static bool IsActionPhase(BakeryWorkPhase phase)
        {
            return phase == BakeryWorkPhase.WaitingForDough
                || phase == BakeryWorkPhase.WaitingForOven
                || phase == BakeryWorkPhase.WaitingForCounter;
        }

        private static bool IsWaitingPhase(BakeryWorkPhase phase)
        {
            return IsActionPhase(phase);
        }

        private static bool IsTimedPhase(BakeryWorkPhase phase)
        {
            return phase == BakeryWorkPhase.FetchingDough
                || phase == BakeryWorkPhase.LoadingOven
                || phase == BakeryWorkPhase.Baking
                || phase == BakeryWorkPhase.Serving;
        }
    }
}
