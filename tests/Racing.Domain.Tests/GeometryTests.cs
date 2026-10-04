using System;
using Racing.Domain.Geometry;
using Xunit;

namespace Racing.Domain.Tests;

public class GeometryTests
{
    [Fact]
    public void Vector2_BasicArithmetic_ShouldBeCorrect()
    {
        var v1 = new Vector2(3f, 4f);
        var v2 = new Vector2(1f, 2f);

        Assert.Equal(new Vector2(4f, 6f), v1 + v2);
        Assert.Equal(new Vector2(2f, 2f), v1 - v2);
        Assert.Equal(5f, v1.Length());
        Assert.Equal(11f, Vector2.Dot(v1, v2));
        Assert.Equal(2f, Vector2.Cross(v1, v2));
    }

    [Fact]
    public void Vector2_Normalized_ShouldHaveUnitLength()
    {
        var v = new Vector2(10f, 0f);
        var normalized = v.Normalized();

        Assert.Equal(1f, normalized.Length(), 5);
        Assert.Equal(1f, normalized.X, 5);
        Assert.Equal(0f, normalized.Y, 5);
    }

    [Fact]
    public void Segment2D_ClosestPoint_ShouldClampCorrectly()
    {
        var seg = new Segment2D(new Vector2(0f, 0f), new Vector2(10f, 0f));

        var p1 = new Vector2(5f, 5f);
        Assert.Equal(new Vector2(5f, 0f), seg.ClosestPoint(p1));

        var p2 = new Vector2(-5f, 5f);
        Assert.Equal(new Vector2(0f, 0f), seg.ClosestPoint(p2));

        var p3 = new Vector2(15f, 5f);
        Assert.Equal(new Vector2(10f, 0f), seg.ClosestPoint(p3));
    }

    [Fact]
    public void Segment2D_Intersection_ShouldDetectCorrectly()
    {
        var s1 = new Segment2D(new Vector2(0f, 0f), new Vector2(10f, 10f));
        var s2 = new Segment2D(new Vector2(0f, 10f), new Vector2(10f, 0f));

        bool intersects = s1.Intersects(s2, out var intersection);
        Assert.True(intersects);
        Assert.Equal(5f, intersection.X, 3);
        Assert.Equal(5f, intersection.Y, 3);
    }

    [Fact]
    public void BoundingCircle_Intersects_ShouldDetectOverlap()
    {
        var c1 = new BoundingCircle(new Vector2(0f, 0f), 2f);
        var c2 = new BoundingCircle(new Vector2(3f, 0f), 2f);
        var c3 = new BoundingCircle(new Vector2(5f, 0f), 2f);

        Assert.True(c1.Intersects(c2));
        Assert.False(c1.Intersects(c3));
    }
}
