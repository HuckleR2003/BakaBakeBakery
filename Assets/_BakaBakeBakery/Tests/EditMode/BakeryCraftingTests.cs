using System.Collections.Generic;
using System.Linq;
using BakaBakeBakery.Data;
using BakaBakeBakery.Gameplay;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class BakeryCraftingTests
    {
        [Test]
        public void MorningBasketSupportsChocolateMuffinDiscovery()
        {
            var crafting = new BakeryCraftingSystem(new BakeryProgressData());
            crafting.AddMorningBasket();

            var ingredients = new List<IngredientId>
            {
                IngredientId.Chocolate,
                IngredientId.Flour,
                IngredientId.Milk
            };

            Assert.That(crafting.TryCraft(ingredients, out var recipe), Is.True);
            Assert.That(recipe.Result, Is.EqualTo(RecipeId.ChocolateMuffin));
            Assert.That(crafting.Count(IngredientId.Flour), Is.EqualTo(5));
            Assert.That(crafting.Count(IngredientId.Milk), Is.EqualTo(3));
            Assert.That(crafting.Count(IngredientId.Chocolate), Is.EqualTo(3));
        }

        [Test]
        public void FailedExperimentConsumesNothing()
        {
            var crafting = new BakeryCraftingSystem(new BakeryProgressData());
            crafting.AddMorningBasket();

            Assert.That(crafting.TryCraft(
                new[] { IngredientId.Flour, IngredientId.Jam },
                out _), Is.False);
            Assert.That(crafting.Count(IngredientId.Flour), Is.EqualTo(6));
            Assert.That(crafting.Count(IngredientId.Jam), Is.EqualTo(3));
        }

        [Test]
        public void CraftingRejectsMissingStockAndOversizedRecipes()
        {
            var empty = new BakeryCraftingSystem(new BakeryProgressData());

            Assert.That(empty.TryCraft(
                new[] { IngredientId.PuffPastry, IngredientId.Jam },
                out _), Is.False);
            Assert.That(empty.TryCraft(
                new[] { IngredientId.Flour, IngredientId.Milk, IngredientId.Jam, IngredientId.Chocolate, IngredientId.PuffPastry },
                out _), Is.False);
        }

        [Test]
        public void WorldSweetRecipesAreUniqueAndOrderIndependent()
        {
            Assert.That(BakeryCraftingSystem.Recipes.Count, Is.EqualTo(8));

            var pastel = new[] { IngredientId.Milk, IngredientId.PuffPastry, IngredientId.Milk };
            Assert.That(BakeryCraftingSystem.TryFindRecipe(pastel, out var pastelRecipe), Is.True);
            Assert.That(pastelRecipe.Result, Is.EqualTo(RecipeId.PastelDeNata));

            var brownie = new[] { IngredientId.Chocolate, IngredientId.Flour, IngredientId.Chocolate };
            Assert.That(BakeryCraftingSystem.TryFindRecipe(brownie, out var brownieRecipe), Is.True);
            Assert.That(brownieRecipe.Result, Is.EqualTo(RecipeId.FudgeBrownie));

            var signatures = new HashSet<string>();
            foreach (var recipe in BakeryCraftingSystem.Recipes)
            {
                var signature = string.Join(",", recipe.Ingredients.OrderBy(value => value));
                Assert.That(signatures.Add(signature), Is.True, $"Duplicate crafting signature for {recipe.DisplayName}");
            }
        }
    }
}
