using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BakeryWorkerView : MonoBehaviour
    {
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Transform idleStation;
        [SerializeField] private Transform fridgeStation;
        [SerializeField] private Transform ovenStation;
        [SerializeField] private Transform counterStation;
        [SerializeField] private GameObject carriedLoaf;

        private BakeryWorkPhase renderedPhase;
        private Vector3 phaseStartPosition;
        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private float pulse;
        private bool initialized;

        public void Initialize(BakerySnapshot snapshot)
        {
            if (visualRoot == null || idleStation == null || fridgeStation == null || ovenStation == null || counterStation == null)
            {
                Debug.LogError("[Baka Bake Bakery] Baker view is missing a station reference.", this);
                enabled = false;
                return;
            }

            visualBasePosition = visualRoot.localPosition;
            visualBaseRotation = visualRoot.localRotation;
            renderedPhase = snapshot.Phase;
            transform.localPosition = ResolveTarget(snapshot.Phase);
            phaseStartPosition = transform.localPosition;
            SetCarry(snapshot.Phase);
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

            if (snapshot.Phase != renderedPhase)
            {
                renderedPhase = snapshot.Phase;
                phaseStartPosition = transform.localPosition;
                SetCarry(snapshot.Phase);
            }

            var target = ResolveTarget(snapshot.Phase);
            var travelProgress = IsTravelPhase(snapshot.Phase)
                ? SmoothStep(snapshot.PhaseProgress)
                : 1f;
            transform.localPosition = Vector3.LerpUnclamped(phaseStartPosition, target, travelProgress);

            var travelLift = IsTravelPhase(snapshot.Phase)
                ? Mathf.Sin(snapshot.PhaseProgress * Mathf.PI) * 0.11f
                : 0f;
            var breathing = Mathf.Sin(Time.unscaledTime * 2.4f) * 0.018f;
            pulse = Mathf.MoveTowards(pulse, 0f, deltaTime * 3.2f);
            visualRoot.localPosition = visualBasePosition + Vector3.up * (travelLift + breathing);
            visualRoot.localScale = Vector3.one * (1f + pulse * 0.055f);

            var direction = target.x - phaseStartPosition.x;
            var facing = Mathf.Abs(direction) > 0.03f ? Mathf.Sign(direction) * 12f : 0f;
            var workingTilt = snapshot.Phase == BakeryWorkPhase.Baking
                ? Mathf.Sin(Time.unscaledTime * 4.2f) * 2.2f
                : 0f;
            visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(0f, facing, workingTilt);
        }

        public void Pulse()
        {
            pulse = 1f;
        }

        private Vector3 ResolveTarget(BakeryWorkPhase phase)
        {
            return phase switch
            {
                BakeryWorkPhase.FetchingDough => fridgeStation.localPosition,
                BakeryWorkPhase.WaitingForOven => fridgeStation.localPosition,
                BakeryWorkPhase.LoadingOven => ovenStation.localPosition,
                BakeryWorkPhase.Baking => ovenStation.localPosition,
                BakeryWorkPhase.WaitingForCounter => ovenStation.localPosition,
                BakeryWorkPhase.Serving => counterStation.localPosition,
                _ => idleStation.localPosition
            };
        }

        private void SetCarry(BakeryWorkPhase phase)
        {
            if (carriedLoaf == null)
            {
                return;
            }

            carriedLoaf.SetActive(
                phase == BakeryWorkPhase.WaitingForOven
                || phase == BakeryWorkPhase.LoadingOven
                || phase == BakeryWorkPhase.WaitingForCounter
                || phase == BakeryWorkPhase.Serving);
        }

        private static bool IsTravelPhase(BakeryWorkPhase phase)
        {
            return phase == BakeryWorkPhase.FetchingDough
                || phase == BakeryWorkPhase.LoadingOven
                || phase == BakeryWorkPhase.Serving;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
