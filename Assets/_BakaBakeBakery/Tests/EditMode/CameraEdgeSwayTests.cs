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
    }
}
