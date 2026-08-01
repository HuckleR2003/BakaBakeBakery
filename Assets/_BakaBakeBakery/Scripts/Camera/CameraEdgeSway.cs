using UnityEngine;
using UnityEngine.InputSystem;
using BakaBakeBakery.Core;

namespace BakaBakeBakery.CameraSystem
{
    [DisallowMultipleComponent]
    public sealed class CameraEdgeSway : MonoBehaviour
    {
        [Header("Framing")]
        [SerializeField, Range(0f, 0.9f)] private float deadZone = 0.52f;
        [SerializeField] private Vector2 maximumOffset = new(0.42f, 0.16f);
        [SerializeField, Range(0f, 8f)] private float maximumYaw = 2.2f;
        [SerializeField, Range(0f, 8f)] private float maximumPitch = 1.2f;
        [SerializeField, Min(0.01f)] private float smoothTime = 0.18f;

        [Header("Accessibility")]
        [SerializeField] private bool reduceMotion;

        private Vector3 baseLocalPosition;
        private Quaternion baseLocalRotation;
        private Vector3 positionVelocity;

        public bool ReduceMotion
        {
            get => reduceMotion;
            set => reduceMotion = value;
        }

        private void Awake()
        {
            baseLocalPosition = transform.localPosition;
            baseLocalRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            reduceMotion = GameSettings.ReduceMotion;
        }

        private void LateUpdate()
        {
            var safeSmoothTime = Mathf.Max(0.01f, smoothTime);
            var pointer = ReadPointerViewportPosition();
            var response = reduceMotion ? Vector2.zero : EvaluatePointer(pointer, deadZone);
            var targetPosition = baseLocalPosition + new Vector3(
                response.x * maximumOffset.x,
                response.y * maximumOffset.y,
                0f);
            var targetRotation = baseLocalRotation * Quaternion.Euler(
                -response.y * maximumPitch,
                response.x * maximumYaw,
                0f);

            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                targetPosition,
                ref positionVelocity,
                safeSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                targetRotation,
                1f - Mathf.Exp(-Time.unscaledDeltaTime / safeSmoothTime));
        }

        private void OnDisable()
        {
            transform.localPosition = baseLocalPosition;
            transform.localRotation = baseLocalRotation;
            positionVelocity = Vector3.zero;
        }

        public static Vector2 EvaluatePointer(Vector2 viewportPosition, float responseDeadZone)
        {
            var centered = viewportPosition * 2f - Vector2.one;
            var horizontal = EvaluateAxis(centered.x, responseDeadZone);
            var upwardOnly = Mathf.Max(0f, EvaluateAxis(centered.y, responseDeadZone));
            return new Vector2(horizontal, upwardOnly);
        }

        private static float EvaluateAxis(float centeredValue, float responseDeadZone)
        {
            var magnitude = Mathf.Abs(centeredValue);
            if (magnitude <= responseDeadZone)
            {
                return 0f;
            }

            var normalized = Mathf.InverseLerp(responseDeadZone, 1f, magnitude);
            return Mathf.Sign(centeredValue) * normalized * normalized;
        }

        private static Vector2 ReadPointerViewportPosition()
        {
            if (Mouse.current == null || Screen.width <= 0 || Screen.height <= 0)
            {
                return new Vector2(0.5f, 0.5f);
            }

            var screenPosition = Mouse.current.position.ReadValue();
            return new Vector2(
                Mathf.Clamp01(screenPosition.x / Screen.width),
                Mathf.Clamp01(screenPosition.y / Screen.height));
        }
    }
}
