using System.Collections.Generic;
using BakaBakeBakery.Data;
using NUnit.Framework;
using UnityEditor;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class RecipeCatalogTests
    {
        private const string CatalogPath = "Assets/_BakaBakeBakery/Data/BakeryCatalog.asset";

        [Test]
        public void FoundationCatalogContainsNineUniqueRecipes()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>(CatalogPath);

            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Recipes, Has.Count.EqualTo(9));

            var identifiers = new HashSet<RecipeId>();
            foreach (var recipe in catalog.Recipes)
            {
                Assert.That(recipe, Is.Not.Null);
                Assert.That(identifiers.Add(recipe.Id), Is.True, $"Duplicate recipe id: {recipe.Id}");
                Assert.That(recipe.TotalProcessSeconds, Is.GreaterThan(0f));
                Assert.That(recipe.BatchRevenue, Is.GreaterThan(0));
            }
        }

        [Test]
        public void CraftingDiscoveriesHavePlayableRecipeDefinitions()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>(CatalogPath);

            Assert.That(catalog.Find(RecipeId.ChocolateMuffin), Is.Not.Null);
            Assert.That(catalog.Find(RecipeId.JamTurnover), Is.Not.Null);
            Assert.That(catalog.Find(RecipeId.ChocolatePillow), Is.Not.Null);
        }

        [Test]
        public void CinnamonSwirlRequiresFinishingAndBakeryLevelTwo()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>(CatalogPath);
            var swirl = catalog.Find(RecipeId.CinnamonSwirl);

            Assert.That(swirl, Is.Not.Null);
            Assert.That(swirl.RequiresFinishing, Is.True);
            Assert.That(swirl.RequiredBakeryLevel, Is.EqualTo(2));
        }

        [Test]
        public void NewPastriesHaveDistinctEconomyAndFinishingSteps()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<BakeryCatalog>(CatalogPath);
            var finezja = catalog.Find(RecipeId.Finezja);
            var monocle = catalog.Find(RecipeId.CinnamonMonocle);

            Assert.That(finezja, Is.Not.Null);
            Assert.That(monocle, Is.Not.Null);
            Assert.That(finezja.RequiresFinishing, Is.True);
            Assert.That(monocle.RequiresFinishing, Is.True);
            Assert.That(finezja.BatchRevenue, Is.Not.EqualTo(monocle.BatchRevenue));
            Assert.That(finezja.UnlockAtSales, Is.LessThan(monocle.UnlockAtSales));
        }
    }
}
