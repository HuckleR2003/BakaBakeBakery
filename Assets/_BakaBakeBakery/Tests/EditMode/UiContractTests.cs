using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.UIElements;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class UiContractTests
    {
        private const string UiRoot = "Assets/_BakaBakeBakery/UI";

        [Test]
        public void StudioIntroContainsEveryAnimatedPart()
        {
            var root = Clone("StudioIntro.uxml");

            Assert.That(root.Q<VisualElement>("vial-reveal"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("copy-reveal"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("intact-vial"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("spill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("scan-line"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("shockwave"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("scene-wipe"), Is.Not.Null);
            Assert.That(
                Enumerable.Range(0, 12).All(index => root.Q<VisualElement>($"shard-{index}") != null),
                Is.True);
        }

        [Test]
        public void MainMenuContainsNavigationAndComfortSettings()
        {
            var root = Clone("MainMenu.uxml");

            Assert.That(root.Q<Button>("start-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("settings-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("quit-button"), Is.Not.Null);
            Assert.That(root.Q<Toggle>("fullscreen-toggle"), Is.Not.Null);
            Assert.That(root.Q<Toggle>("reduce-motion-toggle"), Is.Not.Null);
            Assert.That(root.Q<Slider>("volume-slider"), Is.Not.Null);
        }

        [Test]
        public void BakeryHudSeparatesProductJourneyAndCraftingWorkbench()
        {
            var root = Clone("MainBakery.uxml");

            Assert.That(root.Query<Button>(className: "recipe-card").ToList(), Has.Count.EqualTo(9));
            Assert.That(root.Q<Button>("home-tab"), Is.Not.Null);
            Assert.That(root.Q<Button>("craft-tab"), Is.Not.Null);
            Assert.That(root.Q<Button>("craft-result"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("next-unlock"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("market-map"), Is.Not.Null);
            Assert.That(
                Enumerable.Range(0, 4).All(index => root.Q<Button>($"craft-slot-{index}") != null),
                Is.True);
            Assert.That(root.Q<Button>("action-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("second-oven-button"), Is.Not.Null);
            Assert.That(root.Q<Button>("bakery-upgrade-button"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("warmth-fill"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("baker-bubble"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("grandmother-bubble"), Is.Not.Null);
            Assert.That(root.Q<VisualElement>("friend-bubble"), Is.Not.Null);
        }

        private static VisualElement Clone(string fileName)
        {
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UiRoot}/{fileName}");
            Assert.That(asset, Is.Not.Null, $"Missing UI asset: {fileName}");
            return asset.CloneTree();
        }
    }
}
