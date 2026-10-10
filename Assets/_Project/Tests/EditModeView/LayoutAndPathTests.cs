using NUnit.Framework;
using ParkSort.Core;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace ParkSort.View.Tests
{
    public class LayoutAndPathTests
    {
        private static BoardLayout Layout(int width, int height)
        {
            var layout = new BoardLayout();
            layout.Configure(new LevelDef { width = width, height = height, waitingSlots = 5 });
            return layout;
        }

        private static CarDef Car(int x, int y, Direction direction, int length) => new CarDef
        {
            x = (byte)x, y = (byte)y, direction = direction, length = (byte)length,
        };

        [Test]
        public void CarCenter_IsTheMiddleOfItsCells()
        {
            BoardLayout layout = Layout(4, 4);

            Vector3 center = layout.CarCenter(Car(0, 0, Direction.Right, 2));

            Assert.AreEqual(-1f, center.x, 0.001f);
            Assert.AreEqual(0.5f, center.z, 0.001f);
        }

        [Test]
        public void ExitPoint_IsOnTheRingInFrontOfTheCar()
        {
            BoardLayout layout = Layout(4, 4);

            Vector3 up = layout.ExitPoint(Car(1, 1, Direction.Up, 2));
            Vector3 left = layout.ExitPoint(Car(2, 2, Direction.Left, 2));

            Assert.AreEqual(layout.RingMax.y, up.z, 0.001f);
            Assert.AreEqual(layout.RingMin.x, left.x, 0.001f);
        }

        [Test]
        public void RingRoute_TakesTheShorterWayAround()
        {
            BoardLayout layout = Layout(6, 6);
            var start = new Vector3(layout.RingMax.x, 0f, 1f);
            var goal = new Vector3(2f, 0f, layout.RingMax.y);

            var path = new CarPath();
            path.Add(start);
            layout.AppendRingRoute(path, start, goal);
            path.Build();

            float direct = (layout.RingMax.y - 1f) + (layout.RingMax.x - 2f);
            Assert.Less(path.Length, direct + 0.01f);
            Assert.Greater(path.Length, direct - 1f);
        }

        [Test]
        public void RingRoute_FromBottomToTop_PassesTwoCorners()
        {
            BoardLayout layout = Layout(6, 6);
            var start = new Vector3(-2f, 0f, layout.RingMin.y);
            var goal = new Vector3(-2f, 0f, layout.RingMax.y);

            var path = new CarPath();
            path.Add(start);
            layout.AppendRingRoute(path, start, goal);
            path.Build();

            float height = layout.RingMax.y - layout.RingMin.y;
            float side = -2f - layout.RingMin.x;
            Assert.AreEqual(height + side * 2f, path.Length, 1f);
        }

        [Test]
        public void Path_StartsAndEndsAtItsEndPoints_AndFacesAlongTheWay()
        {
            var path = new CarPath();
            path.Add(new Vector3(0f, 0f, 0f));
            path.Add(new Vector3(0f, 0f, 4f));
            path.Add(new Vector3(3f, 0f, 4f));
            path.Build();

            path.Evaluate(0f, out Vector3 first, out Vector3 firstForward);
            path.Evaluate(path.Length, out Vector3 last, out Vector3 lastForward);

            Assert.Less(Vector3.Distance(first, Vector3.zero), 0.001f);
            Assert.Less(Vector3.Distance(last, new Vector3(3f, 0f, 4f)), 0.001f);
            Assert.Greater(firstForward.z, 0.99f);
            Assert.Greater(lastForward.x, 0.99f);
            Assert.Less(path.Length, 7f);
            Assert.Greater(path.Length, 6.5f);
        }

        [Test]
        public void Path_Evaluate_DoesNotAllocate()
        {
            var path = new CarPath();
            path.Add(new Vector3(0f, 0f, 0f));
            path.Add(new Vector3(0f, 0f, 4f));
            path.Add(new Vector3(3f, 0f, 4f));
            path.Build();

            Assert.That(() =>
            {
                for (float d = 0f; d < 7f; d += 0.1f) path.Evaluate(d, out _, out _);
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
