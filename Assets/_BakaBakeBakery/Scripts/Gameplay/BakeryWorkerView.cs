using System;
using BakaBakeBakery.Data;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BakeryWorkerView : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform idleStation;
        [SerializeField] private Transform fridgeStation;
        [SerializeField] private Transform prepStation;
        [SerializeField] private Transform ovenStation;
        [SerializeField] private Transform counterStation;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private GameObject[] rawCarryDisplays;
        [SerializeField] private GameObject[] bakedCarryDisplays;

        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Quaternion leftArmBase;
        private Quaternion rightArmBase;
        private float pulse;
        private float walkBlend;
        private bool initialized;

        public bool IsCarryingRaw { get; private set; }
        public bool IsCarryingBaked { get; private set; }
        public bool IsWalking => walkBlend > 0.12f;

        public void Initialize(BakerySnapshot snapshot)
        {
            if (visualRoot == null || idleStation == null || fridgeStation == null || prepStation == null || ovenStation == null || counterStation == null)
            {
                Debug.LogError("[Baka Bake Bakery] Baker view is missing a station reference.", this);
                enabled = false;
                return;
            }

            rawCarryDisplays ??= Array.Empty<GameObject>();
            bakedCarryDisplays ??= Array.Empty<GameObject>();
            visualBasePosition = visualRoot.localPosition;
            visualBaseRotation = visualRoot.localRotation;
            leftLegBase = leftLeg != null ? leftLeg.localRotation : Quaternion.identity;
            rightLegBase = rightLeg != null ? rightLeg.localRotation : Quaternion.identity;
            leftArmBase = leftArm != null ? leftArm.localRotation : Quaternion.identity;
            rightArmBase = rightArm != null ? rightArm.localRotation : Quaternion.identity;
            transform.localPosition = ResolveTarget(snapshot);
            RenderCarry(snapshot);
            initialized = true;
        }

        public void Render(BakerySnapshot snapshot, float deltaTime)
        {
            if (!initialized)
            {
                Initialize(snapshot);
            }

            if (!enabled)
            {
                return;
            }

            var target = ResolveTarget(snapshot);
            var before = transform.localPosition;
            var speed = snapshot.Phase == BakeryWorkPhase.Serving ? 3.6f : 4.25f;
            transform.localPosition = Vector3.MoveTowards(before, target, deltaTime * speed);
            var movement = transform.localPosition - before;
            var movingNow = movement.sqrMagnitude > 0.000001f;
            walkBlend = Mathf.MoveTowards(walkBlend, movingNow ? 1f : 0f, deltaTime * 7.5f);

            var stride = Mathf.Sin(Time.unscaledTime * 13f) * walkBlend;
            var stepLift = Mathf.Abs(stride) * 0.085f;
            var breathing = Mathf.Sin(Time.unscaledTime * 2.4f) * 0.016f;
            pulse = Mathf.MoveTowards(pulse, 0f, deltaTime * 3.2f);
            visualRoot.localPosition = visualBasePosition + Vector3.up * (stepLift + breathing);
            visualRoot.localScale = Vector3.one * (1f + pulse * 0.055f);

            var direction = movingNow ? movement.x : target.x - transform.localPosition.x;
            var facing = Mathf.Abs(direction) > 0.01f ? Mathf.Sign(direction) * 14f : 0f;
            var workingTilt = ResolveWorkingTilt(snapshot);
            visualRoot.localRotation = Quaternion.Slerp(
                visualRoot.localRotation,
                visualBaseRotation * Quaternion.Euler(0f, facing, workingTilt),
                1f - Mathf.Exp(-deltaTime * 8f));

            RenderLimbs(snapshot, stride);
            RenderCarry(snapshot);
        }

        public void Pulse()
        {
            pulse = 1f;
        }

        private void RenderLimbs(BakerySnapshot snapshot, float stride)
        {
            var legAngle = stride * 25f;
            var armAngle = -stride * 16f;
            var leftArmAction = 0f;
            var rightArmAction = 0f;

            if (!IsWalking)
            {
                var workWave = Mathf.Sin(snapshot.PhaseProgress * Mathf.PI);
                switch (snapshot.Phase)
                {
                    case BakeryWorkPhase.FetchingDough:
                        rightArmAction = -32f * workWave;
                        break;
                    case BakeryWorkPhase.LoadingOven:
                        leftArmAction = -38f * workWave;
                        rightArmAction = -38f * workWave;
                        break;
                    case BakeryWorkPhase.Baking:
                        rightArmAction = Mathf.Sin(Time.unscaledTime * 2.2f) * 5f;
                        break;
                    case BakeryWorkPhase.Serving:
                        leftArmAction = -24f;
                        rightArmAction = -24f;
                        break;
                }
            }

            ApplyLimb(leftLeg, leftLegBase, legAngle);
            ApplyLimb(rightLeg, rightLegBase, -legAngle);
            ApplyLimb(leftArm, leftArmBase, armAngle + leftArmAction);
            ApplyLimb(rightArm, rightArmBase, -armAngle + rightArmAction);
        }

        private void RenderCarry(BakerySnapshot snapshot)
        {
            var selectedIndex = Mathf.Clamp((int)snapshot.SelectedRecipe, 0, Enum.GetValues(typeof(RecipeId)).Length - 1);
            IsCarryingRaw = (snapshot.Phase == BakeryWorkPhase.FetchingDough && snapshot.PhaseProgress >= 0.82f)
                || snapshot.Phase == BakeryWorkPhase.WaitingForOven
                || (snapshot.Phase == BakeryWorkPhase.LoadingOven && snapshot.PhaseProgress < 0.72f);
            IsCarryingBaked = snapshot.Phase == BakeryWorkPhase.Serving;
            SetOnly(rawCarryDisplays, IsCarryingRaw ? selectedIndex : -1);
            SetOnly(bakedCarryDisplays, IsCarryingBaked ? selectedIndex : -1);
        }

        private Vector3 ResolveTarget(BakerySnapshot snapshot)
        {
            return snapshot.Phase switch
            {
                BakeryWorkPhase.FetchingDough => snapshot.PhaseProgress < 0.36f
                    ? fridgeStation.localPosition
                    : prepStation.localPosition,
                BakeryWorkPhase.WaitingForOven => prepStation.localPosition,
                BakeryWorkPhase.LoadingOven => ovenStation.localPosition,
                BakeryWorkPhase.Baking => ovenStation.localPosition,
                BakeryWorkPhase.WaitingForCounter => ovenStation.localPosition,
                BakeryWorkPhase.Serving => counterStation.localPosition,
                _ => idleStation.localPosition
            };
        }

        private static float ResolveWorkingTilt(BakerySnapshot snapshot)
        {
            return snapshot.Phase switch
            {
                BakeryWorkPhase.FetchingDough => -Mathf.Sin(snapshot.PhaseProgress * Mathf.PI) * 8f,
                BakeryWorkPhase.LoadingOven => Mathf.Sin(snapshot.PhaseProgress * Mathf.PI) * 10f,
                BakeryWorkPhase.Baking => Mathf.Sin(Time.unscaledTime * 3.1f) * 1.8f,
                BakeryWorkPhase.Serving => -Mathf.Sin(snapshot.PhaseProgress * Mathf.PI) * 6f,
                _ => 0f
            };
        }

        private static void ApplyLimb(Transform limb, Quaternion baseRotation, float angle)
        {
            if (limb != null)
            {
                limb.localRotation = baseRotation * Quaternion.Euler(angle, 0f, 0f);
            }
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
