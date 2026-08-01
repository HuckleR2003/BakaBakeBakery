using System.Linq;
using BakaBakeBakery.Core;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class BuildConfigurationTests
    {
        [Test]
        public void ShippingScenesAreEnabledInJourneyOrder()
        {
            var scenes = EditorBuildSettings.scenes;

            Assert.That(scenes, Has.Length.EqualTo(3));
            Assert.That(scenes.All(scene => scene.enabled), Is.True);
            Assert.That(scenes[0].path, Does.EndWith($"/{SceneFlow.StudioIntroScene}.unity"));
            Assert.That(scenes[1].path, Does.EndWith($"/{SceneFlow.MainMenuScene}.unity"));
            Assert.That(scenes[2].path, Does.EndWith($"/{SceneFlow.MainBakeryScene}.unity"));
        }

        [Test]
        public void PlayerBrandAndWindowDefaultsAreIntentional()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo("HCK Labs"));
            Assert.That(PlayerSettings.productName, Is.EqualTo("Baka Bake Bakery"));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("0.4.0"));
            Assert.That(PlayerSettings.defaultScreenWidth, Is.EqualTo(1600));
            Assert.That(PlayerSettings.defaultScreenHeight, Is.EqualTo(900));
            Assert.That(PlayerSettings.resizableWindow, Is.True);
            Assert.That(PlayerSettings.runInBackground, Is.False);
            Assert.That(PlayerSettings.SplashScreen.show, Is.False);
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone),
                Is.EqualTo("com.hcklabs.bakabakebakery"));
        }
    }
}
