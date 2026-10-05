using Racing.Domain.Geometry;
using Racing.Domain.Tracks;
using Racing.Simulation.Laps;
using Racing.Simulation.Tracks;

namespace Racing.Simulation.Tests
{
    public class LapTrackerTests
    {
        [Fact]
        public void CheckpointsSequentialCrossingShouldAdvanceCheckpoint()
        {
            TrackDefinition trackDef = new()
            {
                Id = "loop",
                Name = "Loop",
                IsLoop = true,
                DefaultLaps = 2,
                Points =
                [
                    new(new Vector2(0f, 0f), 10f),
                    new(new Vector2(100f, 0f), 10f),
                    new(new Vector2(100f, 100f), 10f),
                    new(new Vector2(0f, 100f), 10f)
                ]
            };

            var track = new SplineTrackBuilder(targetSampleStep: 1f, checkpointCount: 4).Build(trackDef);
            LapTracker tracker = new(track, totalLaps: 2);
            DriverProgress progress = new()
            {
                CurrentLap = 1,
                NextCheckpointIndex = 1 // 1번 체크포인트를 향해 달림
            };

            var targetCp = track.Checkpoints[1];
            // 체크포인트 라인을 가로지르는 이동
            var normal = (targetCp.Line.End - targetCp.Line.Start).Normalized();
            Vector2 perpendicular = new(-normal.Y, normal.X);

            var pBefore = targetCp.Center - (perpendicular * 2f);
            var pAfter = targetCp.Center + (perpendicular * 2f);

            TrackPosition trackPos = new(targetCp.DistanceAlongTrack + 1f, 0f, 0f, 10f, true);

            tracker.UpdateProgress(progress, pBefore, pAfter, trackPos, dt: 1f / 60f);

            Assert.Equal(2, progress.NextCheckpointIndex);
        }
    }
}
