using System;
using System.Collections.Generic;
using BakaBakeBakery.Core;
using BakaBakeBakery.Data;
using BakaBakeBakery.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BakaBakeBakery.Gameplay
{
    public enum BakerySpeaker
    {
        Baker,
        Grandmother,
        Neighbour,
        Friend
    }

    [DisallowMultipleComponent]
    public sealed class BakeryGameController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private BakeryCatalog catalog;

        [Header("Interaction")]
        [SerializeField] private BakeryWorkerView worker;
        [SerializeField] private BakeryWorldView worldView;
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private Collider bakerHitTarget;

        [Header("Bakery upgrades")]
        [SerializeField] private GameObject lockedOvenBay;
        [SerializeField] private GameObject secondOvenVisual;
        [SerializeField] private GameObject cabinUpgradeVisual;
        [SerializeField] private GameObject goldenMinuteLight;

        [Header("Counter products")]
        [SerializeField] private GameObject countryBreadDisplay;
        [SerializeField] private GameObject kaiserRollDisplay;
        [SerializeField] private GameObject croissantDisplay;
        [SerializeField] private GameObject cinnamonSwirlDisplay;
        [SerializeField] private GameObject finezjaDisplay;
        [SerializeField] private GameObject cinnamonMonocleDisplay;

        private static readonly (BakerySpeaker Speaker, string Text)[] NeighbourhoodLines =
        {
            (BakerySpeaker.Grandmother, "Hey darling, maybe some fresh bread?"),
            (BakerySpeaker.Baker, "The oven is warm. Yours is next, Mrs. Rose."),
            (BakerySpeaker.Neighbour, "It smells like Sunday from halfway down the street."),
            (BakerySpeaker.Baker, "Stay a moment. The crust is just beginning to sing."),
            (BakerySpeaker.Grandmother, "No rush, dear. Good bread keeps its own time."),
            (BakerySpeaker.Neighbour, "Save me a Kaiser roll when they reach the menu.")
        };

        private BakeryLoop loop;
        private BakeryDayCycle dayCycle;
        private BakeryCraftingSystem crafting;
        private BakeryHudController hud;
        private float conversationTimer = 0.85f;
        private float saveTimer;
        private int conversationIndex;
        private bool saveDirty;
        private int tutorialStep;
        private BakeryDayPhase previousDayPhase;

        private static readonly RecipeId[] DisplayOrder =
        {
            RecipeId.CountryBread,
            RecipeId.KaiserRoll,
            RecipeId.ButterCroissant,
            RecipeId.CinnamonSwirl,
            RecipeId.Finezja,
            RecipeId.CinnamonMonocle,
            RecipeId.ChocolateMuffin,
            RecipeId.JamTurnover,
            RecipeId.ChocolatePillow,
            RecipeId.PastelDeNata,
            RecipeId.RaspberryMacaron,
            RecipeId.HoneyBaklava,
            RecipeId.ChocolateCannoli,
            RecipeId.FudgeBrownie
        };

        public bool IsReady { get; private set; }
        public BakerySnapshot CurrentSnapshot => loop?.Snapshot ?? default;
        public BakeryWorkerView WorkerView => worker;
        public BakeryWorldView WorldView => worldView;
        public BakeryDayCycle DayCycle => dayCycle;
        public IReadOnlyList<RecipeId> RecipeDisplayOrder => DisplayOrder;
        public int TutorialStep => tutorialStep;
        public bool CanOpenCrafting => IsReady
            && (dayCycle.DayNumber > 1 || tutorialStep >= (int)BakeryTutorialStep.DiscoverRecipe);

        private void Start()
        {
            if (!TryCreateLoop(out loop))
            {
                return;
            }

            IsReady = true;
            previousDayPhase = dayCycle.Phase;
            if (BuildSmokeProbe.IsSmokeTest || BuildSmokeProbe.IsVisualCapture)
            {
                dayCycle.ForceOpenForTesting();
                previousDayPhase = dayCycle.Phase;
            }
            worker?.Initialize(loop.Snapshot);
            worldView?.Initialize(loop.Snapshot);
            ApplyWorldState(loop.Snapshot);
            hud?.Bind(this);
        }

        private void Update()
        {
            if (!IsReady)
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            dayCycle.Tick(deltaTime);
            if (dayCycle.Phase == BakeryDayPhase.TravellingToMarket || dayCycle.Phase == BakeryDayPhase.Open)
            {
                saveDirty = true;
            }
            if (dayCycle.Phase == BakeryDayPhase.Open)
            {
                loop.Tick(deltaTime);
            }

            HandleDayPhaseChange();
            worker?.Render(loop.Snapshot, deltaTime);
            HandleLoopEvents();
            HandleWorldPointer();
            TickConversation(deltaTime);
            TickSave(deltaTime);
            ApplyWorldState(loop.Snapshot);
            worldView?.Render(loop.Snapshot, deltaTime);
            hud?.Render(loop.Snapshot);
        }

        private void OnDisable()
        {
            SaveIfNeeded();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                SaveIfNeeded();
            }
        }

        public void BindHud(BakeryHudController controller)
        {
            hud = controller;
            if (IsReady)
            {
                hud.Bind(this);
                hud.Render(loop.Snapshot);
                ShowCurrentTutorial();
            }
        }

        public void OpenTutorialGuide()
        {
            if (IsReady)
            {
                ShowCurrentTutorial(true);
            }
        }

        public void ChooseTutorialReply(bool primary)
        {
            if (!IsReady)
            {
                return;
            }

            if (!primary)
            {
                MinimizeCurrentTutorial();
                return;
            }

            switch (BakeryTutorial.Normalize(tutorialStep))
            {
                case BakeryTutorialStep.Welcome:
                    AdvanceTutorial(BakeryTutorialStep.VisitMarket);
                    break;
                case BakeryTutorialStep.VisitMarket:
                    if (!GoToMarket()) ShowCurrentTutorial(true);
                    break;
                case BakeryTutorialStep.OnTheRoad:
                    MinimizeCurrentTutorial();
                    break;
                case BakeryTutorialStep.OpenBakery:
                    if (!StartDay()) ShowCurrentTutorial(true);
                    break;
                case BakeryTutorialStep.FirstLoaf:
                    RequestBakerAction();
                    MinimizeCurrentTutorial();
                    break;
                case BakeryTutorialStep.DiscoverRecipe:
                    hud?.OpenCraftingForTutorial();
                    MinimizeCurrentTutorial(BakeryTutorialTarget.CraftResult);
                    break;
                case BakeryTutorialStep.CloseFirstDay:
                    MinimizeCurrentTutorial();
                    break;
                case BakeryTutorialStep.DaySummary:
                    if (!BeginNextMorning()) ShowCurrentTutorial(true);
                    break;
                default:
                    hud?.HideTutorial();
                    break;
            }
        }

        public bool RequestBakerAction()
        {
            if (!IsReady)
            {
                return false;
            }

            if (dayCycle.Phase != BakeryDayPhase.Open)
            {
                hud?.ShowToast("BAKERY IS CLOSED", "Prepare the morning basket, then open the wooden sign.");
                return false;
            }

            var accepted = loop.RequestAction();
            if (accepted)
            {
                worker?.Pulse();
                hud?.Render(loop.Snapshot);
                if (BakeryTutorial.Normalize(tutorialStep) == BakeryTutorialStep.FirstLoaf)
                {
                    MinimizeCurrentTutorial();
                }
            }
            else if (loop.Snapshot.Phase == BakeryWorkPhase.WaitingForDough
                && loop.Snapshot.CounterStock >= loop.Snapshot.CounterCapacity)
            {
                hud?.ShowToast("COUNTER FULL", "A neighbour is already on the way.");
            }

            return accepted;
        }

        public bool TrySelectRecipe(RecipeId recipeId)
        {
            if (!IsReady)
            {
                return false;
            }

            if (dayCycle.DayNumber == 1 && recipeId != RecipeId.CountryBread)
            {
                hud?.ShowToast("TOMORROW'S SURPRISE", "Today the neighbours meet us through one honest country loaf.");
                return false;
            }

            var accepted = loop.TrySelectRecipe(recipeId);
            if (!accepted)
            {
                hud?.ShowToast(
                    "RECIPE WAITS",
                    loop.Snapshot.CounterStock > 0
                        ? "Sell the fresh pastries already on the counter first."
                        : "Finish the current loaf or reach its milestone.");
                return false;
            }

            saveDirty = true;
            hud?.ShowToast("RECIPE SELECTED", loop.GetRecipe(recipeId).DisplayName);
            return true;
        }

        public bool TryPurchaseSecondOven()
        {
            if (!IsReady || !loop.TryPurchaseSecondOven())
            {
                hud?.ShowToast(
                    "SECOND OVEN",
                    !loop.KaiserUnlocked
                        ? $"Unlocks after {BakeryLoop.KaiserUnlockBreadSales} country breads."
                        : $"Needs {BakeryLoop.SecondOvenCost} coins.");
                return false;
            }

            saveDirty = true;
            HandleLoopEvents();
            return true;
        }

        public bool TryPurchaseBakeryUpgrade()
        {
            if (!IsReady || !loop.TryPurchaseBakeryUpgrade())
            {
                hud?.ShowToast(
                    "WOODEN BAKERY",
                    !loop.BakeryUpgradeAvailable
                        ? $"The neighbourhood offers help after {BakeryLoop.BakeryUpgradeSales} sales."
                        : $"Needs {BakeryLoop.BakeryUpgradeCost} coins.");
                return false;
            }

            saveDirty = true;
            HandleLoopEvents();
            return true;
        }

        public bool IsRecipeUnlocked(RecipeId recipeId)
        {
            return IsReady && loop.IsRecipeUnlocked(recipeId);
        }

        public BakeryRecipeSpec GetRecipe(RecipeId recipeId)
        {
            return IsReady ? loop.GetRecipe(recipeId) : default;
        }

        public int IngredientCount(IngredientId ingredient)
        {
            return crafting?.Count(ingredient) ?? 0;
        }

        public bool GoToMarket()
        {
            if (!IsReady)
            {
                return false;
            }

            if (!dayCycle.BeginMarketTrip(out var cost))
            {
                hud?.ShowToast("MORNING MARKET", "The bicycle is already on its way.");
                return false;
            }

            if (cost > 0)
            {
                loop.TrySpendCoins(cost);
            }

            crafting.AddMorningBasket();
            AdvanceTutorial(BakeryTutorialStep.OnTheRoad);
            saveDirty = true;
            return true;
        }

        public bool StartDay()
        {
            if (!IsReady || !dayCycle.StartDay())
            {
                return false;
            }

            AdvanceTutorial(BakeryTutorialStep.FirstLoaf);
            saveDirty = true;
            if (dayCycle.DayNumber > 1)
            {
                hud?.ShowDialogue(BakerySpeaker.Friend, "Roleta w górę. Zobaczmy, czym dziś pachnie sąsiedztwo.", 5f);
            }
            return true;
        }

        public bool EndDay()
        {
            if (!IsReady || (dayCycle.DayNumber == 1 && loop.TotalItemsSold == 0))
            {
                hud?.ShowToast("ONE WARM LOAF", "Let the first neighbour leave with bread before closing.");
                return false;
            }

            if (loop.Phase != BakeryWorkPhase.WaitingForDough)
            {
                hud?.ShowToast("FINISH THIS BATCH", "Mila will close as soon as Jules returns to the resting station.");
                return false;
            }

            if (!dayCycle.EndDayEarly())
            {
                return false;
            }

            loop.CloseShift();
            AdvanceTutorial(BakeryTutorialStep.DaySummary);
            saveDirty = true;
            return true;
        }

        public bool BeginNextMorning()
        {
            if (!IsReady || !dayCycle.BeginNextMorning())
            {
                return false;
            }

            AdvanceTutorial(BakeryTutorialStep.Complete);
            saveDirty = true;
            return true;
        }

        public bool TryCraft(IReadOnlyList<IngredientId> ingredients)
        {
            if (!CanOpenCrafting)
            {
                hud?.ShowToast("FIRST LOAF FIRST", "Let Jules finish one honest country loaf before experimenting.");
                return false;
            }

            if (!crafting.TryCraft(ingredients, out var result))
            {
                hud?.ShowToast("NOT QUITE YET", "Try another combination of two to four fresh ingredients.");
                return false;
            }

            loop.UnlockCraftedRecipe(result.Result);
            BakeryAudio.Play(BakerySound.Discovery);
            AdvanceTutorial(BakeryTutorialStep.CloseFirstDay);
            saveDirty = true;
            HandleLoopEvents();
            hud?.ShowToast("RECIPE DISCOVERED", $"{result.DisplayName} is written into tomorrow's bakery book.");
            return true;
        }

        private bool TryCreateLoop(out BakeryLoop createdLoop)
        {
            createdLoop = null;
            if (catalog == null || catalog.Recipes == null || catalog.Recipes.Count == 0)
            {
                Debug.LogError("[Baka Bake Bakery] Gameplay cannot start without a recipe catalog.", this);
                hud?.ShowToast("BAKERY CLOSED", "The recipe book could not be opened.");
                return false;
            }

            var specs = new List<BakeryRecipeSpec>(catalog.Recipes.Count);
            foreach (var recipe in catalog.Recipes)
            {
                if (recipe == null)
                {
                    continue;
                }

                specs.Add(new BakeryRecipeSpec(
                    recipe.Id,
                    recipe.DisplayName,
                    recipe.BakeSeconds + recipe.PreparationSeconds + recipe.FinishingSeconds,
                    recipe.BatchYield,
                    recipe.SalePrice,
                    recipe.UnlockAtSales,
                    recipe.RequiredBakeryLevel));
            }

            try
            {
                var progress = BuildSmokeProbe.IsAutomatedRun
                    ? BakeryProgressStore.CreateNewGame()
                    : BakeryProgressStore.Load();
                createdLoop = new BakeryLoop(specs, progress);
                dayCycle = new BakeryDayCycle(progress);
                crafting = new BakeryCraftingSystem(progress);
                tutorialStep = (int)BakeryTutorial.Normalize(progress.tutorialStep);
                SyncTutorialWithWorld();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                hud?.ShowToast("BAKERY CLOSED", "The opening shift could not be prepared safely.");
                return false;
            }
        }

        private void HandleLoopEvents()
        {
            while (loop.TryDequeueEvent(out var loopEvent))
            {
                switch (loopEvent.Type)
                {
                    case BakeryLoopEventType.WorkStarted:
                        BakeryAudio.Play(
                            loop.Snapshot.Phase == BakeryWorkPhase.LoadingOven
                                ? BakerySound.OvenDoor
                                : BakerySound.Knead);
                        break;
                    case BakeryLoopEventType.BatchCompleted:
                        worker?.Pulse();
                        BakeryAudio.Play(BakerySound.BakeReady);
                        AdvanceTutorial(BakeryTutorialStep.DiscoverRecipe);
                        hud?.ShowDialogue(BakerySpeaker.Baker, "Fresh from the oven — easy now.", 2.6f);
                        break;
                    case BakeryLoopEventType.SaleCompleted:
                        saveDirty = true;
                        BakeryAudio.Play(BakerySound.Coin);
                        dayCycle.RecordRevenue(loopEvent.Amount);
                        worldView?.CelebrateSale();
                        OnSaleCompleted(loopEvent);
                        break;
                    case BakeryLoopEventType.CustomerArrived:
                        BakeryAudio.Play(BakerySound.ShopBell, 0.7f);
                        if (loop.TotalItemsSold % 3 == 1)
                        {
                            hud?.ShowDialogue(BakerySpeaker.Neighbour, "Morning! Is that loaf spoken for?", 3f);
                        }

                        break;
                    case BakeryLoopEventType.ManagerUnlocked:
                        saveDirty = true;
                        BakeryAudio.Play(BakerySound.Discovery);
                        hud?.ShowToast("MANAGER UNLOCKED", "Mila now keeps the baker moving between your clicks.");
                        hud?.ShowDialogue(BakerySpeaker.Baker, "Mila wrote the whole rhythm down. We can breathe now.", 4f);
                        break;
                    case BakeryLoopEventType.RecipeUnlocked:
                        saveDirty = true;
                        BakeryAudio.Play(BakerySound.Discovery);
                        var recipe = loop.GetRecipe(loopEvent.RecipeId);
                        hud?.ShowToast("NEW RECIPE", recipe.DisplayName);
                        break;
                    case BakeryLoopEventType.SecondOvenPurchased:
                        BakeryAudio.Play(BakerySound.Discovery);
                        hud?.ShowToast("SECOND OVEN READY", "Bakes are now 40% quicker and the counter holds more.");
                        hud?.ShowDialogue(BakerySpeaker.Grandmother, "Two ovens? This little place is growing up.", 4f);
                        break;
                    case BakeryLoopEventType.BakeryUpgraded:
                        BakeryAudio.Play(BakerySound.DayBell);
                        hud?.ShowToast("BAKA-BAKE-BAKERY", "The neighbourhood helped raise a warm wooden home.");
                        hud?.ShowDialogue(BakerySpeaker.Neighbour, "That glowing sign belongs on this street.", 4f);
                        break;
                    case BakeryLoopEventType.GoldenMinuteStarted:
                        BakeryAudio.Play(BakerySound.Discovery);
                        hud?.ShowToast("GOLDEN MINUTE", "Neighbourhood warmth doubles every sale for 18 seconds.");
                        break;
                    case BakeryLoopEventType.GoldenMinuteEnded:
                        hud?.ShowToast("A GENTLE LULL", "The street settles back into its cosy rhythm.");
                        break;
                }
            }
        }

        private void OnSaleCompleted(BakeryLoopEvent loopEvent)
        {
            if (loop.TotalItemsSold == 1)
            {
                hud?.ShowDialogue(BakerySpeaker.Grandmother, "Listen to that crust. Perfect, darling.", 4f);
            }
            else if (loop.TotalItemsSold == 5)
            {
                hud?.ShowDialogue(BakerySpeaker.Baker, "Five warm parcels, five warmer mornings.", 3.6f);
            }
            else if (loop.TotalItemsSold % 7 == 0)
            {
                hud?.ShowDialogue(BakerySpeaker.Neighbour, $"Keep the change — that was worth all {loopEvent.Amount} coins.", 3.4f);
            }
        }

        private void TickConversation(float deltaTime)
        {
            if (hud == null || dayCycle.Phase != BakeryDayPhase.Open)
            {
                return;
            }

            conversationTimer -= deltaTime;
            if (conversationTimer > 0f)
            {
                return;
            }

            var line = NeighbourhoodLines[conversationIndex % NeighbourhoodLines.Length];
            hud.ShowDialogue(line.Speaker, line.Text, 3.8f);
            conversationIndex++;
            conversationTimer = 11.5f + conversationIndex % 3 * 2.2f;
        }

        private void HandleWorldPointer()
        {
            if (interactionCamera == null || bakerHitTarget == null || worker == null)
            {
                return;
            }

            Vector2 pointerPosition;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                pointerPosition = Mouse.current.position.ReadValue();
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                pointerPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            }
            else
            {
                return;
            }

            if (hud != null && hud.IsPointerOverInteractiveUi(pointerPosition))
            {
                return;
            }

            var ray = interactionCamera.ScreenPointToRay(pointerPosition);
            if (Physics.Raycast(ray, out var hit, 80f)
                && (hit.collider == bakerHitTarget || hit.collider.transform.IsChildOf(worker.transform)))
            {
                RequestBakerAction();
            }
        }

        private void ApplyWorldState(BakerySnapshot snapshot)
        {
            SetActive(lockedOvenBay, !snapshot.SecondOvenPurchased);
            SetActive(secondOvenVisual, snapshot.SecondOvenPurchased);
            SetActive(cabinUpgradeVisual, snapshot.BakeryLevel >= 2);
            SetActive(goldenMinuteLight, snapshot.GoldenMinuteActive);

            var showProduct = snapshot.CounterStock > 0;
            if (worldView == null)
            {
                SetActive(countryBreadDisplay, showProduct && snapshot.StockRecipe == RecipeId.CountryBread);
                SetActive(kaiserRollDisplay, showProduct && snapshot.StockRecipe == RecipeId.KaiserRoll);
                SetActive(croissantDisplay, showProduct && snapshot.StockRecipe == RecipeId.ButterCroissant);
                SetActive(cinnamonSwirlDisplay, showProduct && snapshot.StockRecipe == RecipeId.CinnamonSwirl);
                SetActive(finezjaDisplay, showProduct && snapshot.StockRecipe == RecipeId.Finezja);
                SetActive(cinnamonMonocleDisplay, showProduct && snapshot.StockRecipe == RecipeId.CinnamonMonocle);
            }
        }

        private void TickSave(float deltaTime)
        {
            if (!saveDirty || BuildSmokeProbe.IsAutomatedRun)
            {
                return;
            }

            saveTimer += deltaTime;
            if (saveTimer >= 2f)
            {
                SaveIfNeeded();
            }
        }

        private void SaveIfNeeded()
        {
            if (!saveDirty || loop == null || BuildSmokeProbe.IsAutomatedRun)
            {
                return;
            }

            var progress = loop.ExportProgress();
            dayCycle?.ExportInto(progress);
            crafting?.ExportInto(progress);
            progress.tutorialStep = (int)BakeryTutorial.Normalize(tutorialStep);
            BakeryProgressStore.Save(progress);
            saveDirty = false;
            saveTimer = 0f;
        }

        private void HandleDayPhaseChange()
        {
            if (dayCycle.Phase == previousDayPhase)
            {
                return;
            }

            var prior = previousDayPhase;
            previousDayPhase = dayCycle.Phase;
            saveDirty = true;
            BakeryAudio.SetShiftOpen(dayCycle.Phase == BakeryDayPhase.Open);
            if (dayCycle.Phase == BakeryDayPhase.Open || dayCycle.Phase == BakeryDayPhase.DaySummary)
            {
                BakeryAudio.Play(BakerySound.DayBell, 0.8f);
            }

            if (prior == BakeryDayPhase.TravellingToMarket && dayCycle.Phase == BakeryDayPhase.ReadyToOpen)
            {
                hud?.ShowToast("PANTRY RESTOCKED", "Flour, milk, pastry, jam and chocolate are safely home.");
                AdvanceTutorial(BakeryTutorialStep.OpenBakery);
            }
            else if (prior == BakeryDayPhase.Open && dayCycle.Phase == BakeryDayPhase.DaySummary)
            {
                loop.CloseShift();
                AdvanceTutorial(BakeryTutorialStep.DaySummary);
                hud?.ShowToast("SHIFT COMPLETE", $"Day {dayCycle.DayNumber} closed with {dayCycle.DailyProfit:+#;-#;0} profit.");
            }
        }

        private void SyncTutorialWithWorld()
        {
            if (dayCycle.DayNumber > 1)
            {
                tutorialStep = BakeryTutorial.FinalStep;
                return;
            }

            var minimumStep = dayCycle.Phase switch
            {
                BakeryDayPhase.TravellingToMarket => BakeryTutorialStep.OnTheRoad,
                BakeryDayPhase.ReadyToOpen => BakeryTutorialStep.OpenBakery,
                BakeryDayPhase.Open => BakeryTutorialStep.FirstLoaf,
                BakeryDayPhase.DaySummary => BakeryTutorialStep.DaySummary,
                _ => BakeryTutorialStep.Welcome
            };
            tutorialStep = Math.Max(tutorialStep, (int)minimumStep);
        }

        private bool AdvanceTutorial(BakeryTutorialStep nextStep)
        {
            var safeNext = (int)BakeryTutorial.Normalize((int)nextStep);
            if (safeNext <= tutorialStep)
            {
                return false;
            }

            tutorialStep = safeNext;
            saveDirty = true;
            ShowCurrentTutorial(true);
            return true;
        }

        private void ShowCurrentTutorial(bool forceComplete = false)
        {
            if (hud == null)
            {
                return;
            }

            var step = BakeryTutorial.Normalize(tutorialStep);
            if (step == BakeryTutorialStep.Complete && !forceComplete)
            {
                hud.HideTutorial();
                return;
            }

            hud.ShowTutorial(BakeryTutorial.GetBeat(step), dayCycle);
        }

        private void MinimizeCurrentTutorial(BakeryTutorialTarget? targetOverride = null)
        {
            var step = BakeryTutorial.Normalize(tutorialStep);
            if (step == BakeryTutorialStep.Complete)
            {
                hud?.HideTutorial();
                return;
            }

            hud?.MinimizeTutorial(BakeryTutorial.GetBeat(step), targetOverride);
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }
    }
}
