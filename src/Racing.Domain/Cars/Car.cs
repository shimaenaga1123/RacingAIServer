using Racing.Domain.Geometry;

namespace Racing.Domain.Cars;

public sealed class Car
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public float Rotation { get; set; } // 라디안
    public float AngularVelocity { get; set; }

    public float Steering { get; private set; }
    public float Throttle { get; private set; }
    public float Brake { get; private set; }

    public CarConfig Config { get; }

    public float Speed => Velocity.Length();
    public Vector2 Forward => new(System.MathF.Cos(Rotation), System.MathF.Sin(Rotation));
    public Vector2 Right => new(-System.MathF.Sin(Rotation), System.MathF.Cos(Rotation));
    public BoundingCircle Bounds => new(Position, Config.CollisionRadius);

    public Car(Vector2 initialPosition, float initialRotation, CarConfig? config = null)
    {
        Position = initialPosition;
        Velocity = Vector2.Zero;
        Rotation = initialRotation;
        AngularVelocity = 0f;
        Config = config ?? new CarConfig();
    }

    public void ApplyAction(DriverAction action)
    {
        var clamped = action.Clamped();
        Steering = clamped.Steering;
        Throttle = clamped.Throttle;
        Brake = clamped.Brake;
    }

    public void Teleport(Vector2 position, float rotation)
    {
        Position = position;
        Velocity = Vector2.Zero;
        Rotation = rotation;
        AngularVelocity = 0f;
        Steering = 0f;
        Throttle = 0f;
        Brake = 0f;
    }
}
