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
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private GameObject purchaseParcel;

        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion leftArmBase;
        private Quaternion rightArmBase;
        private Vector3 visualBasePosition;
        private Vector3 targetPosition;
        private float leavePause;
        private int queueIndex = -1;
        private bool initialized;
        private bool leaving;

        public int QueueIndex => queueIndex;
        public bool IsLeaving => leaving;

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
            leavePause = 0.38f;
            targetPosition = exitStation.localPosition;
            SetParcel(true);
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

            if (walking)
            {
                var facing = Mathf.Sign(movement.x);
                visualRoot.localRotation = Quaternion.Slerp(
                    visualRoot.localRotation,
                    Quaternion.Euler(0f, facing < 0f ? 20f : -20f, 0f),
                    1f - Mathf.Exp(-deltaTime * 8f));
            }

            if (Vector3.SqrMagnitude(transform.localPosition - targetPosition) <= 0.0025f
                && queueIndex < 0)
            {
                leaving = false;
                SetParcel(false);
                visualRoot.gameObject.SetActive(false);
            }
        }

        private void SetParcel(bool visible)
        {
            if (purchaseParcel != null)
            {
                purchaseParcel.SetActive(visible);
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
