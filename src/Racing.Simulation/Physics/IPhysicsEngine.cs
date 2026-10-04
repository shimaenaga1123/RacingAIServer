using Racing.Domain.Cars;

namespace Racing.Simulation.Physics;

public interface IPhysicsEngine
{
    void Step(Car car, float dt, bool isOnTrack);
}
