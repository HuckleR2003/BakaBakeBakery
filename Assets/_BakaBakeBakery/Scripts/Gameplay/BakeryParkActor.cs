using System;
using BakaBakeBakery.Core;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    public enum BakeryParkActorKind
    {
        Pedestrian,
        Vehicle
    }

    [DisallowMultipleComponent]
    public sealed class BakeryParkActor : MonoBehaviour
    {
        [SerializeField] private BakeryParkActorKind kind;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform startStation;
        [SerializeField] private Transform endStation;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform[] wheels;
        [SerializeField] private float speed = 1.2f;
        [SerializeField] private float initialDelay;
        [SerializeField] private float loopDelay = 2f;
        [SerializeField] private bool reverseAtEnd = true;

        private Quaternion visualBaseRotation;
        private Vector3 visualBasePosition;
        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion leftArmBase;
        private Quaternion rightArmBase;
        private Vector3 target;
        private float delayRemaining;
        private bool initialized;
        private bool headingToEnd = true;

        public BakeryParkActorKind Kind => kind;
        public bool IsMoving { get; private set; }
        public bool IsConfigured => visualRoot != null && startStation != null && endStation != null;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            Initialize();
            if (!initialized || visualRoot == null)
            {
                return;
            }

            var deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            if (delayRemaining > 0f)
            {
                delayRemaining = Mathf.Max(0f, delayRemaining - deltaTime);
                IsMoving = false;
                visualRoot.gameObject.SetActive(kind == BakeryParkActorKind.Pedestrian);
                AnimateVisual(0f, deltaTime);
                return;
            }

            visualRoot.gameObject.SetActive(true);
            var before = transform.localPosition;
            transform.localPosition = Vector3.MoveTowards(before, target, deltaTime * Mathf.Max(0.1f, speed));
            var movement = transform.localPosition - before;
            IsMoving = movement.sqrMagnitude > 0.000001f;
            AnimateVisual(movement.magnitude, deltaTime);

            if (Vector3.SqrMagnitude(transform.localPosition - target) <= 0.0025f)
            {
                ReachEnd();
            }
        }

        private void Initialize()
        {
            if (initialized || visualRoot == null || startStation == null || endStation == null)
            {
                return;
            }

            wheels ??= Array.Empty<Transform>();
            visualBasePosition = visualRoot.localPosition;
            visualBaseRotation = visualRoot.localRotation;
            leftLegBase = RotationOf(leftLeg);
            rightLegBase = RotationOf(rightLeg);
            leftArmBase = RotationOf(leftArm);
            rightArmBase = RotationOf(rightArm);
            transform.localPosition = startStation.localPosition;
            target = endStation.localPosition;
            delayRemaining = Mathf.Max(0f, initialDelay);
            visualRoot.gameObject.SetActive(kind == BakeryParkActorKind.Pedestrian || delayRemaining <= 0f);
            initialized = true;
        }

        private void AnimateVisual(float travelled, float deltaTime)
        {
            var motionScale = GameSettings.ReduceMotion ? 0.35f : 1f;
            if (kind == BakeryParkActorKind.Vehicle)
            {
                foreach (var wheel in wheels)
                {
                    if (wheel != null && travelled > 0f)
                    {
                        wheel.Rotate(Vector3.right, travelled * 520f * motionScale, Space.Self);
                    }
                }

                visualRoot.localPosition = visualBasePosition
                    + Vector3.up * (Mathf.Sin(Time.unscaledTime * 5f) * 0.008f * motionScale);
                return;
            }

            var walkWave = IsMoving ? Mathf.Sin(Time.unscaledTime * 9.5f) * motionScale : 0f;
            var bob = IsMoving
                ? Mathf.Abs(walkWave) * 0.045f
                : Mathf.Sin(Time.unscaledTime * 1.7f) * 0.01f * motionScale;
            visualRoot.localPosition = visualBasePosition + Vector3.up * bob;
            ApplySwing(leftLeg, leftLegBase, walkWave * 22f);
            ApplySwing(rightLeg, rightLegBase, -walkWave * 22f);
            ApplySwing(leftArm, leftArmBase, -walkWave * 13f);
            ApplySwing(rightArm, rightArmBase, walkWave * 13f);

            if (IsMoving)
            {
                var direction = target - transform.localPosition;
                direction.y = 0f;
                if (direction.sqrMagnitude > 0.001f)
                {
                    var facing = Quaternion.LookRotation(-direction.normalized, Vector3.up);
                    visualRoot.localRotation = Quaternion.Slerp(
                        visualRoot.localRotation,
                        facing,
                        1f - Mathf.Exp(-deltaTime * 6f));
                }
            }
            else
            {
                visualRoot.localRotation = Quaternion.Slerp(
                    visualRoot.localRotation,
                    visualBaseRotation,
                    1f - Mathf.Exp(-deltaTime * 2f));
            }
        }

        private void ReachEnd()
        {
            delayRemaining = Mathf.Max(0.15f, loopDelay);
            if (reverseAtEnd)
            {
                headingToEnd = !headingToEnd;
                target = headingToEnd ? endStation.localPosition : startStation.localPosition;
                return;
            }

            transform.localPosition = startStation.localPosition;
            target = endStation.localPosition;
            if (kind == BakeryParkActorKind.Vehicle)
            {
                visualRoot.gameObject.SetActive(false);
            }
        }

        private static Quaternion RotationOf(Transform targetTransform)
        {
            return targetTransform != null ? targetTransform.localRotation : Quaternion.identity;
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
