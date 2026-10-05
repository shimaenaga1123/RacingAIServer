using System;
using Racing.Domain.Cars;
using Racing.Simulation.Observations;

namespace Racing.Simulation.Agents;

public interface ILocalAgent
{
    DriverAction Act(Observation observation);
}

public sealed class ConstantAgent(float steering = 0f, float throttle = 1f, float brake = 0f) : ILocalAgent
{
    private readonly float _steering = steering;
    private readonly float _throttle = throttle;
    private readonly float _brake = brake;

    public DriverAction Act(Observation observation)
    {
        return new DriverAction(_steering, _throttle, _brake);
    }
}

public sealed class RandomAgent(int seed = 42) : ILocalAgent
{
    private readonly Random _random = new Random(seed);

    public DriverAction Act(Observation observation)
    {
        float steering = (float)(_random.NextDouble() * 2.0 - 1.0) * 0.3f;
        float throttle = (float)(_random.NextDouble() * 0.5 + 0.5);
        return new DriverAction(steering, throttle, 0f);
    }
}

public sealed class FollowTrackAgent(float kpHeading = 1.2f, float kpLateral = 0.25f, float targetSpeed = 16f) : ILocalAgent
{
    private readonly float _kpHeading = kpHeading;
    private readonly float _kpLateral = kpLateral;
    private readonly float _targetSpeed = targetSpeed;

    public DriverAction Act(Observation obs)
    {
        // 1. 스티어링: 헤딩 오차와 횡방향 오프셋 보정
        // HeadingError가 양수면 시계 방향(우측)으로 꺾여 있으므로 좌측(-Steering)으로 보정
        // LateralOffset이 양수면 트랙 우측에 치우쳐 있으므로 좌측(-Steering)으로 보정
        float steering = -(_kpHeading * obs.Track.HeadingError + _kpLateral * obs.Track.LateralOffset);

        // 2. 스로틀 및 브레이크: 목표 속도 유지 및 전방 곡률에 따른 감속
        float currentSpeed = obs.Player.Speed;
        float effectiveTargetSpeed = _targetSpeed;

        // 전방에 급커브가 있으면 속도 하향
        if (obs.Track.AheadCurvatures.Count > 0)
        {
            float nearCurvature = MathF.Abs(obs.Track.AheadCurvatures[0]);
            if (nearCurvature > 0.05f)
            {
                effectiveTargetSpeed *= 0.6f;
            }
        }

        float throttle = 0f;
        float brake = 0f;

        if (currentSpeed < effectiveTargetSpeed)
        {
            throttle = Math.Clamp((effectiveTargetSpeed - currentSpeed) / 5f, 0.2f, 1f);
        }
        else
        {
            brake = Math.Clamp((currentSpeed - effectiveTargetSpeed) / 5f, 0.1f, 0.8f);
        }

        return new DriverAction(steering, throttle, brake);
    }
}
