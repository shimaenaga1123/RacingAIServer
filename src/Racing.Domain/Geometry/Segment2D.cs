using System;

namespace Racing.Domain.Geometry;

public readonly record struct Segment2D(Vector2 Start, Vector2 End)
{
    public Vector2 Direction => (End - Start).Normalized();
    public float Length => Vector2.Distance(Start, End);

    public Vector2 ClosestPoint(Vector2 point)
    {
        Vector2 ab = End - Start;
        float abLenSq = ab.LengthSquared();
        if (abLenSq < 1e-6f) return Start;

        float t = Vector2.Dot(point - Start, ab) / abLenSq;
        t = Math.Clamp(t, 0f, 1f);
        return Start + ab * t;
    }

    public float DistanceTo(Vector2 point)
    {
        Vector2 closest = ClosestPoint(point);
        return Vector2.Distance(point, closest);
    }

    public bool Intersects(Segment2D other, out Vector2 intersection)
    {
        intersection = Vector2.Zero;

        Vector2 p = Start;
        Vector2 r = End - Start;
        Vector2 q = other.Start;
        Vector2 s = other.End - other.Start;

        float rCrossS = Vector2.Cross(r, s);
        Vector2 qMinusP = q - p;

        if (MathF.Abs(rCrossS) < 1e-6f)
        {
            return false; // 평행이거나 일직선
        }

        float t = Vector2.Cross(qMinusP, s) / rCrossS;
        float u = Vector2.Cross(qMinusP, r) / rCrossS;

        if (t >= 0f && t <= 1f && u >= 0f && u <= 1f)
        {
            intersection = p + r * t;
            return true;
        }

        return false;
    }
}
