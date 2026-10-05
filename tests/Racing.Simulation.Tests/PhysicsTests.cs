using Racing.Domain.Cars;
using Racing.Domain.Geometry;
using Racing.Simulation.Physics;

namespace Racing.Simulation.Tests
{
    public class PhysicsTests
    {
        private readonly ArcadePhysicsEngine _engine = new();

        [Fact]
        public void ThrottleShouldAccelerateCarForward()
        {
            Car car = new(Vector2.Zero, 0f); // 0도 = UnitX 방향
            car.ApplyAction(new DriverAction(0f, 1f, 0f));

            const float dt = 1f / 60f;
            for (int i = 0; i < 60; i++) // 1초 가속
            {
                _engine.Step(car, dt, isOnTrack: true);
            }

            Assert.True(car.Speed > 4f);
            Assert.True(car.Position.X > 1f);
            Assert.Equal(0f, car.Position.Y, 2);
        }

        [Fact]
        public void BrakeShouldDecelerateCar()
        {
            Car car = new(Vector2.Zero, 0f);
            car.ApplyAction(new DriverAction(0f, 1f, 0f));

            const float dt = 1f / 60f;
            for (int i = 0; i < 60; i++)
            {
                _engine.Step(car, dt, isOnTrack: true);
            }

            float topSpeed = car.Speed;

            // 브레이크 작동
            car.ApplyAction(new DriverAction(0f, 0f, 1f));
            for (int i = 0; i < 30; i++)
            {
                _engine.Step(car, dt, isOnTrack: true);
            }

            Assert.True(car.Speed < topSpeed);
        }

        [Fact]
        public void OffTrackShouldHaveHigherResistance()
        {
            Car carOnTrack = new(Vector2.Zero, 0f);
            Car carOffTrack = new(Vector2.Zero, 0f);

            carOnTrack.ApplyAction(new DriverAction(0f, 1f, 0f));
            carOffTrack.ApplyAction(new DriverAction(0f, 1f, 0f));

            const float dt = 1f / 60f;
            for (int i = 0; i < 60; i++)
            {
                _engine.Step(carOnTrack, dt, isOnTrack: true);
                _engine.Step(carOffTrack, dt, isOnTrack: false);
            }

            Assert.True(carOnTrack.Speed > carOffTrack.Speed);
            Assert.True(carOnTrack.Position.X > carOffTrack.Position.X);
        }

        [Fact]
        public void SteeringShouldChangeRotation()
        {
            Car car = new(Vector2.Zero, 0f);
            car.ApplyAction(new DriverAction(Steering: 1f, Throttle: 1f, Brake: 0f));

            const float dt = 1f / 60f;
            for (int i = 0; i < 60; i++)
            {
                _engine.Step(car, dt, isOnTrack: true);
            }

            Assert.NotEqual(0f, car.Rotation);
            Assert.NotEqual(0f, car.Position.Y);
        }
    }
}
