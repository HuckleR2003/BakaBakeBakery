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
            var before = Stock(crafting);

            var ingredients = new List<IngredientId>
            {
                IngredientId.Chocolate,
                IngredientId.Flour,
                IngredientId.Milk
            };

            Assert.That(crafting.TryCraft(ingredients, out var recipe), Is.True);
            Assert.That(recipe.Result, Is.EqualTo(RecipeId.ChocolateMuffin));
            Assert.That(crafting.Count(IngredientId.Flour), Is.EqualTo(before[IngredientId.Flour] - 1));
            Assert.That(crafting.Count(IngredientId.Milk), Is.EqualTo(before[IngredientId.Milk] - 1));
            Assert.That(crafting.Count(IngredientId.Chocolate), Is.EqualTo(before[IngredientId.Chocolate] - 1));
            Assert.That(crafting.Count(IngredientId.Jam), Is.EqualTo(before[IngredientId.Jam]));
        }

        [Test]
        public void FailedExperimentConsumesNothing()
        {
            var crafting = new BakeryCraftingSystem(new BakeryProgressData());
            crafting.AddMorningBasket();
            var before = Stock(crafting);

            Assert.That(crafting.TryCraft(
                new[] { IngredientId.Flour, IngredientId.Jam },
                out _), Is.False);

            foreach (var pair in before)
            {
                Assert.That(crafting.Count(pair.Key), Is.EqualTo(pair.Value), $"{pair.Key} was spent by a failed idea.");
            }
        }

        /// <summary>
        /// The test kitchen is meant to be played with, and an empty pantry reads as broken
        /// crafting rather than as a limit, so one basket has to cover the whole book at once.
        /// </summary>
        [Test]
        public void OneMorningBasketCoversEveryDiscoveryWithoutRestocking()
        {
            var crafting = new BakeryCraftingSystem(new BakeryProgressData());
            crafting.AddMorningBasket();

            foreach (var recipe in BakeryCraftingSystem.Recipes)
            {
                Assert.That(
                    crafting.TryCraft(recipe.Ingredients.ToArray(), out var crafted),
                    Is.True,
                    $"The basket ran dry before {recipe.DisplayName}.");
                Assert.That(crafted.Result, Is.EqualTo(recipe.Result));
            }

            foreach (IngredientId ingredient in System.Enum.GetValues(typeof(IngredientId)))
            {
                Assert.That(crafting.Count(ingredient), Is.GreaterThan(0), $"No {ingredient} left to keep experimenting.");
            }
        }

        [Test]
        public void ANewBakeryCanExperimentBeforeItsFirstMarketRun()
        {
            var crafting = new BakeryCraftingSystem(BakeryProgressStore.CreateNewGame());

            foreach (IngredientId ingredient in System.Enum.GetValues(typeof(IngredientId)))
            {
                Assert.That(crafting.Count(ingredient), Is.GreaterThan(0), $"A fresh pantry has no {ingredient}.");
            }

            Assert.That(
                crafting.TryCraft(new[] { IngredientId.PuffPastry, IngredientId.Jam }, out var recipe),
                Is.True);
            Assert.That(recipe.Result, Is.EqualTo(RecipeId.JamTurnover));
        }

        private static Dictionary<IngredientId, int> Stock(BakeryCraftingSystem crafting)
        {
            var stock = new Dictionary<IngredientId, int>();
            foreach (IngredientId ingredient in System.Enum.GetValues(typeof(IngredientId)))
            {
                stock[ingredient] = crafting.Count(ingredient);
            }

            return stock;
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
