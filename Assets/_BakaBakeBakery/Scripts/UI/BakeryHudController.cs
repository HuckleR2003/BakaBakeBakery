using System;
using System.Collections.Generic;
using BakaBakeBakery.Data;
using BakaBakeBakery.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BakaBakeBakery.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class BakeryHudController : MonoBehaviour
    {
        [SerializeField] private StyleSheet styleSheet;

        private readonly Dictionary<Button, RecipeId> recipeCards = new();
        private readonly Dictionary<VisualElement, IngredientId> ingredientCards = new();
        private readonly Dictionary<VisualElement, IVisualElementScheduledItem> bubbleSchedules = new();
        private readonly List<IngredientId> craftingSlots = new();
        private readonly List<Button> recipeSlotButtons = new();
        private readonly List<Button> craftSlotButtons = new();

        private VisualElement root;
        private VisualElement ledger;
        private VisualElement homePanel;
        private VisualElement craftPanel;
        private VisualElement homeContent;
        private VisualElement nextUnlock;
        private VisualElement recipePlaceholder;
        private VisualElement marketMap;
        private VisualElement mapRouteFill;
        private VisualElement bikeMarker;
        private VisualElement dragGhost;
        private VisualElement actionProgressFill;
        private VisualElement milestoneProgressFill;
        private VisualElement warmthFill;
        private VisualElement goldenChip;
        private VisualElement toast;
        private VisualElement tutorialCard;
        private Label tutorialDaySummary;
        private Label coinValue;
        private Label salesValue;
        private Label counterValue;
        private Label customerValue;
        private Label milestoneTitle;
        private Label milestoneValue;
        private Label shiftMode;
        private Label actionTitle;
        private Label actionDetail;
        private Label warmthValue;
        private Label goldenValue;
        private Label bakeryLevel;
        private Label toastTitle;
        private Label toastCopy;
        private Label nextUnlockTitle;
        private Label nextUnlockCopy;
        private Label ledgerDay;
        private Label dragGhostLabel;
        private Label tutorialTitle;
        private Label tutorialCopy;
        private Label tutorialObjective;
        private Label tutorialProgress;
        private Label craftGuideHint;
        private Label craftGeneralHint;
        private Button ledgerButton;
        private Button dayButton;
        private Button actionButton;
        private Button secondOvenButton;
        private Button bakeryUpgradeButton;
        private Button homeTab;
        private Button craftTab;
        private Button clearCraftButton;
        private Button craftResult;
        private Button guideButton;
        private Button tutorialPrimaryButton;
        private Button tutorialSecondaryButton;
        private Button tutorialRibbon;
        private BakeryGameController game;
        private IVisualElementScheduledItem toastSchedule;
        private IngredientId? draggedIngredient;

        private void OnEnable()
        {
            var document = GetComponent<UIDocument>();
            root = document.rootVisualElement;
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            QueryElements();
            RegisterCallbacks();
        }

        private void Start()
        {
            FindAnyObjectByType<BakeryGameController>()?.BindHud(this);
        }

        private void OnDisable()
        {
            UnregisterCallbacks();
            bubbleSchedules.Clear();
            toastSchedule = null;
            game = null;
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseLedger();
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                ToggleLedger();
            }

            if (Keyboard.current.spaceKey.wasPressedThisFrame && CanUseSpaceForBaker())
            {
                game?.RequestBakerAction();
            }
        }

        public void Bind(BakeryGameController controller)
        {
            game = controller;
            if (game != null && game.IsReady)
            {
                Render(game.CurrentSnapshot);
            }
        }

        public void Render(BakerySnapshot snapshot)
        {
            if (root == null)
            {
                return;
            }

            SetText(coinValue, snapshot.Coins.ToString("N0"));
            SetText(salesValue, snapshot.TotalItemsSold.ToString("N0"));
            SetText(counterValue, $"{snapshot.CounterStock} / {snapshot.CounterCapacity}");
            SetText(customerValue, snapshot.WaitingCustomers == 1 ? "1 neighbour" : $"{snapshot.WaitingCustomers} neighbours");
            SetText(bakeryLevel, snapshot.BakeryLevel == 1 ? "FOOD TRUCK · LEVEL 1" : "WOODEN BAKERY · LEVEL 2");

            RenderMilestone(snapshot);
            RenderWarmth(snapshot);
            RenderDay();
            RenderWorkCard(snapshot);
            RenderProductJourney(snapshot);
            RenderCrafting();
            RenderLedger(snapshot);
        }

        public void ShowDialogue(BakerySpeaker speaker, string text, float duration)
        {
            var bubbleName = speaker switch
            {
                BakerySpeaker.Baker => "baker-bubble",
                BakerySpeaker.Grandmother => "grandmother-bubble",
                BakerySpeaker.Friend => "friend-bubble",
                _ => "neighbour-bubble"
            };
            var labelName = speaker switch
            {
                BakerySpeaker.Baker => "baker-bubble-text",
                BakerySpeaker.Grandmother => "grandmother-bubble-text",
                BakerySpeaker.Friend => "friend-bubble-text",
                _ => "neighbour-bubble-text"
            };

            var bubble = root?.Q<VisualElement>(bubbleName);
            var label = root?.Q<Label>(labelName);
            if (bubble == null || label == null)
            {
                return;
            }

            label.text = text;
            bubble.AddToClassList("speech-bubble--visible");
            if (bubbleSchedules.TryGetValue(bubble, out var previous))
            {
                previous.Pause();
            }

            bubbleSchedules[bubble] = bubble.schedule
                .Execute(() => bubble.RemoveFromClassList("speech-bubble--visible"))
                .StartingIn(Mathf.RoundToInt(Mathf.Max(1.2f, duration) * 1000f));
        }

        public void ShowTutorial(BakeryTutorialBeat beat, BakeryDayCycle day)
        {
            if (tutorialCard == null)
            {
                return;
            }

            CloseLedger();
            tutorialCard.parent?.BringToFront();
            root?.Q<VisualElement>("friend-bubble")?.RemoveFromClassList("speech-bubble--visible");
            SetText(tutorialTitle, beat.Title);
            SetText(tutorialCopy, beat.Copy);
            SetText(tutorialObjective, beat.Objective);
            SetText(tutorialProgress, $"MORNING NOTES · {beat.DisplayStage} / {BakeryTutorial.FinalStep}");
            if (tutorialPrimaryButton != null) tutorialPrimaryButton.text = beat.PrimaryReply.ToUpperInvariant();
            if (tutorialSecondaryButton != null) tutorialSecondaryButton.text = beat.SecondaryReply.ToUpperInvariant();

            var showSummary = beat.Step == BakeryTutorialStep.DaySummary && day != null;
            if (tutorialDaySummary != null)
            {
                tutorialDaySummary.style.display = showSummary ? DisplayStyle.Flex : DisplayStyle.None;
                if (showSummary)
                {
                    tutorialDaySummary.text = $"MARKET -{day.DailyCosts}  ·  SALES {day.DailyItemsSold} / +{day.DailyRevenue}  ·  PROFIT {day.DailyProfit:+#;-#;0}";
                }
            }

            tutorialCard.AddToClassList("tutorial-card--visible");
            tutorialRibbon?.RemoveFromClassList("tutorial-ribbon--visible");
            ApplyTutorialTarget(beat.Target);
            tutorialPrimaryButton?.Focus();
        }

        public void MinimizeTutorial(BakeryTutorialBeat beat, BakeryTutorialTarget? targetOverride = null)
        {
            tutorialCard?.parent?.BringToFront();
            tutorialCard?.RemoveFromClassList("tutorial-card--visible");
            if (tutorialRibbon != null)
            {
                tutorialRibbon.text = $"MILA · {beat.Objective}   [OPEN]";
                tutorialRibbon.AddToClassList("tutorial-ribbon--visible");
            }

            ApplyTutorialTarget(targetOverride ?? beat.Target);
        }

        public void HideTutorial()
        {
            tutorialCard?.RemoveFromClassList("tutorial-card--visible");
            tutorialRibbon?.RemoveFromClassList("tutorial-ribbon--visible");
            ApplyTutorialTarget(BakeryTutorialTarget.None);
        }

        public void OpenCraftingForTutorial()
        {
            ShowCraft();
            ApplyTutorialTarget(BakeryTutorialTarget.CraftResult);
        }

        public void ShowToast(string title, string copy)
        {
            if (toast == null)
            {
                return;
            }

            SetText(toastTitle, title);
            SetText(toastCopy, copy);
            toast.AddToClassList("toast--visible");
            toastSchedule?.Pause();
            toastSchedule = toast.schedule.Execute(() => toast.RemoveFromClassList("toast--visible")).StartingIn(4200);
        }

        public bool IsPointerOverInteractiveUi(Vector2 screenPosition)
        {
            if (root?.panel == null || Screen.height <= 0)
            {
                return false;
            }

            var topLeftPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            var panelPosition = RuntimePanelUtils.ScreenToPanel(root.panel, topLeftPosition);
            var picked = root.panel.Pick(panelPosition);
            for (var current = picked; current != null; current = current.parent)
            {
                if (current is Button || current is Toggle || current is Slider || current.ClassListContains("blocks-world-input"))
                {
                    return true;
                }
            }

            return false;
        }

        private void QueryElements()
        {
            ledger = root.Q<VisualElement>("upgrade-ledger");
            homePanel = root.Q<VisualElement>("home-panel");
            craftPanel = root.Q<VisualElement>("craft-panel");
            homeContent = root.Q<VisualElement>("home-content");
            nextUnlock = root.Q<VisualElement>("next-unlock");
            recipePlaceholder = root.Q<VisualElement>("recipe-placeholder");
            marketMap = root.Q<VisualElement>("market-map");
            mapRouteFill = root.Q<VisualElement>("map-route-fill");
            bikeMarker = root.Q<VisualElement>("bike-marker");
            dragGhost = root.Q<VisualElement>("drag-ghost");
            actionProgressFill = root.Q<VisualElement>("action-progress-fill");
            milestoneProgressFill = root.Q<VisualElement>("milestone-progress-fill");
            warmthFill = root.Q<VisualElement>("warmth-fill");
            goldenChip = root.Q<VisualElement>("golden-chip");
            toast = root.Q<VisualElement>("toast");
            tutorialCard = root.Q<VisualElement>("tutorial-card");
            tutorialDaySummary = root.Q<Label>("tutorial-day-summary");
            coinValue = root.Q<Label>("coin-value");
            salesValue = root.Q<Label>("sales-value");
            counterValue = root.Q<Label>("counter-value");
            customerValue = root.Q<Label>("customer-value");
            milestoneTitle = root.Q<Label>("milestone-title");
            milestoneValue = root.Q<Label>("milestone-value");
            shiftMode = root.Q<Label>("shift-mode");
            actionTitle = root.Q<Label>("action-title");
            actionDetail = root.Q<Label>("action-detail");
            warmthValue = root.Q<Label>("warmth-value");
            goldenValue = root.Q<Label>("golden-value");
            bakeryLevel = root.Q<Label>("bakery-level");
            toastTitle = root.Q<Label>("toast-title");
            toastCopy = root.Q<Label>("toast-copy");
            nextUnlockTitle = root.Q<Label>("next-unlock-title");
            nextUnlockCopy = root.Q<Label>("next-unlock-copy");
            ledgerDay = root.Q<Label>("ledger-day");
            dragGhostLabel = root.Q<Label>("drag-ghost-label");
            tutorialTitle = root.Q<Label>("tutorial-title");
            tutorialCopy = root.Q<Label>("tutorial-copy");
            tutorialObjective = root.Q<Label>("tutorial-objective");
            tutorialProgress = root.Q<Label>("tutorial-progress");
            craftGuideHint = root.Q<Label>("craft-guide-hint");
            craftGeneralHint = root.Q<Label>("craft-general-hint");
            ledgerButton = root.Q<Button>("ledger-button");
            dayButton = root.Q<Button>("day-button");
            actionButton = root.Q<Button>("action-button");
            secondOvenButton = root.Q<Button>("second-oven-button");
            bakeryUpgradeButton = root.Q<Button>("bakery-upgrade-button");
            homeTab = root.Q<Button>("home-tab");
            craftTab = root.Q<Button>("craft-tab");
            clearCraftButton = root.Q<Button>("clear-craft-button");
            craftResult = root.Q<Button>("craft-result");
            guideButton = root.Q<Button>("guide-button");
            tutorialPrimaryButton = root.Q<Button>("tutorial-primary-button");
            tutorialSecondaryButton = root.Q<Button>("tutorial-secondary-button");
            tutorialRibbon = root.Q<Button>("tutorial-ribbon");

            recipeSlotButtons.Clear();
            craftSlotButtons.Clear();
            for (var index = 0; index < 9; index++)
            {
                var button = root.Q<Button>($"recipe-slot-{index}");
                if (button != null)
                {
                    recipeSlotButtons.Add(button);
                }
            }

            for (var index = 0; index < BakeryCraftingSystem.MaximumIngredients; index++)
            {
                var button = root.Q<Button>($"craft-slot-{index}");
                if (button != null)
                {
                    craftSlotButtons.Add(button);
                }
            }

            ingredientCards.Clear();
            AddIngredientCard("ingredient-flour", IngredientId.Flour);
            AddIngredientCard("ingredient-milk", IngredientId.Milk);
            AddIngredientCard("ingredient-pastry", IngredientId.PuffPastry);
            AddIngredientCard("ingredient-jam", IngredientId.Jam);
            AddIngredientCard("ingredient-chocolate", IngredientId.Chocolate);
        }

        private void RegisterCallbacks()
        {
            foreach (var card in recipeSlotButtons)
            {
                card.RegisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            foreach (var card in craftSlotButtons)
            {
                card.RegisterCallback<ClickEvent>(OnCraftSlotClicked);
            }

            foreach (var card in ingredientCards.Keys)
            {
                card.RegisterCallback<PointerDownEvent>(OnIngredientPointerDown);
                card.RegisterCallback<PointerMoveEvent>(OnIngredientPointerMove);
                card.RegisterCallback<PointerUpEvent>(OnIngredientPointerUp);
                card.RegisterCallback<PointerCaptureOutEvent>(OnIngredientPointerCaptureOut);
            }

            if (ledgerButton != null) ledgerButton.clicked += ToggleLedger;
            if (dayButton != null) dayButton.clicked += OnDayButtonClicked;
            if (actionButton != null) actionButton.clicked += OnActionClicked;
            if (secondOvenButton != null) secondOvenButton.clicked += OnSecondOvenClicked;
            if (bakeryUpgradeButton != null) bakeryUpgradeButton.clicked += OnBakeryUpgradeClicked;
            if (homeTab != null) homeTab.clicked += ShowHome;
            if (craftTab != null) craftTab.clicked += ShowCraft;
            if (clearCraftButton != null) clearCraftButton.clicked += ClearCrafting;
            if (craftResult != null) craftResult.clicked += OnCraftResultClicked;
            if (guideButton != null) guideButton.clicked += OnGuideClicked;
            if (tutorialPrimaryButton != null) tutorialPrimaryButton.clicked += OnTutorialPrimaryClicked;
            if (tutorialSecondaryButton != null) tutorialSecondaryButton.clicked += OnTutorialSecondaryClicked;
            if (tutorialRibbon != null) tutorialRibbon.clicked += OnGuideClicked;
        }

        private void UnregisterCallbacks()
        {
            foreach (var card in recipeSlotButtons)
            {
                card.UnregisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            foreach (var card in craftSlotButtons)
            {
                card.UnregisterCallback<ClickEvent>(OnCraftSlotClicked);
            }

            foreach (var card in ingredientCards.Keys)
            {
                card.UnregisterCallback<PointerDownEvent>(OnIngredientPointerDown);
                card.UnregisterCallback<PointerMoveEvent>(OnIngredientPointerMove);
                card.UnregisterCallback<PointerUpEvent>(OnIngredientPointerUp);
                card.UnregisterCallback<PointerCaptureOutEvent>(OnIngredientPointerCaptureOut);
            }

            if (ledgerButton != null) ledgerButton.clicked -= ToggleLedger;
            if (dayButton != null) dayButton.clicked -= OnDayButtonClicked;
            if (actionButton != null) actionButton.clicked -= OnActionClicked;
            if (secondOvenButton != null) secondOvenButton.clicked -= OnSecondOvenClicked;
            if (bakeryUpgradeButton != null) bakeryUpgradeButton.clicked -= OnBakeryUpgradeClicked;
            if (homeTab != null) homeTab.clicked -= ShowHome;
            if (craftTab != null) craftTab.clicked -= ShowCraft;
            if (clearCraftButton != null) clearCraftButton.clicked -= ClearCrafting;
            if (craftResult != null) craftResult.clicked -= OnCraftResultClicked;
            if (guideButton != null) guideButton.clicked -= OnGuideClicked;
            if (tutorialPrimaryButton != null) tutorialPrimaryButton.clicked -= OnTutorialPrimaryClicked;
            if (tutorialSecondaryButton != null) tutorialSecondaryButton.clicked -= OnTutorialSecondaryClicked;
            if (tutorialRibbon != null) tutorialRibbon.clicked -= OnGuideClicked;
        }

        private void RenderDay()
        {
            if (game?.DayCycle == null)
            {
                return;
            }

            var day = game.DayCycle;
            SetText(ledgerDay, $"MORNING {day.DayNumber:00}");
            marketMap.EnableInClassList("market-map--visible", day.Phase == BakeryDayPhase.TravellingToMarket);
            SetWidth(mapRouteFill, day.TravelProgress);
            if (bikeMarker != null)
            {
                bikeMarker.style.left = Length.Percent(Mathf.Lerp(0f, 91f, day.TravelProgress));
            }

            switch (day.Phase)
            {
                case BakeryDayPhase.MorningPreparation:
                    dayButton.text = $"MORNING {day.DayNumber:00}\nGO TO MARKET";
                    dayButton.SetEnabled(true);
                    break;
                case BakeryDayPhase.TravellingToMarket:
                    dayButton.text = $"ON THE ROAD\n{day.RemainingSeconds:0.0} S";
                    dayButton.SetEnabled(false);
                    break;
                case BakeryDayPhase.ReadyToOpen:
                    dayButton.text = $"MORNING {day.DayNumber:00}\nSTART DAY!";
                    dayButton.SetEnabled(true);
                    break;
                case BakeryDayPhase.Open:
                    dayButton.text = $"DAY {day.DayNumber:00} · {FormatTime(day.RemainingSeconds)}\nPROFIT {day.DailyProfit:+#;-#;0} · END EARLY";
                    dayButton.SetEnabled(true);
                    break;
                case BakeryDayPhase.DaySummary:
                    dayButton.text = $"DAY {day.DayNumber:00} CLOSED\nPROFIT {day.DailyProfit:+#;-#;0} · NEXT MORNING";
                    dayButton.SetEnabled(true);
                    break;
            }
        }

        private void RenderMilestone(BakerySnapshot snapshot)
        {
            int current;
            int target;
            if (!snapshot.ManagerUnlocked)
            {
                SetText(milestoneTitle, "A HELPING HAND");
                SetText(milestoneValue, $"Manager · {snapshot.CountryBreadSold} / {BakeryLoop.ManagerUnlockBreadSales} breads");
                current = snapshot.CountryBreadSold;
                target = BakeryLoop.ManagerUnlockBreadSales;
            }
            else if (!snapshot.KaiserUnlocked)
            {
                SetText(milestoneTitle, "A BIGGER MORNING");
                SetText(milestoneValue, $"Kaiser rolls · {snapshot.CountryBreadSold} / {BakeryLoop.KaiserUnlockBreadSales} breads");
                current = snapshot.CountryBreadSold;
                target = BakeryLoop.KaiserUnlockBreadSales;
            }
            else if (snapshot.BakeryLevel == 1)
            {
                SetText(milestoneTitle, "A PLACE TO CALL HOME");
                SetText(milestoneValue, $"Wooden bakery · {snapshot.TotalItemsSold} / {BakeryLoop.BakeryUpgradeSales} sales");
                current = snapshot.TotalItemsSold;
                target = BakeryLoop.BakeryUpgradeSales;
            }
            else
            {
                SetText(milestoneTitle, "THE STREET KNOWS OUR NAME");
                SetText(milestoneValue, "New recipes arrive with every warm morning");
                current = target = 1;
            }

            SetWidth(milestoneProgressFill, target <= 0 ? 1f : (float)current / target);
        }

        private void RenderWarmth(BakerySnapshot snapshot)
        {
            SetText(warmthValue, $"{snapshot.Warmth} / {snapshot.WarmthGoal}");
            SetWidth(warmthFill, (float)snapshot.Warmth / snapshot.WarmthGoal);
            goldenChip.EnableInClassList("golden-chip--visible", snapshot.GoldenMinuteActive);
            SetText(goldenValue, snapshot.GoldenMinuteActive
                ? $"2x COINS · {Mathf.CeilToInt(snapshot.GoldenMinuteRemaining)} s"
                : "KINDNESS FILLS THE STREET");
        }

        private void RenderWorkCard(BakerySnapshot snapshot)
        {
            if (game?.DayCycle != null && game.DayCycle.Phase != BakeryDayPhase.Open)
            {
                SetText(shiftMode, game.DayCycle.Phase == BakeryDayPhase.DaySummary ? "SHIFT COMPLETE · SHUTTER DOWN" : "MORNING PREPARATION · PANTRY FIRST");
                SetText(actionTitle, game.DayCycle.Phase == BakeryDayPhase.ReadyToOpen ? "Everything is ready" : "The bakery is resting");
                SetText(actionDetail, game.DayCycle.Phase == BakeryDayPhase.MorningPreparation
                    ? "Take the bicycle to the market before warming the ovens."
                    : "Use the wooden day sign above to continue.");
                actionButton.text = "BAKERY CLOSED";
                actionButton.SetEnabled(false);
                SetWidth(actionProgressFill, game.DayCycle.Phase == BakeryDayPhase.TravellingToMarket ? game.DayCycle.TravelProgress : 0f);
                return;
            }

            SetText(shiftMode, snapshot.ManagerUnlocked ? "MILA · MANAGER ON DUTY" : "MANUAL MORNING · LOAF BY LOAF");
            var title = string.Empty;
            var detail = string.Empty;
            var button = string.Empty;
            var progress = snapshot.PhaseProgress;

            switch (snapshot.Phase)
            {
                case BakeryWorkPhase.WaitingForDough:
                    title = snapshot.CounterStock >= snapshot.CounterCapacity ? "The counter is beautifully full" : "Prepare a fresh batch";
                    detail = snapshot.CounterStock >= snapshot.CounterCapacity ? "A neighbour is already walking over." : "Click Jules or press Space: pantry, prep board, then oven.";
                    button = snapshot.CounterStock >= snapshot.CounterCapacity ? "WAIT FOR A CUSTOMER" : "GATHER INGREDIENTS";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.FetchingDough:
                    title = snapshot.PhaseProgress < 0.4f ? "Gathering chilled ingredients" : "Working the fresh dough";
                    detail = "Jules crosses the whole kitchen; nothing appears by magic.";
                    button = "JULES IS MOVING";
                    break;
                case BakeryWorkPhase.WaitingForOven:
                    title = "The raw batch is ready";
                    detail = "Click Jules again to load the glowing oven.";
                    button = "LOAD THE OVEN";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.LoadingOven:
                    title = "Into the amber glow";
                    detail = "The first warmth reaches the crust.";
                    button = "LOADING OVEN";
                    break;
                case BakeryWorkPhase.Baking:
                    title = $"Baking {game.GetRecipe(snapshot.SelectedRecipe).DisplayName}";
                    detail = $"{snapshot.PhaseRemaining:0.0} seconds · watch the oven breathe";
                    button = "BAKING";
                    break;
                case BakeryWorkPhase.WaitingForCounter:
                    title = "The bake is ready";
                    detail = "One last walk: carry it from oven to the empty counter.";
                    button = "LIFT THE BAKE";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.Serving:
                    title = "Setting out something warm";
                    detail = "The display fills only when Jules reaches the counter.";
                    button = "TO THE COUNTER";
                    break;
            }

            SetText(actionTitle, title);
            SetText(actionDetail, detail);
            actionButton.text = button;
            actionButton.SetEnabled(snapshot.CanRequestAction);
            SetWidth(actionProgressFill, progress);
        }

        private void RenderProductJourney(BakerySnapshot snapshot)
        {
            if (game == null)
            {
                return;
            }

            recipeCards.Clear();
            var visibleIndex = 0;
            RecipeId? next = null;
            foreach (var recipeId in game.RecipeDisplayOrder)
            {
                if (game.IsRecipeUnlocked(recipeId))
                {
                    if (visibleIndex < recipeSlotButtons.Count)
                    {
                        var button = recipeSlotButtons[visibleIndex++];
                        var recipe = game.GetRecipe(recipeId);
                        button.text = $"{recipe.DisplayName.ToUpperInvariant()}\n{recipe.BatchYield} per batch · {recipe.SalePrice} coins";
                        EnsureRecipeIcon(button, recipeId);
                        button.AddToClassList("recipe-card--visible");
                        button.EnableInClassList("recipe-card--selected", snapshot.SelectedRecipe == recipeId);
                        button.SetEnabled(true);
                        recipeCards[button] = recipeId;
                    }
                }
                else if (!next.HasValue)
                {
                    next = recipeId;
                }
            }

            for (var index = visibleIndex; index < recipeSlotButtons.Count; index++)
            {
                recipeSlotButtons[index].RemoveFromClassList("recipe-card--visible");
                recipeSlotButtons[index].RemoveFromClassList("recipe-card--selected");
            }

            homeContent.EnableInClassList("home-content--mature", visibleIndex >= 3);
            if (recipePlaceholder != null)
            {
                recipePlaceholder.style.display = visibleIndex < 3 ? DisplayStyle.Flex : DisplayStyle.None;
            }
            nextUnlock.style.display = next.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            if (next.HasValue)
            {
                RenderNextUnlock(next.Value, snapshot);
            }
        }

        private void RenderNextUnlock(RecipeId recipeId, BakerySnapshot snapshot)
        {
            var recipe = game.GetRecipe(recipeId);
            SetText(nextUnlockTitle, recipe.DisplayName);
            var progress = 0f;
            string copy;
            switch (recipeId)
            {
                case RecipeId.KaiserRoll:
                    copy = $"{snapshot.CountryBreadSold} / {BakeryLoop.KaiserUnlockBreadSales} country breads";
                    progress = (float)snapshot.CountryBreadSold / BakeryLoop.KaiserUnlockBreadSales;
                    break;
                case RecipeId.ButterCroissant:
                case RecipeId.CinnamonSwirl:
                case RecipeId.Finezja:
                case RecipeId.CinnamonMonocle:
                    copy = snapshot.BakeryLevel < recipe.RequiredBakeryLevel
                        ? $"Bakery level {recipe.RequiredBakeryLevel} · {snapshot.TotalItemsSold} / {recipe.UnlockAtSales} sales"
                        : $"{snapshot.TotalItemsSold} / {recipe.UnlockAtSales} neighbourhood sales";
                    progress = recipe.UnlockAtSales <= 0 ? 0f : (float)snapshot.TotalItemsSold / recipe.UnlockAtSales;
                    break;
                default:
                    copy = "Hidden in Mila's Crafting tab · experiment with 2–4 ingredients";
                    progress = 0f;
                    break;
            }

            SetText(nextUnlockCopy, copy);
            SetWidth(root.Q<VisualElement>("next-unlock-fill"), progress);
        }

        private void RenderCrafting()
        {
            if (game == null)
            {
                return;
            }

            for (var index = 0; index < craftSlotButtons.Count; index++)
            {
                var filled = index < craftingSlots.Count;
                craftSlotButtons[index].text = filled ? BakeryCraftingSystem.IngredientName(craftingSlots[index]).ToUpperInvariant() : "DROP";
                craftSlotButtons[index].EnableInClassList("craft-slot--filled", filled);
            }

            foreach (var pair in ingredientCards)
            {
                var count = game.IngredientCount(pair.Value);
                if (pair.Key is Button button)
                {
                    button.text = $"{BakeryCraftingSystem.IngredientName(pair.Value).ToUpperInvariant()}\n{count}";
                    button.SetEnabled(count > ReservedCount(pair.Value));
                }
            }

            var hasRecipe = BakeryCraftingSystem.TryFindRecipe(craftingSlots, out var preview);
            craftResult.text = hasRecipe ? $"{preview.DisplayName.ToUpperInvariant()}\nDISCOVER" : "???\nDISCOVER";
            craftResult.SetEnabled(hasRecipe);
        }

        private void RenderLedger(BakerySnapshot snapshot)
        {
            secondOvenButton.text = snapshot.SecondOvenPurchased
                ? "SECOND OVEN · INSTALLED"
                : snapshot.KaiserUnlocked
                    ? $"BUY SECOND OVEN · {BakeryLoop.SecondOvenCost} COINS"
                    : $"SECOND OVEN · {snapshot.CountryBreadSold} / {BakeryLoop.KaiserUnlockBreadSales} BREADS";
            secondOvenButton.SetEnabled(!snapshot.SecondOvenPurchased && snapshot.KaiserUnlocked && snapshot.Coins >= BakeryLoop.SecondOvenCost);

            bakeryUpgradeButton.text = snapshot.BakeryLevel >= 2
                ? "BAKA-BAKE-BAKERY · HOME"
                : snapshot.BakeryUpgradeAvailable
                    ? $"BUILD WOODEN BAKERY · {BakeryLoop.BakeryUpgradeCost} COINS"
                    : $"WOODEN BAKERY · {snapshot.TotalItemsSold} / {BakeryLoop.BakeryUpgradeSales} SALES";
            bakeryUpgradeButton.SetEnabled(snapshot.BakeryUpgradeAvailable && snapshot.Coins >= BakeryLoop.BakeryUpgradeCost);
        }

        private void AddIngredientCard(string name, IngredientId ingredient)
        {
            var card = root.Q<VisualElement>(name);
            if (card != null)
            {
                ingredientCards[card] = ingredient;
            }
        }

        private static void EnsureRecipeIcon(Button button, RecipeId recipeId)
        {
            if (button.userData is RecipeId current && current == recipeId && button.Q<VisualElement>("product-icon") != null)
            {
                return;
            }

            button.Q<VisualElement>("product-icon")?.RemoveFromHierarchy();
            var icon = new VisualElement { name = "product-icon" };
            icon.AddToClassList("product-icon");
            icon.AddToClassList($"product-icon--{recipeId.ToString().ToLowerInvariant()}");
            for (var index = 0; index < 3; index++)
            {
                var detail = new VisualElement();
                detail.AddToClassList("product-icon-detail");
                detail.AddToClassList($"product-icon-detail--{index}");
                icon.Add(detail);
            }

            button.Insert(0, icon);
            button.userData = recipeId;
        }

        private void OnRecipeCardClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button button && recipeCards.TryGetValue(button, out var recipe))
            {
                game?.TrySelectRecipe(recipe);
            }
        }

        private void OnCraftSlotClicked(ClickEvent evt)
        {
            if (evt.currentTarget is not Button button)
            {
                return;
            }

            var index = craftSlotButtons.IndexOf(button);
            if (index >= 0 && index < craftingSlots.Count)
            {
                craftingSlots.RemoveAt(index);
                RenderCrafting();
            }
        }

        private void OnIngredientPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || evt.currentTarget is not VisualElement source || !ingredientCards.TryGetValue(source, out var ingredient))
            {
                return;
            }

            if (game == null || game.IngredientCount(ingredient) <= ReservedCount(ingredient) || craftingSlots.Count >= BakeryCraftingSystem.MaximumIngredients)
            {
                return;
            }

            draggedIngredient = ingredient;
            source.CapturePointer(evt.pointerId);
            dragGhost.AddToClassList("drag-ghost--visible");
            SetText(dragGhostLabel, BakeryCraftingSystem.IngredientName(ingredient).ToUpperInvariant());
            MoveDragGhost(evt.position);
            evt.StopPropagation();
        }

        private void OnIngredientPointerMove(PointerMoveEvent evt)
        {
            if (draggedIngredient.HasValue)
            {
                MoveDragGhost(evt.position);
                evt.StopPropagation();
            }
        }

        private void OnIngredientPointerUp(PointerUpEvent evt)
        {
            if (!draggedIngredient.HasValue)
            {
                return;
            }

            var pointer = new Vector2(evt.position.x, evt.position.y);
            var targetIndex = -1;
            for (var index = 0; index < craftSlotButtons.Count; index++)
            {
                if (craftSlotButtons[index].worldBound.Contains(pointer))
                {
                    targetIndex = index;
                    break;
                }
            }

            if (targetIndex < 0 && craftPanel.worldBound.Contains(pointer))
            {
                targetIndex = craftingSlots.Count;
            }

            if (targetIndex >= 0 && targetIndex <= craftingSlots.Count && craftingSlots.Count < BakeryCraftingSystem.MaximumIngredients)
            {
                craftingSlots.Insert(targetIndex, draggedIngredient.Value);
            }

            if (evt.currentTarget is VisualElement source && source.HasPointerCapture(evt.pointerId))
            {
                source.ReleasePointer(evt.pointerId);
            }

            EndDrag();
            RenderCrafting();
            evt.StopPropagation();
        }

        private void OnIngredientPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            EndDrag();
        }

        private void MoveDragGhost(Vector3 position)
        {
            dragGhost.style.left = position.x - 54f;
            dragGhost.style.top = position.y - 24f;
        }

        private void EndDrag()
        {
            draggedIngredient = null;
            dragGhost?.RemoveFromClassList("drag-ghost--visible");
        }

        private int ReservedCount(IngredientId ingredient)
        {
            var count = 0;
            foreach (var item in craftingSlots)
            {
                if (item == ingredient) count++;
            }

            return count;
        }

        private void OnCraftResultClicked()
        {
            if (game?.TryCraft(craftingSlots) == true)
            {
                craftingSlots.Clear();
                ShowHome();
            }
        }

        private void ClearCrafting()
        {
            craftingSlots.Clear();
            RenderCrafting();
        }

        private void OnDayButtonClicked()
        {
            if (game?.DayCycle == null) return;
            switch (game.DayCycle.Phase)
            {
                case BakeryDayPhase.MorningPreparation: game.GoToMarket(); break;
                case BakeryDayPhase.ReadyToOpen: game.StartDay(); break;
                case BakeryDayPhase.Open: game.EndDay(); break;
                case BakeryDayPhase.DaySummary: game.BeginNextMorning(); break;
            }
        }

        private void OnActionClicked() => game?.RequestBakerAction();
        private void OnSecondOvenClicked() => game?.TryPurchaseSecondOven();
        private void OnBakeryUpgradeClicked() => game?.TryPurchaseBakeryUpgrade();
        private void OnGuideClicked() => game?.OpenTutorialGuide();
        private void OnTutorialPrimaryClicked() => game?.ChooseTutorialReply(true);
        private void OnTutorialSecondaryClicked() => game?.ChooseTutorialReply(false);

        private void ShowHome()
        {
            homePanel.RemoveFromClassList("dock-panel--hidden");
            craftPanel.AddToClassList("dock-panel--hidden");
            homeTab.AddToClassList("nav-button--active");
            craftTab.RemoveFromClassList("nav-button--active");
        }

        private void ShowCraft()
        {
            if (game != null && !game.CanOpenCrafting)
            {
                ShowToast("FIRST LOAF FIRST", "Mila will open the test kitchen after one warm bake reaches the counter.");
                game.OpenTutorialGuide();
                return;
            }

            craftPanel.RemoveFromClassList("dock-panel--hidden");
            homePanel.AddToClassList("dock-panel--hidden");
            craftTab.AddToClassList("nav-button--active");
            homeTab.RemoveFromClassList("nav-button--active");
            RenderCrafting();
            if (craftGuideHint != null)
            {
                var showGuideClue = game != null
                    && game.TutorialStep == (int)BakeryTutorialStep.DiscoverRecipe
                    && !game.IsRecipeUnlocked(RecipeId.ChocolateMuffin);
                craftGuideHint.style.display = showGuideClue ? DisplayStyle.Flex : DisplayStyle.None;
                if (craftGeneralHint != null)
                {
                    craftGeneralHint.style.display = showGuideClue ? DisplayStyle.None : DisplayStyle.Flex;
                }
            }
        }

        private void ApplyTutorialTarget(BakeryTutorialTarget target)
        {
            dayButton?.RemoveFromClassList("tutorial-target");
            actionButton?.RemoveFromClassList("tutorial-target");
            craftTab?.RemoveFromClassList("tutorial-target");
            craftResult?.RemoveFromClassList("tutorial-target");

            var element = target switch
            {
                BakeryTutorialTarget.DayBoard => dayButton,
                BakeryTutorialTarget.BakerAction => actionButton,
                BakeryTutorialTarget.CraftTab => craftTab,
                BakeryTutorialTarget.CraftResult => craftResult,
                _ => null
            };
            element?.AddToClassList("tutorial-target");
        }

        private void ToggleLedger() => ledger?.ToggleInClassList("ledger--closed");

        private void CloseLedger()
        {
            ledger?.AddToClassList("ledger--closed");
            ledgerButton?.Focus();
        }

        private bool CanUseSpaceForBaker()
        {
            var focused = root?.focusController?.focusedElement;
            return focused == null || focused == actionButton || focused is not Button;
        }

        private static string FormatTime(float seconds)
        {
            var safe = Mathf.Max(0, Mathf.CeilToInt(seconds));
            return $"{safe / 60:00}:{safe % 60:00}";
        }

        private static void SetText(Label label, string value)
        {
            if (label != null && label.text != value) label.text = value;
        }

        private static void SetWidth(VisualElement element, float normalized)
        {
            if (element != null) element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
        }
    }
}
