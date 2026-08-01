using BakaBakeBakery.Gameplay;
using NUnit.Framework;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class BakeryTutorialTests
    {
        [TestCase(-50, BakeryTutorialStep.Welcome)]
        [TestCase(0, BakeryTutorialStep.Welcome)]
        [TestCase(5, BakeryTutorialStep.DiscoverRecipe)]
        [TestCase(500, BakeryTutorialStep.Complete)]
        public void CorruptedTutorialStepsAreClamped(int rawStep, BakeryTutorialStep expected)
        {
            Assert.That(BakeryTutorial.Normalize(rawStep), Is.EqualTo(expected));
        }

        [Test]
        public void EveryGuideBeatHasClickableRepliesAndARealObjective()
        {
            for (var rawStep = 0; rawStep <= BakeryTutorial.FinalStep; rawStep++)
            {
                var beat = BakeryTutorial.GetBeat((BakeryTutorialStep)rawStep);

                Assert.That(beat.Title, Is.Not.Empty);
                Assert.That(beat.Copy.Length, Is.GreaterThan(30));
                Assert.That(beat.Objective, Is.Not.Empty);
                Assert.That(beat.PrimaryReply, Is.Not.Empty);
                Assert.That(beat.SecondaryReply, Is.Not.Empty);
            }
        }

        [Test]
        public void MechanicBeatsPointAtTheControlThatCanAdvanceThem()
        {
            Assert.That(BakeryTutorial.GetBeat(BakeryTutorialStep.VisitMarket).Target, Is.EqualTo(BakeryTutorialTarget.DayBoard));
            Assert.That(BakeryTutorial.GetBeat(BakeryTutorialStep.FirstLoaf).Target, Is.EqualTo(BakeryTutorialTarget.BakerAction));
            Assert.That(BakeryTutorial.GetBeat(BakeryTutorialStep.DiscoverRecipe).Target, Is.EqualTo(BakeryTutorialTarget.CraftTab));
            Assert.That(BakeryTutorial.GetBeat(BakeryTutorialStep.DaySummary).Target, Is.EqualTo(BakeryTutorialTarget.DayBoard));
        }
    }
}
