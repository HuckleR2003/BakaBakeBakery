using System;
using System.Collections.Generic;
using System.Linq;
using BakaBakeBakery.Data;

namespace BakaBakeBakery.Gameplay
{
    public enum IngredientId
    {
        Flour,
        Milk,
        PuffPastry,
        Jam,
        Chocolate
    }

    public readonly struct CraftingRecipe
    {
        public CraftingRecipe(RecipeId result, string displayName, params IngredientId[] ingredients)
        {
            Result = result;
            DisplayName = displayName;
            Ingredients = ingredients ?? Array.Empty<IngredientId>();
        }

        public RecipeId Result { get; }
        public string DisplayName { get; }
        public IReadOnlyList<IngredientId> Ingredients { get; }
    }

    public sealed class BakeryCraftingSystem
    {
        public const int MinimumIngredients = 2;
        public const int MaximumIngredients = 4;

        private static readonly CraftingRecipe[] KnownRecipes =
        {
            new(RecipeId.ChocolateMuffin, "Chocolate Muffin", IngredientId.Flour, IngredientId.Milk, IngredientId.Chocolate),
            new(RecipeId.JamTurnover, "Village Jam Turnover", IngredientId.PuffPastry, IngredientId.Jam),
            new(RecipeId.ChocolatePillow, "Chocolate Pillow", IngredientId.PuffPastry, IngredientId.Milk, IngredientId.Chocolate),
            new(RecipeId.PastelDeNata, "Pastel de Nata", IngredientId.PuffPastry, IngredientId.Milk, IngredientId.Milk),
            new(RecipeId.RaspberryMacaron, "Raspberry Macaron", IngredientId.Flour, IngredientId.Milk, IngredientId.Jam, IngredientId.Jam),
            new(RecipeId.HoneyBaklava, "Honey Baklava", IngredientId.PuffPastry, IngredientId.Flour, IngredientId.Jam),
            new(RecipeId.ChocolateCannoli, "Chocolate Cannoli", IngredientId.Flour, IngredientId.Milk, IngredientId.Chocolate, IngredientId.Chocolate),
            new(RecipeId.FudgeBrownie, "Fudge Brownie", IngredientId.Flour, IngredientId.Chocolate, IngredientId.Chocolate)
        };

        private readonly int[] stock = new int[Enum.GetValues(typeof(IngredientId)).Length];

        public BakeryCraftingSystem(BakeryProgressData progress)
        {
            var safe = BakeryProgressStore.Sanitize(progress);
            stock[(int)IngredientId.Flour] = safe.flour;
            stock[(int)IngredientId.Milk] = safe.milk;
            stock[(int)IngredientId.PuffPastry] = safe.puffPastry;
            stock[(int)IngredientId.Jam] = safe.jam;
            stock[(int)IngredientId.Chocolate] = safe.chocolate;
        }

        public static IReadOnlyList<CraftingRecipe> Recipes => KnownRecipes;

        public int Count(IngredientId ingredient) => stock[(int)ingredient];

        /// <summary>
        /// A morning basket worth experimenting with. The test kitchen is meant to be played with,
        /// and a failed formula costs nothing, so a thin pantry only ever made the panel feel dead.
        /// </summary>
        public void AddMorningBasket()
        {
            Add(IngredientId.Flour, 24);
            Add(IngredientId.Milk, 18);
            Add(IngredientId.PuffPastry, 14);
            Add(IngredientId.Jam, 14);
            Add(IngredientId.Chocolate, 16);
        }

        public void Add(IngredientId ingredient, int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            stock[(int)ingredient] = Math.Min(999, stock[(int)ingredient] + amount);
        }

        public bool CanReserve(IngredientId ingredient, IReadOnlyList<IngredientId> currentSlots)
        {
            var alreadyReserved = currentSlots?.Count(item => item == ingredient) ?? 0;
            return Count(ingredient) > alreadyReserved;
        }

        public bool TryCraft(IReadOnlyList<IngredientId> ingredients, out CraftingRecipe result)
        {
            result = default;
            if (ingredients == null
                || ingredients.Count < MinimumIngredients
                || ingredients.Count > MaximumIngredients)
            {
                return false;
            }

            for (var index = 0; index < ingredients.Count; index++)
            {
                if (!CanReserve(ingredients[index], ingredients.Take(index).ToArray()))
                {
                    return false;
                }
            }

            if (!TryFindRecipe(ingredients, out result))
            {
                return false;
            }

            foreach (var ingredient in ingredients)
            {
                stock[(int)ingredient]--;
            }

            return true;
        }

        public static bool TryFindRecipe(IReadOnlyList<IngredientId> ingredients, out CraftingRecipe result)
        {
            if (ingredients != null)
            {
                foreach (var recipe in KnownRecipes)
                {
                    if (SameIngredients(recipe.Ingredients, ingredients))
                    {
                        result = recipe;
                        return true;
                    }
                }
            }

            result = default;
            return false;
        }

        public void ExportInto(BakeryProgressData progress)
        {
            progress.flour = Count(IngredientId.Flour);
            progress.milk = Count(IngredientId.Milk);
            progress.puffPastry = Count(IngredientId.PuffPastry);
            progress.jam = Count(IngredientId.Jam);
            progress.chocolate = Count(IngredientId.Chocolate);
        }

        public static string IngredientName(IngredientId ingredient)
        {
            return ingredient switch
            {
                IngredientId.Flour => "Flour",
                IngredientId.Milk => "Milk",
                IngredientId.PuffPastry => "Puff pastry",
                IngredientId.Jam => "Strawberry jam",
                IngredientId.Chocolate => "Chocolate",
                _ => ingredient.ToString()
            };
        }

        private static bool SameIngredients(IReadOnlyList<IngredientId> left, IReadOnlyList<IngredientId> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            var counts = new int[Enum.GetValues(typeof(IngredientId)).Length];
            foreach (var ingredient in left)
            {
                counts[(int)ingredient]++;
            }

            foreach (var ingredient in right)
            {
                counts[(int)ingredient]--;
            }

            return counts.All(count => count == 0);
        }
    }
}
