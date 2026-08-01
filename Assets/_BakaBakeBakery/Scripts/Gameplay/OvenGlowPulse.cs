using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [RequireComponent(typeof(Light))]
    public sealed class OvenGlowPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 0.5f)] private float amplitude = 0.12f;
        [SerializeField, Min(0.01f)] private float frequency = 0.7f;

        private Light glow;
        private float baseIntensity;
        private float workingBlend;
        private float phase;

        public void SetWorking(bool working)
        {
            workingBlend = Mathf.MoveTowards(
                workingBlend,
                working ? 1f : 0f,
                Time.unscaledDeltaTime * 3.5f);
        }

        private void Awake()
        {
            glow = GetComponent<Light>();
            baseIntensity = glow.intensity;
            phase = transform.position.x * 0.37f;
        }

        private void Update()
        {
            var safeAmplitude = Mathf.Clamp(amplitude, 0f, 0.5f);
            var safeFrequency = Mathf.Max(0.01f, frequency);
            var restingLevel = Mathf.Lerp(0.14f, 1f, workingBlend);
            glow.intensity = baseIntensity * restingLevel * (
                1f + Mathf.Sin((Time.unscaledTime + phase) * safeFrequency) * safeAmplitude * workingBlend);
        }
    }
}
