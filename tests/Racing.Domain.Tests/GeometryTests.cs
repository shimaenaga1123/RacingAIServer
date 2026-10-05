using Racing.Domain.Geometry;

namespace Racing.Domain.Tests
{
    public class GeometryTests
    {
        [Fact]
        public void Vector2BasicArithmeticShouldBeCorrect()
        {
            Vector2 v1 = new(3f, 4f);
            Vector2 v2 = new(1f, 2f);

            Assert.Equal(new Vector2(4f, 6f), v1 + v2);
            Assert.Equal(new Vector2(2f, 2f), v1 - v2);
            Assert.Equal(5f, v1.Length());
            Assert.Equal(11f, Vector2.Dot(v1, v2));
            Assert.Equal(2f, Vector2.Cross(v1, v2));
        }

        [Fact]
        public void Vector2NormalizedShouldHaveUnitLength()
        {
            Vector2 v = new(10f, 0f);
            var normalized = v.Normalized();

            Assert.Equal(1f, normalized.Length(), 5);
            Assert.Equal(1f, normalized.X, 5);
            Assert.Equal(0f, normalized.Y, 5);
        }

        [Fact]
        public void Segment2DClosestPointShouldClampCorrectly()
        {
            Segment2D seg = new(new Vector2(0f, 0f), new Vector2(10f, 0f));

            Vector2 p1 = new(5f, 5f);
            Assert.Equal(new Vector2(5f, 0f), seg.ClosestPoint(p1));

            Vector2 p2 = new(-5f, 5f);
            Assert.Equal(new Vector2(0f, 0f), seg.ClosestPoint(p2));

            Vector2 p3 = new(15f, 5f);
            Assert.Equal(new Vector2(10f, 0f), seg.ClosestPoint(p3));
        }

        [Fact]
        public void Segment2DIntersectionShouldDetectCorrectly()
        {
            Segment2D s1 = new(new Vector2(0f, 0f), new Vector2(10f, 10f));
            Segment2D s2 = new(new Vector2(0f, 10f), new Vector2(10f, 0f));

            bool intersects = s1.Intersects(s2, out var intersection);
            Assert.True(intersects);
            Assert.Equal(5f, intersection.X, 3);
            Assert.Equal(5f, intersection.Y, 3);
        }

        [Fact]
        public void BoundingCircleIntersectsShouldDetectOverlap()
        {
            BoundingCircle c1 = new(new Vector2(0f, 0f), 2f);
            BoundingCircle c2 = new(new Vector2(3f, 0f), 2f);
            BoundingCircle c3 = new(new Vector2(5f, 0f), 2f);

            Assert.True(c1.Intersects(c2));
            Assert.False(c1.Intersects(c3));
        }
    }
}
