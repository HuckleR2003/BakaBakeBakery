using System.Collections.Generic;
using BakaBakeBakery.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BakaBakeBakery.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class StudioIntroController : MonoBehaviour
    {
        private const float VialRevealStart = 0.18f;
        private const float VialRevealEnd = 0.88f;
        private const float CopyRevealStart = 0.68f;
        private const float CopyRevealEnd = 1.48f;
        private const float ExplosionStart = 3.48f;
        private const float WipeStart = 4.25f;
        private const float SceneChangeTime = 5.05f;
        private const float FailsafeTime = 8f;

        private static readonly Vector2[] ShardDirections =
        {
            new(-72f, -46f),
            new(-54f, 42f),
            new(-18f, -78f),
            new(24f, -72f),
            new(58f, -35f),
            new(70f, 24f),
            new(30f, 58f),
            new(-34f, 64f)
        };

        [SerializeField] private StyleSheet styleSheet;

        private readonly List<VisualElement> shards = new();
        private VisualElement vialReveal;
        private VisualElement copyReveal;
        private VisualElement intactVial;
        private VisualElement spill;
        private VisualElement wipe;
        private VisualElement wipeEdge;
        private float elapsed;
        private bool ready;
        private bool skipRequested;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            vialReveal = root.Q<VisualElement>("vial-reveal");
            copyReveal = root.Q<VisualElement>("copy-reveal");
            intactVial = root.Q<VisualElement>("intact-vial");
            spill = root.Q<VisualElement>("spill");
            wipe = root.Q<VisualElement>("scene-wipe");
            wipeEdge = root.Q<VisualElement>("wipe-edge");

            shards.Clear();
            for (var index = 0; index < ShardDirections.Length; index++)
            {
                var shard = root.Q<VisualElement>($"shard-{index}");
                if (shard != null)
                {
                    shards.Add(shard);
                }
            }

            ready = vialReveal != null
                && copyReveal != null
                && intactVial != null
                && spill != null
                && wipe != null
                && wipeEdge != null
                && shards.Count == ShardDirections.Length;

            if (!ready)
            {
                Debug.LogError("[Baka Bake Bakery] Studio intro UI is incomplete. Falling back to Main Menu.");
            }

            elapsed = 0f;
            skipRequested = false;
            RenderFrame(0f);
        }

        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;

            if (BuildSmokeProbe.IsSmokeTest)
            {
                if (elapsed >= 0.15f)
                {
                    LoadMainMenu();
                }

                return;
            }

            if (!ready)
            {
                if (elapsed >= 0.2f)
                {
                    LoadMainMenu();
                }

                return;
            }

            if (!skipRequested && WasSkipPressed())
            {
                skipRequested = true;
                elapsed = Mathf.Max(elapsed, WipeStart);
            }

            if (GameSettings.ReduceMotion && elapsed < ExplosionStart)
            {
                RenderReducedMotionFrame(elapsed);
            }
            else
            {
                RenderFrame(elapsed);
            }

            if (elapsed >= SceneChangeTime || elapsed >= FailsafeTime)
            {
                LoadMainMenu();
            }
        }

        private void RenderReducedMotionFrame(float time)
        {
            vialReveal.style.width = 144f;
            copyReveal.style.width = 530f;
            intactVial.style.opacity = 1f;
            spill.style.opacity = 0f;
            HideShards();

            var fade = SmoothProgress(time, 2.9f, 3.48f);
            intactVial.style.opacity = 1f - fade;
            copyReveal.style.opacity = 1f - fade;
            RenderWipe(Mathf.Max(0f, time - WipeStart));
        }

        private void RenderFrame(float time)
        {
            var vialProgress = EaseOutCubic(Progress(time, VialRevealStart, VialRevealEnd));
            var copyProgress = EaseOutCubic(Progress(time, CopyRevealStart, CopyRevealEnd));
            vialReveal.style.width = 144f * vialProgress;
            copyReveal.style.width = 530f * copyProgress;
            copyReveal.style.opacity = SmoothProgress(time, CopyRevealStart, CopyRevealStart + 0.25f);

            var explosionProgress = Progress(time, ExplosionStart, WipeStart);
            intactVial.style.opacity = explosionProgress <= 0f ? 1f : 0f;
            RenderExplosion(explosionProgress);
            RenderWipe(Mathf.Max(0f, time - WipeStart));
        }

        private void RenderExplosion(float progress)
        {
            if (progress <= 0f)
            {
                spill.style.opacity = 0f;
                spill.style.scale = new Scale(Vector2.zero);
                HideShards();
                return;
            }

            var eased = EaseOutCubic(progress);
            spill.style.opacity = Mathf.Clamp01(progress * 3f);
            spill.style.scale = new Scale(new Vector2(
                Mathf.Lerp(0.12f, 1f, eased),
                Mathf.Lerp(0.2f, 1f, eased)));

            for (var index = 0; index < shards.Count; index++)
            {
                var shard = shards[index];
                var direction = ShardDirections[index];
                var gravity = 62f * progress * progress;
                shard.style.display = DisplayStyle.Flex;
                shard.style.opacity = 1f - Mathf.Max(0f, (progress - 0.78f) * 2.8f);
                shard.style.translate = new Translate(
                    new Length(direction.x * eased),
                    new Length(direction.y * eased + gravity));
                shard.style.rotate = new Rotate(new Angle(
                    (index % 2 == 0 ? 1f : -1f) * (55f + index * 19f) * eased,
                    AngleUnit.Degree));
            }
        }

        private void RenderWipe(float timeSinceWipe)
        {
            var progress = EaseInOutCubic(Mathf.Clamp01(timeSinceWipe / 0.76f));
            var x = Mathf.Lerp(-145f, 8f, progress);
            wipe.style.translate = new Translate(Length.Percent(x), Length.Percent(0f));
            wipeEdge.style.opacity = progress > 0f && progress < 1f ? 1f : 0f;
        }

        private void HideShards()
        {
            foreach (var shard in shards)
            {
                shard.style.display = DisplayStyle.None;
            }
        }

        private static bool WasSkipPressed()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);
        }

        private static float Progress(float value, float start, float end)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(start, end, value));
        }

        private static float SmoothProgress(float value, float start, float end)
        {
            return Mathf.SmoothStep(0f, 1f, Progress(value, start, end));
        }

        private static float EaseOutCubic(float value)
        {
            var inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static float EaseInOutCubic(float value)
        {
            return value < 0.5f
                ? 4f * value * value * value
                : 1f - Mathf.Pow(-2f * value + 2f, 3f) * 0.5f;
        }

        private static void LoadMainMenu()
        {
            SceneFlow.TryLoad(SceneFlow.MainMenuScene);
        }
    }
}
