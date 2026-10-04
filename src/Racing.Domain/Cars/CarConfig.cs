namespace Racing.Domain.Cars;

public sealed record CarConfig
{
    public float Mass { get; init; } = 1000f;              // kg
    public float MaxEngineForce { get; init; } = 6000f;     // N
    public float MaxBrakeForce { get; init; } = 9000f;      // N
    public float DragCoefficient { get; init; } = 0.35f;    // 공기 저항
    public float RollingResistance { get; init; } = 15f;    // 구름 저항
    public float CorneringStiffness { get; init; } = 8.0f;  // 횡방향 그립
    public float SteeringSpeed { get; init; } = 2.5f;       // rad/s 스티어링 회전율
    public float MaxSteeringAngle { get; init; } = 0.6f;    // 약 34도
    public float CollisionRadius { get; init; } = 1.2f;     // 충돌 반경(m)
    public float OffTrackFrictionMultiplier { get; init; } = 4.0f; // 트랙 이탈 시 감속 배율
}
