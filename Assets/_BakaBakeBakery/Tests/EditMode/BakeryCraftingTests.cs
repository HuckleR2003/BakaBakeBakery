using System.Collections.Generic;
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
    }
}
