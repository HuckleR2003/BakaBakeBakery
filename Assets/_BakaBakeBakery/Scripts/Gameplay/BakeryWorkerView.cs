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
        [SerializeField] private Transform leftShoulder;
        [SerializeField] private Transform rightShoulder;
        [SerializeField] private Transform leftUpperArm;
        [SerializeField] private Transform rightUpperArm;
        [SerializeField] private Transform leftForearm;
        [SerializeField] private Transform rightForearm;
        [SerializeField] private Transform leftHand;
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform carryAnchor;
        [SerializeField] private GameObject carryTray;
        [SerializeField] private GameObject[] rawCarryDisplays;
        [SerializeField] private GameObject[] bakedCarryDisplays;

        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private Quaternion leftLegBase;
        private Quaternion rightLegBase;
        private Vector3 leftUpperArmBaseScale;
        private Vector3 rightUpperArmBaseScale;
        private Vector3 leftForearmBaseScale;
        private Vector3 rightForearmBaseScale;
        private Vector3 currentLeftHand;
        private Vector3 currentRightHand;
        private float pulse;
        private float walkBlend;
        private bool initialized;

        public bool IsCarryingRaw { get; private set; }
        public bool IsCarryingBaked { get; private set; }
        public bool IsWalking => walkBlend > 0.12f;
        public bool NaturalHandRigReady => HasHandRig() && carryAnchor != null;
        public float HandGripWidth => leftHand != null && rightHand != null
            ? Vector3.Distance(leftHand.position, rightHand.position)
            : float.PositiveInfinity;

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
            leftUpperArmBaseScale = ScaleOf(leftUpperArm);
            rightUpperArmBaseScale = ScaleOf(rightUpperArm);
            leftForearmBaseScale = ScaleOf(leftForearm);
            rightForearmBaseScale = ScaleOf(rightForearm);
            currentLeftHand = leftHand != null ? leftHand.localPosition : new Vector3(-0.64f, 1.02f, -0.4f);
            currentRightHand = rightHand != null ? rightHand.localPosition : new Vector3(0.68f, 1.08f, -0.42f);
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

            RenderCarry(snapshot);
            RenderLimbs(snapshot, stride, deltaTime);
        }

        public void Pulse()
        {
            pulse = 1f;
        }

        private void RenderLimbs(BakerySnapshot snapshot, float stride, float deltaTime)
        {
            var legAngle = stride * 25f;
            ApplyLimb(leftLeg, leftLegBase, legAngle);
            ApplyLimb(rightLeg, rightLegBase, -legAngle);

            if (!HasHandRig())
            {
                return;
            }

            ResolveHandTargets(snapshot, stride, out var leftTarget, out var rightTarget, out var elbowsWide);
            var follow = 1f - Mathf.Exp(-Mathf.Max(0f, deltaTime) * 14f);
            currentLeftHand = Vector3.Lerp(currentLeftHand, leftTarget, follow);
            currentRightHand = Vector3.Lerp(currentRightHand, rightTarget, follow);
            PlaceArm(
                leftShoulder.localPosition,
                currentLeftHand,
                true,
                elbowsWide,
                leftUpperArm,
                leftForearm,
                leftHand,
                leftUpperArmBaseScale,
                leftForearmBaseScale);
            PlaceArm(
                rightShoulder.localPosition,
                currentRightHand,
                false,
                elbowsWide,
                rightUpperArm,
                rightForearm,
                rightHand,
                rightUpperArmBaseScale,
                rightForearmBaseScale);

            if (carryAnchor != null && (IsCarryingRaw || IsCarryingBaked))
            {
                var carryTarget = (currentLeftHand + currentRightHand) * 0.5f + new Vector3(0f, 0.035f, -0.015f);
                if (snapshot.Phase == BakeryWorkPhase.LoadingOven
                    && snapshot.PhaseProgress >= 0.48f
                    && snapshot.PhaseProgress < 0.66f)
                {
                    carryTarget = currentLeftHand + new Vector3(0.42f, 0.025f, 0f);
                }

                carryAnchor.localPosition = Vector3.Lerp(
                    carryAnchor.localPosition,
                    carryTarget,
                    follow);
                carryAnchor.localRotation = Quaternion.Slerp(carryAnchor.localRotation, Quaternion.identity, follow);
            }
        }

        private void ResolveHandTargets(
            BakerySnapshot snapshot,
            float stride,
            out Vector3 left,
            out Vector3 right,
            out bool elbowsWide)
        {
            left = new Vector3(-0.61f, 1.02f, -0.35f);
            right = new Vector3(0.65f, 1.08f, -0.37f);
            elbowsWide = false;

            if (IsWalking)
            {
                if (IsCarryingRaw || IsCarryingBaked)
                {
                    SetCarryPose(out left, out right);
                    elbowsWide = true;
                }
                else
                {
                    left += new Vector3(0f, stride * 0.08f, stride * 0.2f);
                    right += new Vector3(0f, -stride * 0.08f, -stride * 0.2f);
                }

                return;
            }

            var progress = Mathf.Clamp01(snapshot.PhaseProgress);
            switch (snapshot.Phase)
            {
                case BakeryWorkPhase.FetchingDough:
                    if (progress < 0.34f)
                    {
                        var reach = SmoothPulse(progress / 0.34f);
                        right = Vector3.Lerp(right, new Vector3(0.76f, 1.29f, -0.76f), reach);
                    }
                    else if (progress < 0.82f)
                    {
                        var knead = Mathf.Sin((progress - 0.34f) * 9f * Mathf.PI);
                        left = new Vector3(-0.25f, 0.9f + knead * 0.06f, -0.72f);
                        right = new Vector3(0.25f, 0.9f - knead * 0.06f, -0.72f);
                        elbowsWide = true;
                    }
                    else
                    {
                        SetCarryPose(out left, out right);
                        elbowsWide = true;
                    }
                    break;
                case BakeryWorkPhase.WaitingForOven:
                    SetCarryPose(out left, out right);
                    elbowsWide = true;
                    break;
                case BakeryWorkPhase.LoadingOven:
                    if (progress < 0.48f)
                    {
                        SetCarryPose(out left, out right);
                        elbowsWide = true;
                    }
                    else if (progress < 0.66f)
                    {
                        left = new Vector3(-0.42f, 1.42f, -0.68f);
                        right = Vector3.Lerp(
                            new Vector3(0.42f, 1.42f, -0.68f),
                            new Vector3(1.15f, 1.63f, -0.7f),
                            Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.48f, 0.66f, progress)));
                    }
                    else if (progress < 0.91f)
                    {
                        var push = Mathf.InverseLerp(0.66f, 0.91f, progress);
                        left = Vector3.Lerp(new Vector3(-0.42f, 1.42f, -0.68f), new Vector3(-0.27f, 1.08f, -0.9f), push);
                        right = Vector3.Lerp(new Vector3(0.42f, 1.42f, -0.68f), new Vector3(0.27f, 1.08f, -0.9f), push);
                        elbowsWide = true;
                    }
                    break;
                case BakeryWorkPhase.Baking:
                    right = new Vector3(
                        0.58f,
                        1.12f + Mathf.Sin(Time.unscaledTime * 1.7f) * 0.03f,
                        -0.4f);
                    break;
                case BakeryWorkPhase.WaitingForCounter:
                    left = new Vector3(-0.25f, 0.94f, -0.78f);
                    right = new Vector3(0.25f, 0.94f, -0.78f);
                    elbowsWide = true;
                    break;
                case BakeryWorkPhase.Serving:
                    if (progress < 0.24f)
                    {
                        var lift = SmoothPulse(progress / 0.24f);
                        left = Vector3.Lerp(new Vector3(-0.25f, 0.92f, -0.8f), new Vector3(-0.29f, 1.12f, -0.64f), lift);
                        right = Vector3.Lerp(new Vector3(0.25f, 0.92f, -0.8f), new Vector3(0.29f, 1.12f, -0.64f), lift);
                    }
                    else if (progress < 0.78f)
                    {
                        SetCarryPose(out left, out right);
                    }
                    else
                    {
                        var place = Mathf.InverseLerp(0.78f, 1f, progress);
                        left = Vector3.Lerp(new Vector3(-0.3f, 1.16f, -0.64f), new Vector3(-0.3f, 1.0f, -0.82f), place);
                        right = Vector3.Lerp(new Vector3(0.3f, 1.16f, -0.64f), new Vector3(0.3f, 1.0f, -0.82f), place);
                    }
                    elbowsWide = true;
                    break;
            }
        }

        private static void SetCarryPose(out Vector3 left, out Vector3 right)
        {
            left = new Vector3(-0.43f, 1.4f, -0.68f);
            right = new Vector3(0.43f, 1.4f, -0.68f);
        }

        private void PlaceArm(
            Vector3 shoulder,
            Vector3 handPosition,
            bool isLeft,
            bool elbowsWide,
            Transform upperArm,
            Transform forearm,
            Transform hand,
            Vector3 upperBaseScale,
            Vector3 forearmBaseScale)
        {
            var side = isLeft ? -1f : 1f;
            var elbow = Vector3.Lerp(shoulder, handPosition, 0.48f)
                + new Vector3(side * (elbowsWide ? 0.3f : 0.19f), 0.1f, 0.07f);
            PlaceSegment(upperArm, shoulder, elbow, upperBaseScale);
            PlaceSegment(forearm, elbow, handPosition, forearmBaseScale);
            hand.localPosition = handPosition;
            var grip = IsCarryingRaw || IsCarryingBaked;
            var handTarget = grip
                ? Quaternion.Euler(8f, 0f, isLeft ? -34f : 34f)
                : Quaternion.identity;
            hand.localRotation = Quaternion.Slerp(hand.localRotation, handTarget, 0.35f);
        }

        private static void PlaceSegment(Transform segment, Vector3 start, Vector3 end, Vector3 baseScale)
        {
            if (segment == null)
            {
                return;
            }

            var direction = end - start;
            if (direction.sqrMagnitude < 0.0001f)
            {
                return;
            }

            segment.localPosition = (start + end) * 0.5f;
            segment.localRotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
            segment.localScale = new Vector3(baseScale.x, direction.magnitude * 0.5f, baseScale.z);
        }

        private void RenderCarry(BakerySnapshot snapshot)
        {
            var selectedIndex = Mathf.Clamp((int)snapshot.SelectedRecipe, 0, Enum.GetValues(typeof(RecipeId)).Length - 1);
            IsCarryingRaw = (snapshot.Phase == BakeryWorkPhase.FetchingDough && snapshot.PhaseProgress >= 0.82f)
                || snapshot.Phase == BakeryWorkPhase.WaitingForOven
                || (snapshot.Phase == BakeryWorkPhase.LoadingOven && snapshot.PhaseProgress < 0.91f);
            IsCarryingBaked = snapshot.Phase == BakeryWorkPhase.Serving;
            SetOnly(rawCarryDisplays, IsCarryingRaw ? selectedIndex : -1);
            SetOnly(bakedCarryDisplays, IsCarryingBaked ? selectedIndex : -1);
            if (carryTray != null)
            {
                carryTray.SetActive(IsCarryingRaw || IsCarryingBaked);
            }
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

        private bool HasHandRig()
        {
            return leftShoulder != null
                && rightShoulder != null
                && leftUpperArm != null
                && rightUpperArm != null
                && leftForearm != null
                && rightForearm != null
                && leftHand != null
                && rightHand != null;
        }

        private static Vector3 ScaleOf(Transform target)
        {
            return target != null ? target.localScale : Vector3.one * 0.2f;
        }

        private static float SmoothPulse(float normalized)
        {
            var value = Mathf.Clamp01(normalized);
            return Mathf.Sin(value * Mathf.PI);
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
