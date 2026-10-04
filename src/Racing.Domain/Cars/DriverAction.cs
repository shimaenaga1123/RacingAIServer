using System;

namespace Racing.Domain.Cars;

public readonly record struct DriverAction(float Steering, float Throttle, float Brake)
{
    public static readonly DriverAction None = new(0f, 0f, 0f);

    public DriverAction Clamped()
    {
        return new DriverAction(
            Math.Clamp(Steering, -1f, 1f),
            Math.Clamp(Throttle, 0f, 1f),
            Math.Clamp(Brake, 0f, 1f)
        );
    }
}
