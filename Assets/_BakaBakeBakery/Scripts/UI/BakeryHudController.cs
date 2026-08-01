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
        private readonly Dictionary<VisualElement, IVisualElementScheduledItem> bubbleSchedules = new();

        private UIDocument document;
        private VisualElement root;
        private VisualElement ledger;
        private VisualElement actionProgressFill;
        private VisualElement milestoneProgressFill;
        private VisualElement warmthFill;
        private VisualElement goldenChip;
        private VisualElement toast;
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
        private Button ledgerButton;
        private Button actionButton;
        private Button secondOvenButton;
        private Button bakeryUpgradeButton;
        private BakeryGameController game;
        private IVisualElementScheduledItem toastSchedule;

        private void OnEnable()
        {
            document = GetComponent<UIDocument>();
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
            var controller = FindAnyObjectByType<BakeryGameController>();
            controller?.BindHud(this);
            actionButton?.schedule.Execute(actionButton.Focus).StartingIn(180);
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

            if (Keyboard.current.escapeKey.wasPressedThisFrame
                && ledger != null
                && !ledger.ClassListContains("ledger--closed"))
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

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.CountryBread);
            }
            else if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.KaiserRoll);
            }
            else if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.ButterCroissant);
            }
            else if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.CinnamonSwirl);
            }
            else if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.Finezja);
            }
            else if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                game?.TrySelectRecipe(RecipeId.CinnamonMonocle);
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
            RenderWorkCard(snapshot);
            RenderRecipeCards(snapshot);
            RenderLedger(snapshot);
        }

        public void ShowDialogue(BakerySpeaker speaker, string text, float duration)
        {
            var bubbleName = speaker switch
            {
                BakerySpeaker.Baker => "baker-bubble",
                BakerySpeaker.Grandmother => "grandmother-bubble",
                _ => "neighbour-bubble"
            };
            var labelName = speaker switch
            {
                BakerySpeaker.Baker => "baker-bubble-text",
                BakerySpeaker.Grandmother => "grandmother-bubble-text",
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
            toastSchedule = toast.schedule
                .Execute(() => toast.RemoveFromClassList("toast--visible"))
                .StartingIn(4200);
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
                if (current is Button || current is Toggle || current is Slider
                    || current.ClassListContains("blocks-world-input"))
                {
                    return true;
                }
            }

            return false;
        }

        private void QueryElements()
        {
            ledger = root.Q<VisualElement>("upgrade-ledger");
            actionProgressFill = root.Q<VisualElement>("action-progress-fill");
            milestoneProgressFill = root.Q<VisualElement>("milestone-progress-fill");
            warmthFill = root.Q<VisualElement>("warmth-fill");
            goldenChip = root.Q<VisualElement>("golden-chip");
            toast = root.Q<VisualElement>("toast");
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
            ledgerButton = root.Q<Button>("ledger-button");
            actionButton = root.Q<Button>("action-button");
            secondOvenButton = root.Q<Button>("second-oven-button");
            bakeryUpgradeButton = root.Q<Button>("bakery-upgrade-button");

            recipeCards.Clear();
            AddRecipeCard("recipe-bread", RecipeId.CountryBread);
            AddRecipeCard("recipe-kaiser", RecipeId.KaiserRoll);
            AddRecipeCard("recipe-croissant", RecipeId.ButterCroissant);
            AddRecipeCard("recipe-swirl", RecipeId.CinnamonSwirl);
            AddRecipeCard("recipe-finezja", RecipeId.Finezja);
            AddRecipeCard("recipe-monocle", RecipeId.CinnamonMonocle);
        }

        private void RegisterCallbacks()
        {
            foreach (var card in recipeCards.Keys)
            {
                card.RegisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            if (ledgerButton != null)
            {
                ledgerButton.clicked += ToggleLedger;
            }

            if (actionButton != null)
            {
                actionButton.clicked += OnActionClicked;
            }

            if (secondOvenButton != null)
            {
                secondOvenButton.clicked += OnSecondOvenClicked;
            }

            if (bakeryUpgradeButton != null)
            {
                bakeryUpgradeButton.clicked += OnBakeryUpgradeClicked;
            }
        }

        private void UnregisterCallbacks()
        {
            foreach (var card in recipeCards.Keys)
            {
                card.UnregisterCallback<ClickEvent>(OnRecipeCardClicked);
            }

            if (ledgerButton != null)
            {
                ledgerButton.clicked -= ToggleLedger;
            }

            if (actionButton != null)
            {
                actionButton.clicked -= OnActionClicked;
            }

            if (secondOvenButton != null)
            {
                secondOvenButton.clicked -= OnSecondOvenClicked;
            }

            if (bakeryUpgradeButton != null)
            {
                bakeryUpgradeButton.clicked -= OnBakeryUpgradeClicked;
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
                current = snapshot.CountryBreadSold - BakeryLoop.ManagerUnlockBreadSales;
                target = BakeryLoop.KaiserUnlockBreadSales - BakeryLoop.ManagerUnlockBreadSales;
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
                current = 1;
                target = 1;
            }

            SetWidth(milestoneProgressFill, target <= 0 ? 1f : Mathf.Clamp01((float)current / target));
        }

        private void RenderWarmth(BakerySnapshot snapshot)
        {
            SetText(warmthValue, $"{snapshot.Warmth} / {snapshot.WarmthGoal}");
            SetWidth(warmthFill, (float)snapshot.Warmth / snapshot.WarmthGoal);
            if (goldenChip != null)
            {
                goldenChip.EnableInClassList("golden-chip--visible", snapshot.GoldenMinuteActive);
            }

            SetText(goldenValue, snapshot.GoldenMinuteActive
                ? $"2× COINS · {Mathf.CeilToInt(snapshot.GoldenMinuteRemaining)} s"
                : "KINDNESS FILLS THE STREET");
        }

        private void RenderWorkCard(BakerySnapshot snapshot)
        {
            SetText(shiftMode, snapshot.ManagerUnlocked ? "MILA · MANAGER ON DUTY" : "MANUAL MORNING · LOAF BY LOAF");
            var title = string.Empty;
            var detail = string.Empty;
            var button = string.Empty;
            var progress = snapshot.PhaseProgress;

            switch (snapshot.Phase)
            {
                case BakeryWorkPhase.WaitingForDough:
                    title = snapshot.CounterStock >= snapshot.CounterCapacity ? "The counter is beautifully full" : "Fetch fresh dough";
                    detail = snapshot.CounterStock >= snapshot.CounterCapacity
                        ? "A neighbour is already walking over."
                        : snapshot.ManagerUnlocked ? "Mila will send Jules — or lend a hand now." : "Click Jules in the truck or press Space.";
                    button = snapshot.CounterStock >= snapshot.CounterCapacity ? "WAIT FOR A CUSTOMER" : "FETCH DOUGH";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.FetchingDough:
                    title = "Crossing to the refrigerator";
                    detail = "A cool tray, a warm pair of hands.";
                    button = "JULES IS MOVING";
                    break;
                case BakeryWorkPhase.WaitingForOven:
                    title = "The dough is ready";
                    detail = snapshot.ManagerUnlocked ? "Mila has the next step — or click to help." : "Click Jules again to load the oven.";
                    button = "LOAD THE OVEN";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.LoadingOven:
                    title = "Into the amber glow";
                    detail = "The first warmth reaches the crust.";
                    button = "LOADING OVEN";
                    break;
                case BakeryWorkPhase.Baking:
                    var recipeName = game != null
                        ? game.GetRecipe(snapshot.SelectedRecipe).DisplayName
                        : "today's batch";
                    title = $"Baking {recipeName}";
                    detail = $"{snapshot.PhaseRemaining:0.0} seconds · watch the oven light breathe";
                    button = "BAKING";
                    break;
                case BakeryWorkPhase.WaitingForCounter:
                    title = "The bake is ready";
                    detail = snapshot.ManagerUnlocked ? "Mila will collect it — your click is always welcome." : "One last click: bring it to the counter.";
                    button = "LIFT THE BAKE";
                    progress = 0f;
                    break;
                case BakeryWorkPhase.Serving:
                    title = "Setting out something warm";
                    detail = "Customers buy from the counter in arrival order.";
                    button = "TO THE COUNTER";
                    break;
            }

            SetText(actionTitle, title);
            SetText(actionDetail, detail);
            if (actionButton != null)
            {
                actionButton.text = button;
                actionButton.SetEnabled(snapshot.CanRequestAction);
            }

            SetWidth(actionProgressFill, progress);
        }

        private void RenderRecipeCards(BakerySnapshot snapshot)
        {
            foreach (var pair in recipeCards)
            {
                var unlocked = game != null && game.IsRecipeUnlocked(pair.Value);
                pair.Key.EnableInClassList("recipe-card--locked", !unlocked);
                pair.Key.EnableInClassList("recipe-card--selected", snapshot.SelectedRecipe == pair.Value);
                pair.Key.SetEnabled(unlocked);
            }
        }

        private void RenderLedger(BakerySnapshot snapshot)
        {
            if (secondOvenButton != null)
            {
                secondOvenButton.text = snapshot.SecondOvenPurchased
                    ? "SECOND OVEN · INSTALLED"
                    : snapshot.KaiserUnlocked
                        ? $"BUY SECOND OVEN · {BakeryLoop.SecondOvenCost} COINS"
                        : $"SECOND OVEN · {snapshot.CountryBreadSold} / {BakeryLoop.KaiserUnlockBreadSales} BREADS";
                secondOvenButton.SetEnabled(
                    !snapshot.SecondOvenPurchased
                    && snapshot.KaiserUnlocked
                    && snapshot.Coins >= BakeryLoop.SecondOvenCost);
            }

            if (bakeryUpgradeButton != null)
            {
                bakeryUpgradeButton.text = snapshot.BakeryLevel >= 2
                    ? "BAKA-BAKE-BAKERY · HOME"
                    : snapshot.BakeryUpgradeAvailable
                        ? $"BUILD WOODEN BAKERY · {BakeryLoop.BakeryUpgradeCost} COINS"
                        : $"WOODEN BAKERY · {snapshot.TotalItemsSold} / {BakeryLoop.BakeryUpgradeSales} SALES";
                bakeryUpgradeButton.SetEnabled(
                    snapshot.BakeryUpgradeAvailable
                    && snapshot.Coins >= BakeryLoop.BakeryUpgradeCost);
            }
        }

        private void AddRecipeCard(string elementName, RecipeId recipeId)
        {
            var button = root.Q<Button>(elementName);
            if (button != null)
            {
                recipeCards[button] = recipeId;
            }
        }

        private void OnRecipeCardClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button selected
                && recipeCards.TryGetValue(selected, out var recipeId))
            {
                game?.TrySelectRecipe(recipeId);
            }
        }

        private void OnActionClicked()
        {
            game?.RequestBakerAction();
        }

        private void OnSecondOvenClicked()
        {
            game?.TryPurchaseSecondOven();
        }

        private void OnBakeryUpgradeClicked()
        {
            game?.TryPurchaseBakeryUpgrade();
        }

        private void ToggleLedger()
        {
            ledger?.ToggleInClassList("ledger--closed");
        }

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

        private static void SetText(Label label, string value)
        {
            if (label != null && label.text != value)
            {
                label.text = value;
            }
        }

        private static void SetWidth(VisualElement element, float normalized)
        {
            if (element != null)
            {
                element.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
            }
        }
    }
}
