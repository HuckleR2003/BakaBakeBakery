using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BakeryCustomerActor : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform entranceStation;
        [SerializeField] private Transform serviceStation;
        [SerializeField] private Transform queueStation;
        [SerializeField] private Transform exitStation;
        [SerializeField] private Transform postPurchaseStation;
        [SerializeField] private Transform farExitStation;
        [SerializeField] private bool eatsPurchase;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform pickupArm;
        [SerializeField] private Transform pickupHand;
        [SerializeField] private Transform counterPickupStation;
        [SerializeField] private GameObject purchaseParcel;

        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion leftArmBase;
        private Quaternion rightArmBase;
        private Vector3 visualBasePosition;
        private Vector3 targetPosition;
        private float leavePause;
        private float eatingRemaining;
        private int queueIndex = -1;
        private bool initialized;
        private bool leaving;
        private DepartureStage departureStage;
        private Vector3 parcelBasePosition;
        private Vector3 pickupHandBasePosition;
        private float collectionRemaining;

        private const float CollectionDuration = 1.18f;

        private enum DepartureStage
        {
            None,
            Collecting,
            HeadingToPark,
            Eating,
            Exiting
        }

        public int QueueIndex => queueIndex;
        public bool IsLeaving => leaving;
        public bool BlocksService => leaving
            && (departureStage == DepartureStage.Collecting || departureStage == DepartureStage.HeadingToPark)
            && serviceStation != null
            && Vector3.SqrMagnitude(transform.localPosition - serviceStation.localPosition) < 2.25f;

        public void Initialize()
        {
            if (initialized || entranceStation == null || serviceStation == null || queueStation == null || exitStation == null)
            {
                return;
            }

            transform.localPosition = entranceStation.localPosition;
            targetPosition = transform.localPosition;
            visualBasePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            leftLegBase = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            rightLegBase = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            leftArmBase = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmBase = rightArm != null ? rightArm.localRotation : Quaternion.identity;
            parcelBasePosition = purchaseParcel != null ? purchaseParcel.transform.localPosition : Vector3.zero;
            pickupHandBasePosition = pickupHand != null ? pickupHand.localPosition : Vector3.zero;
            SetParcel(false);
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(false);
            }

            initialized = true;
        }

        public void SetQueueIndex(int index)
        {
            Initialize();
            if (!initialized || leaving)
            {
                return;
            }

            queueIndex = index;
            if (visualRoot != null)
            {
                visualRoot.gameObject.SetActive(index >= 0);
            }

            targetPosition = index switch
            {
                0 => serviceStation.localPosition,
                1 => queueStation.localPosition,
                _ => entranceStation.localPosition
            };
        }

        public void CompletePurchase()
        {
            Initialize();
            if (!initialized || leaving)
            {
                return;
            }

            queueIndex = -1;
            leaving = true;
            leavePause = 0f;
            collectionRemaining = CollectionDuration;
            departureStage = DepartureStage.Collecting;
            targetPosition = serviceStation.localPosition;
            SetParcel(true);
            if (purchaseParcel != null && counterPickupStation != null)
            {
                purchaseParcel.transform.position = counterPickupStation.position;
            }
        }

        public void Render(float deltaTime)
        {
            Initialize();
            if (!initialized || visualRoot == null || !visualRoot.gameObject.activeSelf)
            {
                return;
            }

            if (leavePause > 0f)
            {
                leavePause = Mathf.Max(0f, leavePause - deltaTime);
            }

            if (departureStage == DepartureStage.Collecting)
            {
                RenderCollection(deltaTime);
                return;
            }

            if (departureStage == DepartureStage.Eating)
            {
                eatingRemaining = Mathf.Max(0f, eatingRemaining - deltaTime);
                var bite = Mathf.SmoothStep(0f, 1f, Mathf.PingPong((3.4f - eatingRemaining) * 1.35f, 1f));
                ApplySwing(leftArm, leftArmBase, -18f);
                ApplySwing(rightArm, rightArmBase, -18f - bite * 48f);
                if (purchaseParcel != null)
                {
                    purchaseParcel.transform.localPosition = Vector3.Lerp(
                        parcelBasePosition,
                        new Vector3(0.18f, 1.48f, -0.48f),
                        bite * 0.72f);
                }

                visualRoot.localPosition = visualBasePosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * 2f) * 0.012f);
                if (eatingRemaining <= 0f)
                {
                    departureStage = DepartureStage.Exiting;
                    targetPosition = farExitStation != null ? farExitStation.localPosition : exitStation.localPosition;
                    if (purchaseParcel != null) purchaseParcel.transform.localPosition = parcelBasePosition;
                }

                return;
            }

            var before = transform.localPosition;
            if (leavePause <= 0f)
            {
                transform.localPosition = Vector3.MoveTowards(before, targetPosition, deltaTime * 2.15f);
            }

            var movement = transform.localPosition - before;
            var walking = movement.sqrMagnitude > 0.000001f;
            var walkWave = walking ? Mathf.Sin(Time.unscaledTime * 11f) : 0f;
            var bob = walking ? Mathf.Abs(walkWave) * 0.055f : Mathf.Sin(Time.unscaledTime * 1.8f) * 0.012f;
            visualRoot.localPosition = visualBasePosition + Vector3.up * bob;

            ApplySwing(leftLeg, leftLegBase, walkWave * 20f);
            ApplySwing(rightLeg, rightLegBase, -walkWave * 20f);
            ApplySwing(leftArm, leftArmBase, -walkWave * 11f);
            ApplySwing(rightArm, rightArmBase, walkWave * 11f);
            if (leaving && !walking)
            {
                ApplySwing(leftArm, leftArmBase, -18f);
                ApplySwing(rightArm, rightArmBase, -22f);
            }

            if (walking)
            {
                var facing = Mathf.Sign(movement.x);
                visualRoot.localRotation = Quaternion.Slerp(
                    visualRoot.localRotation,
                    Quaternion.Euler(0f, facing < 0f ? 20f : -20f, 0f),
                    1f - Mathf.Exp(-deltaTime * 8f));
            }

            if (leaving && Vector3.SqrMagnitude(transform.localPosition - targetPosition) <= 0.0025f)
            {
                if (departureStage == DepartureStage.HeadingToPark && eatsPurchase)
                {
                    departureStage = DepartureStage.Eating;
                    eatingRemaining = 3.4f;
                }
                else if (departureStage == DepartureStage.HeadingToPark)
                {
                    departureStage = DepartureStage.Exiting;
                    targetPosition = farExitStation != null ? farExitStation.localPosition : exitStation.localPosition;
                }
                else if (departureStage == DepartureStage.Exiting)
                {
                    leaving = false;
                    departureStage = DepartureStage.None;
                    SetParcel(false);
                    if (pickupHand != null) pickupHand.localPosition = pickupHandBasePosition;
                    visualRoot.gameObject.SetActive(false);
                    transform.localPosition = entranceStation.localPosition;
                    targetPosition = transform.localPosition;
                }
            }
        }

        private void RenderCollection(float deltaTime)
        {
            collectionRemaining = Mathf.Max(0f, collectionRemaining - deltaTime);
            var progress = 1f - collectionRemaining / CollectionDuration;
            var reach = progress < 0.48f
                ? Mathf.SmoothStep(0f, 1f, progress / 0.48f)
                : Mathf.SmoothStep(1f, 0f, (progress - 0.48f) / 0.52f);

            if (pickupHand != null && counterPickupStation != null && visualRoot != null)
            {
                var pickupLocal = visualRoot.InverseTransformPoint(counterPickupStation.position);
                pickupHand.localPosition = Vector3.Lerp(pickupHandBasePosition, pickupLocal, reach);
            }

            ApplySwing(leftArm, leftArmBase, -10f);
            ApplySwing(rightArm, rightArmBase, -10f);
            if (pickupArm != null)
            {
                var baseRotation = pickupArm == leftArm ? leftArmBase : rightArmBase;
                ApplySwing(pickupArm, baseRotation, -18f - reach * 62f);
            }

            if (purchaseParcel != null && counterPickupStation != null)
            {
                if (progress < 0.48f)
                {
                    purchaseParcel.transform.position = counterPickupStation.position;
                }
                else if (pickupHand != null)
                {
                    purchaseParcel.transform.position = pickupHand.position;
                }
            }

            visualRoot.localPosition = visualBasePosition + Vector3.up * (Mathf.Sin(Time.unscaledTime * 2f) * 0.01f);
            if (collectionRemaining > 0f)
            {
                return;
            }

            if (purchaseParcel != null)
            {
                purchaseParcel.transform.SetParent(visualRoot, true);
                purchaseParcel.transform.localPosition = parcelBasePosition;
            }

            if (pickupHand != null) pickupHand.localPosition = pickupHandBasePosition;
            departureStage = postPurchaseStation != null ? DepartureStage.HeadingToPark : DepartureStage.Exiting;
            targetPosition = postPurchaseStation != null ? postPurchaseStation.localPosition : exitStation.localPosition;
            leavePause = 0.12f;
        }

        private void SetParcel(bool visible)
        {
            if (purchaseParcel != null)
            {
                purchaseParcel.SetActive(visible);
                purchaseParcel.transform.localPosition = parcelBasePosition;
            }
        }

        private static void ApplySwing(Transform limb, Quaternion baseRotation, float angle)
        {
            if (limb != null)
            {
                limb.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
            }
        }
    }
}
