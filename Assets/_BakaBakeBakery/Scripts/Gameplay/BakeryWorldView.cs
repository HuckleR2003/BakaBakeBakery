using System;
using BakaBakeBakery.Data;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    public enum BakeryIngredientStage
    {
        MiseEnPlace,
        CarriedRaw,
        InOvenRaw,
        InOvenBaked,
        CarriedBaked,
        OnCounter
    }

    [DisallowMultipleComponent]
    public sealed class BakeryWorldView : MonoBehaviour
    {
        [Header("Production story")]
        [SerializeField] private GameObject[] ingredientDisplays;
        [SerializeField] private GameObject[] ovenRawDisplays;
        [SerializeField] private GameObject[] ovenBakedDisplays;
        [SerializeField] private BakeryCounterDisplay[] counterDisplays;

        [Header("Working fixtures")]
        [SerializeField] private Transform fridgeDoor;
        [SerializeField] private Transform ovenDoor;
        [SerializeField] private OvenGlowPulse ovenGlow;
        [SerializeField] private Transform[] steamPuffs;
        [SerializeField] private Transform hangingBell;

        [Header("Neighbourhood")]
        [SerializeField] private BakeryCustomerActor[] customers;

        private Quaternion fridgeDoorClosed;
        private Quaternion ovenDoorClosed;
        private Quaternion hangingBellBase;
        private Vector3[] steamBasePositions;
        private Vector3[] steamBaseScales;
        private bool initialized;

        public BakeryIngredientStage IngredientStage { get; private set; }
        public int VisibleCounterItems { get; private set; }
        public bool RawIngredientsVisible => IngredientStage == BakeryIngredientStage.MiseEnPlace;
        public bool OvenContentsVisible => IngredientStage == BakeryIngredientStage.InOvenRaw
            || IngredientStage == BakeryIngredientStage.InOvenBaked;

        public void Initialize(BakerySnapshot snapshot)
        {
            if (initialized)
            {
                return;
            }

            ingredientDisplays ??= Array.Empty<GameObject>();
            ovenRawDisplays ??= Array.Empty<GameObject>();
            ovenBakedDisplays ??= Array.Empty<GameObject>();
            counterDisplays ??= Array.Empty<BakeryCounterDisplay>();
            steamPuffs ??= Array.Empty<Transform>();
            customers ??= Array.Empty<BakeryCustomerActor>();

            fridgeDoorClosed = fridgeDoor != null ? fridgeDoor.localRotation : Quaternion.identity;
            ovenDoorClosed = ovenDoor != null ? ovenDoor.localRotation : Quaternion.identity;
            hangingBellBase = hangingBell != null ? hangingBell.localRotation : Quaternion.identity;
            steamBasePositions = new Vector3[steamPuffs.Length];
            steamBaseScales = new Vector3[steamPuffs.Length];
            for (var index = 0; index < steamPuffs.Length; index++)
            {
                if (steamPuffs[index] == null)
                {
                    continue;
                }

                steamBasePositions[index] = steamPuffs[index].localPosition;
                steamBaseScales[index] = steamPuffs[index].localScale;
                steamPuffs[index].gameObject.SetActive(false);
            }

            foreach (var display in counterDisplays)
            {
                display?.Initialize();
            }

            foreach (var customer in customers)
            {
                customer?.Initialize();
            }

            initialized = true;
            Render(snapshot, 0f);
        }

        public void Render(BakerySnapshot snapshot, float deltaTime)
        {
            if (!initialized)
            {
                Initialize(snapshot);
            }

            var recipeIndex = Mathf.Clamp((int)snapshot.SelectedRecipe, 0, Enum.GetValues(typeof(RecipeId)).Length - 1);
            IngredientStage = ResolveIngredientStage(snapshot);
            RenderRecipeStage(recipeIndex);
            RenderCounter(snapshot);
            RenderDoors(snapshot, deltaTime);
            RenderOvenLife(snapshot);
            RenderCustomers(snapshot, deltaTime);
            RenderAmbientLife();
        }

        public void CelebrateSale()
        {
            foreach (var customer in customers)
            {
                if (customer != null && !customer.IsLeaving && customer.QueueIndex == 0)
                {
                    customer.CompletePurchase();
                    return;
                }
            }

            foreach (var customer in customers)
            {
                if (customer != null && !customer.IsLeaving && customer.QueueIndex >= 0)
                {
                    customer.CompletePurchase();
                    return;
                }
            }
        }

        private void RenderRecipeStage(int selectedIndex)
        {
            SetOnly(ingredientDisplays, IngredientStage == BakeryIngredientStage.MiseEnPlace ? selectedIndex : -1);
            SetOnly(ovenRawDisplays, IngredientStage == BakeryIngredientStage.InOvenRaw ? selectedIndex : -1);
            SetOnly(ovenBakedDisplays, IngredientStage == BakeryIngredientStage.InOvenBaked ? selectedIndex : -1);
        }

        private void RenderCounter(BakerySnapshot snapshot)
        {
            VisibleCounterItems = 0;
            foreach (var display in counterDisplays)
            {
                if (display == null)
                {
                    continue;
                }

                var stock = display.RecipeId == snapshot.StockRecipe ? snapshot.CounterStock : 0;
                display.SetStock(stock, snapshot.CounterCapacity);
                VisibleCounterItems += display.VisibleCount;
            }
        }

        private void RenderDoors(BakerySnapshot snapshot, float deltaTime)
        {
            var fridgeOpen = snapshot.Phase == BakeryWorkPhase.FetchingDough
                && snapshot.PhaseProgress > 0.1f
                && snapshot.PhaseProgress < 0.46f;
            var ovenOpen = (snapshot.Phase == BakeryWorkPhase.LoadingOven
                    && snapshot.PhaseProgress > 0.58f)
                || (snapshot.Phase == BakeryWorkPhase.Serving
                    && snapshot.PhaseProgress < 0.76f);

            if (fridgeDoor != null)
            {
                var target = fridgeDoorClosed * Quaternion.Euler(0f, fridgeOpen ? -72f : 0f, 0f);
                fridgeDoor.localRotation = Quaternion.Slerp(
                    fridgeDoor.localRotation,
                    target,
                    1f - Mathf.Exp(-deltaTime * 10f));
            }

            if (ovenDoor != null)
            {
                var target = ovenDoorClosed * Quaternion.Euler(ovenOpen ? 62f : 0f, 0f, 0f);
                ovenDoor.localRotation = Quaternion.Slerp(
                    ovenDoor.localRotation,
                    target,
                    1f - Mathf.Exp(-deltaTime * 12f));
            }
        }

        private void RenderOvenLife(BakerySnapshot snapshot)
        {
            var ovenWorking = snapshot.Phase == BakeryWorkPhase.LoadingOven
                || snapshot.Phase == BakeryWorkPhase.Baking
                || snapshot.Phase == BakeryWorkPhase.WaitingForCounter;
            ovenGlow?.SetWorking(ovenWorking);

            for (var index = 0; index < steamPuffs.Length; index++)
            {
                var puff = steamPuffs[index];
                if (puff == null)
                {
                    continue;
                }

                var active = snapshot.Phase == BakeryWorkPhase.Baking && snapshot.PhaseProgress > 0.18f;
                puff.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var cycle = Mathf.Repeat(Time.unscaledTime * 0.48f + index * 0.31f, 1f);
                puff.localPosition = steamBasePositions[index] + Vector3.up * (cycle * 0.82f);
                puff.localScale = steamBaseScales[index] * Mathf.Lerp(0.35f, 1.15f, cycle);
            }
        }

        private void RenderCustomers(BakerySnapshot snapshot, float deltaTime)
        {
            var assigned = new bool[customers.Length];
            var waitingCount = Mathf.Clamp(snapshot.WaitingCustomers, 0, customers.Length);
            var counterBlockedByDeparture = false;
            foreach (var customer in customers)
            {
                counterBlockedByDeparture |= customer != null && customer.BlocksService;
            }

            for (var queueOffset = 0; queueOffset < waitingCount; queueOffset++)
            {
                var targetIndex = Mathf.Min(1, queueOffset + (counterBlockedByDeparture ? 1 : 0));
                var actorIndex = FindCustomerAtQueueIndex(targetIndex, assigned);
                if (actorIndex < 0)
                {
                    actorIndex = FindAnyWaitingCustomer(assigned);
                }

                if (actorIndex < 0)
                {
                    break;
                }

                assigned[actorIndex] = true;
                customers[actorIndex].SetQueueIndex(targetIndex);
            }

            for (var index = 0; index < customers.Length; index++)
            {
                if (!assigned[index] && customers[index] != null && !customers[index].IsLeaving)
                {
                    customers[index].SetQueueIndex(-1);
                }
            }

            foreach (var customer in customers)
            {
                customer?.Render(deltaTime);
            }
        }

        private int FindCustomerAtQueueIndex(int queueIndex, bool[] assigned)
        {
            for (var index = 0; index < customers.Length; index++)
            {
                var customer = customers[index];
                if (!assigned[index] && customer != null && !customer.IsLeaving && customer.QueueIndex == queueIndex)
                {
                    return index;
                }
            }

            return -1;
        }

        private int FindAnyWaitingCustomer(bool[] assigned)
        {
            var unassignedActor = -1;
            for (var index = 0; index < customers.Length; index++)
            {
                var customer = customers[index];
                if (assigned[index] || customer == null || customer.IsLeaving)
                {
                    continue;
                }

                if (customer.QueueIndex >= 0)
                {
                    return index;
                }

                unassignedActor = unassignedActor < 0 ? index : unassignedActor;
            }

            return unassignedActor;
        }

        private void RenderAmbientLife()
        {
            if (hangingBell != null)
            {
                hangingBell.localRotation = hangingBellBase
                    * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.unscaledTime * 1.35f) * 4.2f);
            }
        }

        private static BakeryIngredientStage ResolveIngredientStage(BakerySnapshot snapshot)
        {
            return snapshot.Phase switch
            {
                BakeryWorkPhase.WaitingForDough => BakeryIngredientStage.MiseEnPlace,
                BakeryWorkPhase.FetchingDough => snapshot.PhaseProgress < 0.82f
                    ? BakeryIngredientStage.MiseEnPlace
                    : BakeryIngredientStage.CarriedRaw,
                BakeryWorkPhase.WaitingForOven => BakeryIngredientStage.CarriedRaw,
                BakeryWorkPhase.LoadingOven => snapshot.PhaseProgress < 0.91f
                    ? BakeryIngredientStage.CarriedRaw
                    : BakeryIngredientStage.InOvenRaw,
                BakeryWorkPhase.Baking => snapshot.PhaseProgress < 0.72f
                    ? BakeryIngredientStage.InOvenRaw
                    : BakeryIngredientStage.InOvenBaked,
                BakeryWorkPhase.WaitingForCounter => BakeryIngredientStage.InOvenBaked,
                BakeryWorkPhase.Serving => BakeryIngredientStage.CarriedBaked,
                _ => BakeryIngredientStage.MiseEnPlace
            };
        }

        private static void SetOnly(GameObject[] displays, int activeIndex)
        {
            for (var index = 0; index < displays.Length; index++)
            {
                var display = displays[index];
                if (display != null && display.activeSelf != (index == activeIndex))
                {
                    display.SetActive(index == activeIndex);
                }
            }
        }
    }
}
