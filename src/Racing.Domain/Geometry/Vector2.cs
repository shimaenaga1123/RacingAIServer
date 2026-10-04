using System;

namespace Racing.Domain.Geometry;

public readonly record struct Vector2(float X, float Y)
{
    public static readonly Vector2 Zero = new(0f, 0f);
    public static readonly Vector2 UnitX = new(1f, 0f);
    public static readonly Vector2 UnitY = new(0f, 1f);

    public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);
    public static Vector2 operator *(Vector2 v, float s) => new(v.X * s, v.Y * s);
    public static Vector2 operator *(float s, Vector2 v) => new(v.X * s, v.Y * s);
    public static Vector2 operator /(Vector2 v, float s) => new(v.X / s, v.Y / s);

    public float LengthSquared() => X * X + Y * Y;
    public float Length() => MathF.Sqrt(LengthSquared());

    public Vector2 Normalized()
    {
        float len = Length();
        return len > 1e-6f ? this / len : Zero;
    }

    public static float Dot(Vector2 a, Vector2 b) => a.X * b.X + a.Y * b.Y;
    public static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
    public static float Distance(Vector2 a, Vector2 b) => (a - b).Length();
    public static float DistanceSquared(Vector2 a, Vector2 b) => (a - b).LengthSquared();

    public static Vector2 Lerp(Vector2 a, Vector2 b, float t) =>
        new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);

    public Vector2 Rotate(float radians)
    {
        float cos = MathF.Cos(radians);
        float sin = MathF.Sin(radians);
        return new Vector2(X * cos - Y * sin, X * sin + Y * cos);
    }

    public float Angle() => MathF.Atan2(Y, X);

    public static Vector2 FromAngle(float radians) =>
        new(MathF.Cos(radians), MathF.Sin(radians));
}
