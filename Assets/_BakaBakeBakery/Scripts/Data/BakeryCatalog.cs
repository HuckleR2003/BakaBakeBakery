using System.Collections.Generic;
using UnityEngine;

namespace BakaBakeBakery.Data
{
    [CreateAssetMenu(fileName = "BakeryCatalog", menuName = "Baka Bake Bakery/Recipe Catalog")]
    public sealed class BakeryCatalog : ScriptableObject
    {
        [SerializeField] private List<RecipeDefinition> recipes = new();

        public IReadOnlyList<RecipeDefinition> Recipes => recipes;

        public RecipeDefinition Find(RecipeId id)
        {
            if (recipes == null)
            {
                return null;
            }

            for (var index = 0; index < recipes.Count; index++)
            {
                var recipe = recipes[index];
                if (recipe != null && recipe.Id == id)
                {
                    return recipe;
                }
            }

            return null;
        }
    }
}
