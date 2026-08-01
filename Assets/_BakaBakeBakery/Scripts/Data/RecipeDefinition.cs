using UnityEngine;

namespace BakaBakeBakery.Data
{
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Baka Bake Bakery/Recipe")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private RecipeId id;
        [SerializeField] private string displayName = string.Empty;
        [TextArea(2, 4)]
        [SerializeField] private string customerDescription = string.Empty;

        [Header("Production")]
        [Min(0f)]
        [SerializeField] private float preparationSeconds;
        [Min(0.1f)]
        [SerializeField] private float bakeSeconds = 1f;
        [Min(0f)]
        [SerializeField] private float finishingSeconds;
        [Min(1)]
        [SerializeField] private int batchYield = 1;

        [Header("Economy")]
        [Min(0)]
        [SerializeField] private int salePrice = 1;
        [Min(0)]
        [SerializeField] private int unlockAtSales;
        [Min(1)]
        [SerializeField] private int requiredBakeryLevel = 1;

        public RecipeId Id => id;
        public string DisplayName => displayName;
        public string CustomerDescription => customerDescription;
        public float PreparationSeconds => preparationSeconds;
        public float BakeSeconds => bakeSeconds;
        public float FinishingSeconds => finishingSeconds;
        public int BatchYield => batchYield;
        public int SalePrice => salePrice;
        public int UnlockAtSales => unlockAtSales;
        public int RequiredBakeryLevel => requiredBakeryLevel;
        public bool RequiresFinishing => finishingSeconds > 0f;
        public float TotalProcessSeconds => preparationSeconds + bakeSeconds + finishingSeconds;
        public int BatchRevenue => batchYield * salePrice;
        public float RevenuePerSecond => TotalProcessSeconds <= 0f ? 0f : BatchRevenue / TotalProcessSeconds;

        private void OnValidate()
        {
            preparationSeconds = Mathf.Max(0f, preparationSeconds);
            bakeSeconds = Mathf.Max(0.1f, bakeSeconds);
            finishingSeconds = Mathf.Max(0f, finishingSeconds);
            batchYield = Mathf.Max(1, batchYield);
            salePrice = Mathf.Max(0, salePrice);
            unlockAtSales = Mathf.Max(0, unlockAtSales);
            requiredBakeryLevel = Mathf.Max(1, requiredBakeryLevel);
        }
    }
}
