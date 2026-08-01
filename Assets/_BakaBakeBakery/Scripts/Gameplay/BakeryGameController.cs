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
        Neighbour
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
        private BakeryHudController hud;
        private float conversationTimer = 0.85f;
        private float saveTimer;
        private int conversationIndex;
        private bool saveDirty;

        public bool IsReady { get; private set; }
        public BakerySnapshot CurrentSnapshot => loop?.Snapshot ?? default;
        public BakeryWorkerView WorkerView => worker;
        public BakeryWorldView WorldView => worldView;

        private void Start()
        {
            if (!TryCreateLoop(out loop))
            {
                return;
            }

            IsReady = true;
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
            loop.Tick(deltaTime);
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
            }
        }

        public bool RequestBakerAction()
        {
            if (!IsReady)
            {
                return false;
            }

            var accepted = loop.RequestAction();
            if (accepted)
            {
                worker?.Pulse();
                hud?.Render(loop.Snapshot);
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
                var progress = BuildSmokeProbe.IsSmokeTest || BuildSmokeProbe.IsVisualCapture
                    ? new BakeryProgressData()
                    : BakeryProgressStore.Load();
                createdLoop = new BakeryLoop(specs, progress);
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
                    case BakeryLoopEventType.BatchCompleted:
                        worker?.Pulse();
                        hud?.ShowDialogue(BakerySpeaker.Baker, "Fresh from the oven — easy now.", 2.6f);
                        break;
                    case BakeryLoopEventType.SaleCompleted:
                        saveDirty = true;
                        worldView?.CelebrateSale();
                        OnSaleCompleted(loopEvent);
                        break;
                    case BakeryLoopEventType.CustomerArrived:
                        if (loop.TotalItemsSold % 3 == 1)
                        {
                            hud?.ShowDialogue(BakerySpeaker.Neighbour, "Morning! Is that loaf spoken for?", 3f);
                        }

                        break;
                    case BakeryLoopEventType.ManagerUnlocked:
                        saveDirty = true;
                        hud?.ShowToast("MANAGER UNLOCKED", "Mila now keeps the baker moving between your clicks.");
                        hud?.ShowDialogue(BakerySpeaker.Baker, "Mila wrote the whole rhythm down. We can breathe now.", 4f);
                        break;
                    case BakeryLoopEventType.RecipeUnlocked:
                        saveDirty = true;
                        var recipe = loop.GetRecipe(loopEvent.RecipeId);
                        hud?.ShowToast("NEW RECIPE", recipe.DisplayName);
                        break;
                    case BakeryLoopEventType.SecondOvenPurchased:
                        hud?.ShowToast("SECOND OVEN READY", "Bakes are now 40% quicker and the counter holds more.");
                        hud?.ShowDialogue(BakerySpeaker.Grandmother, "Two ovens? This little place is growing up.", 4f);
                        break;
                    case BakeryLoopEventType.BakeryUpgraded:
                        hud?.ShowToast("BAKA-BAKE-BAKERY", "The neighbourhood helped raise a warm wooden home.");
                        hud?.ShowDialogue(BakerySpeaker.Neighbour, "That glowing sign belongs on this street.", 4f);
                        break;
                    case BakeryLoopEventType.GoldenMinuteStarted:
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
            if (hud == null)
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
            if (!saveDirty || BuildSmokeProbe.IsSmokeTest || BuildSmokeProbe.IsVisualCapture)
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
            if (!saveDirty || loop == null || BuildSmokeProbe.IsSmokeTest || BuildSmokeProbe.IsVisualCapture)
            {
                return;
            }

            BakeryProgressStore.Save(loop.ExportProgress());
            saveDirty = false;
            saveTimer = 0f;
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
