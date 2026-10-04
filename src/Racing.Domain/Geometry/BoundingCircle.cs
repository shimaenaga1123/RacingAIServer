namespace Racing.Domain.Geometry;

public readonly record struct BoundingCircle(Vector2 Center, float Radius)
{
    public bool Intersects(BoundingCircle other)
    {
        float combinedRadius = Radius + other.Radius;
        return Vector2.DistanceSquared(Center, other.Center) <= combinedRadius * combinedRadius;
    }

    public bool Contains(Vector2 point)
    {
        return Vector2.DistanceSquared(Center, point) <= Radius * Radius;
    }
}
