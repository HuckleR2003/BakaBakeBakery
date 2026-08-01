using BakaBakeBakery.CameraSystem;
using NUnit.Framework;
using UnityEngine;

namespace BakaBakeBakery.Tests.EditMode
{
    public sealed class CameraEdgeSwayTests
    {
        [Test]
        public void CentreOfScreenHasNoResponse()
        {
            var response = CameraEdgeSway.EvaluatePointer(new Vector2(0.5f, 0.5f), 0.52f);

            Assert.That(response, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void BottomEdgeNeverTiltsCameraDown()
        {
            var response = CameraEdgeSway.EvaluatePointer(new Vector2(0.5f, 0f), 0.52f);

            Assert.That(response.y, Is.EqualTo(0f));
        }

        [Test]
        public void LeftAndRightEdgesAreSymmetrical()
        {
            var left = CameraEdgeSway.EvaluatePointer(new Vector2(0f, 0.5f), 0.52f);
            var right = CameraEdgeSway.EvaluatePointer(new Vector2(1f, 0.5f), 0.52f);

            Assert.That(left.x, Is.EqualTo(-1f).Within(0.0001f));
            Assert.That(right.x, Is.EqualTo(1f).Within(0.0001f));
        }

        [Test]
        public void TopCornerCombinesHorizontalAndUpwardResponse()
        {
            var response = CameraEdgeSway.EvaluatePointer(new Vector2(1f, 1f), 0.52f);

            Assert.That(response.x, Is.GreaterThan(0f));
            Assert.That(response.y, Is.GreaterThan(0f));
        }

        [Test]
        public void LookAroundRangeIsAtLeastSeventyFivePercentWiderThanTheOriginalFraming()
        {
            Assert.That(
                CameraEdgeSway.DefaultMaximumOffset.x,
                Is.GreaterThanOrEqualTo(CameraEdgeSway.LegacyMaximumOffset.x * 1.75f));
            Assert.That(
                CameraEdgeSway.DefaultMaximumOffset.y,
                Is.GreaterThanOrEqualTo(CameraEdgeSway.LegacyMaximumOffset.y * 1.75f));
            Assert.That(
                CameraEdgeSway.DefaultMaximumYaw,
                Is.GreaterThanOrEqualTo(CameraEdgeSway.LegacyMaximumYaw * 1.75f));
            Assert.That(
                CameraEdgeSway.DefaultMaximumPitch,
                Is.GreaterThanOrEqualTo(CameraEdgeSway.LegacyMaximumPitch * 1.75f));
        }

        [Test]
        public void ThePointOfViewStartsMovingNearTheCentreOfTheScreen()
        {
            Assert.That(CameraEdgeSway.DefaultDeadZone, Is.LessThan(CameraEdgeSway.LegacyDeadZone * 0.5f));

            var slightlyOffCentre = CameraEdgeSway.EvaluatePointer(
                new Vector2(0.62f, 0.5f),
                CameraEdgeSway.DefaultDeadZone);
            var legacyAtTheSamePoint = CameraEdgeSway.EvaluatePointer(
                new Vector2(0.62f, 0.5f),
                CameraEdgeSway.LegacyDeadZone);

            Assert.That(slightlyOffCentre.x, Is.GreaterThan(0f), "A quarter of the way out must already lean the camera.");
            Assert.That(legacyAtTheSamePoint.x, Is.Zero, "The original framing stayed frozen this close to the centre.");
        }

        [Test]
        public void WidenedFramingKeepsTheFullEdgeTravelBounded()
        {
            var edge = CameraEdgeSway.EvaluatePointer(new Vector2(1f, 1f), CameraEdgeSway.DefaultDeadZone);

            Assert.That(edge.x, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(edge.y, Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
