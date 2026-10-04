using System;
using Racing.Domain.Cars;
using Racing.Domain.Geometry;

namespace Racing.Simulation.Physics;

public sealed class ArcadePhysicsEngine : IPhysicsEngine
{
    public void Step(Car car, float dt, bool isOnTrack)
    {
        if (dt <= 0f) return;

        var config = car.Config;

        // 1. 현재 로컬 방향 벡터
        Vector2 forward = car.Forward;
        Vector2 right = car.Right;

        // 2. 속도 벡터의 로컬 분해 (종방향 속도 forwardSpeed, 횡방향 속도 lateralSpeed)
        float forwardSpeed = Vector2.Dot(car.Velocity, forward);
        float lateralSpeed = Vector2.Dot(car.Velocity, right);

        // 3. 엔진 구동력 및 제동력
        float driveForce = car.Throttle * config.MaxEngineForce;
        float brakeForce = 0f;
        if (car.Brake > 0f)
        {
            float brakeDir = forwardSpeed > 0.05f ? -1f : (forwardSpeed < -0.05f ? 1f : 0f);
            brakeForce = brakeDir * car.Brake * config.MaxBrakeForce;
        }

        // 4. 공기 및 구름 저항
        float drag = 0.5f * config.DragCoefficient * forwardSpeed * MathF.Abs(forwardSpeed);
        float rolling = config.RollingResistance * forwardSpeed;

        // 트랙 이탈 시 저항 대폭 증가
        float surfaceMultiplier = isOnTrack ? 1.0f : config.OffTrackFrictionMultiplier;
        float totalResistance = (drag + rolling) * surfaceMultiplier;

        float netForwardForce = driveForce + brakeForce - totalResistance;
        float forwardAcceleration = netForwardForce / config.Mass;

        // 종방향 속도 업데이트
        forwardSpeed += forwardAcceleration * dt;

        // 브레이크 시 속도가 0 근처에서 반대로 튀지 않도록 클램핑
        if (car.Brake > 0f && MathF.Abs(forwardSpeed) < 0.2f && car.Throttle <= 0f)
        {
            forwardSpeed = 0f;
        }

        // 5. 조향 및 각속도 (Speed-dependent steering)
        float speedNormalized = Math.Clamp(MathF.Abs(forwardSpeed) / 10f, 0f, 1f);
        float targetYawRate = car.Steering * config.MaxSteeringAngle * config.SteeringSpeed * speedNormalized;

        // 후진 중일 경우 스티어링 반전
        if (forwardSpeed < -0.1f)
        {
            targetYawRate = -targetYawRate;
        }

        car.AngularVelocity = targetYawRate;
        car.Rotation = NormalizeAngle(car.Rotation + car.AngularVelocity * dt);

        // 6. 횡방향 그립 (타이어 횡슬립 감쇠)
        // 온트랙일수록 빠르게 옆으로 미끄러지는 속도를 잡음
        float lateralGripFactor = isOnTrack ? config.CorneringStiffness : (config.CorneringStiffness * 0.2f);
        lateralSpeed = MathF.Max(0f, 1f - lateralGripFactor * dt) * lateralSpeed;

        // 7. 새로운 월드 속도 및 위치 계산
        car.Velocity = forward * forwardSpeed + right * lateralSpeed;
        car.Position += car.Velocity * dt;
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }
}
