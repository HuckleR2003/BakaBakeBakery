using BakaBakeBakery.Core;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class SettingsTests
    {
        [Test]
        public void MasterVolumeIsClampedToSafeRange()
        {
            var original = GameSettings.MasterVolume;

            try
            {
                GameSettings.SetMasterVolume(2f);
                Assert.That(GameSettings.MasterVolume, Is.EqualTo(1f));

                GameSettings.SetMasterVolume(-1f);
                Assert.That(GameSettings.MasterVolume, Is.EqualTo(0f));
            }
            finally
            {
                GameSettings.SetMasterVolume(original);
            }
        }

        [Test]
        public void ReduceMotionPreferenceCanBeChangedAtRuntime()
        {
            var original = GameSettings.ReduceMotion;

            try
            {
                GameSettings.SetReduceMotion(!original);
                Assert.That(GameSettings.ReduceMotion, Is.EqualTo(!original));
            }
            finally
            {
                GameSettings.SetReduceMotion(original);
            }
        }

        [Test]
        public void SceneNamesRemainStableForBuildFlow()
        {
            Assert.That(SceneFlow.StudioIntroScene, Is.EqualTo("StudioIntro"));
            Assert.That(SceneFlow.MainMenuScene, Is.EqualTo("MainMenu"));
            Assert.That(SceneFlow.MainBakeryScene, Is.EqualTo("MainBakery"));
        }
    }
}
