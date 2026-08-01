using BakaBakeBakery.Data;
using UnityEngine;

namespace BakaBakeBakery.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class BakeryCounterDisplay : MonoBehaviour
    {
        [SerializeField] private RecipeId recipeId;
        [SerializeField] private Transform[] servings;

        private Vector3[] fullScales;
        private int desiredVisibleCount;
        private bool initialized;

        public RecipeId RecipeId => recipeId;
        public int VisibleCount => desiredVisibleCount;

        public void Initialize()
        {
            if (initialized)
            {
                return;
            }

            servings ??= System.Array.Empty<Transform>();
            fullScales = new Vector3[servings.Length];
            for (var index = 0; index < servings.Length; index++)
            {
                var serving = servings[index];
                if (serving == null)
                {
                    continue;
                }

                fullScales[index] = serving.localScale;
                serving.gameObject.SetActive(false);
            }

            initialized = true;
        }

        public void SetStock(int stock, int capacity)
        {
            Initialize();
            var safeStock = Mathf.Max(0, stock);
            var safeCapacity = Mathf.Max(1, capacity);
            var normalizedCount = safeCapacity <= servings.Length
                ? safeStock
                : Mathf.CeilToInt((float)safeStock / safeCapacity * servings.Length);
            desiredVisibleCount = Mathf.Clamp(normalizedCount, 0, servings.Length);
        }

        private void Update()
        {
            if (!initialized)
            {
                Initialize();
            }

            var delta = Time.unscaledDeltaTime;
            for (var index = 0; index < servings.Length; index++)
            {
                var serving = servings[index];
                if (serving == null)
                {
                    continue;
                }

                if (index < desiredVisibleCount)
                {
                    if (!serving.gameObject.activeSelf)
                    {
                        serving.localScale = fullScales[index] * 0.08f;
                        serving.gameObject.SetActive(true);
                    }

                    serving.localScale = Vector3.Lerp(
                        serving.localScale,
                        fullScales[index],
                        1f - Mathf.Exp(-delta * 12f));
                }
                else if (serving.gameObject.activeSelf)
                {
                    serving.localScale = Vector3.MoveTowards(
                        serving.localScale,
                        Vector3.zero,
                        delta * 4.5f);
                    if (serving.localScale.sqrMagnitude <= 0.001f)
                    {
                        serving.gameObject.SetActive(false);
                        serving.localScale = fullScales[index];
                    }
                }
            }
        }
    }
}
