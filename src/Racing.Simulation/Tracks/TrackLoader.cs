using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Racing.Domain.Geometry;
using Racing.Domain.Tracks;

namespace Racing.Simulation.Tracks;

public interface ITrackLoader
{
    TrackDefinition LoadDefinition(string trackId);
    TrackRuntime LoadRuntime(string trackId);
}

public sealed class TrackLoader : ITrackLoader
{
    private readonly string _tracksDirectory;
    private readonly SplineTrackBuilder _splineBuilder;
    private readonly ConcurrentDictionary<string, TrackDefinition> _definitionCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, TrackRuntime> _runtimeCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public TrackLoader(string? tracksDirectory = null, SplineTrackBuilder? splineBuilder = null)
    {
        _tracksDirectory = tracksDirectory ?? FindTracksDirectory();
        _splineBuilder = splineBuilder ?? new SplineTrackBuilder();
    }

    public TrackDefinition LoadDefinition(string trackId)
    {
        return _definitionCache.GetOrAdd(trackId, id =>
        {
            string filePath = Path.Combine(_tracksDirectory, $"{id}.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                var loaded = JsonSerializer.Deserialize<TrackDefinition>(json, JsonOptions);
                if (loaded != null)
                {
                    return loaded;
                }
            }

            // 디스크 파일 부재 시 기본 내장 회로 폴백
            return GetBuiltinTrack(id);
        });
    }

    public TrackRuntime LoadRuntime(string trackId)
    {
        return _runtimeCache.GetOrAdd(trackId, id =>
        {
            var def = LoadDefinition(id);
            return _splineBuilder.Build(def);
        });
    }

    private static string FindTracksDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            string candidate = Path.Combine(current, "tracks");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Directory.GetParent(current)?.FullName;
            if (parent == null) break;
            current = parent;
        }

        return AppContext.BaseDirectory;
    }

    private static TrackDefinition GetBuiltinTrack(string id)
    {
        return new TrackDefinition
        {
            Id = id,
            Name = id == "beginner-circuit" ? "Beginner Circuit" : "Default Oval",
            IsLoop = true,
            DefaultLaps = 3,
            Points =
            [
                new TrackPoint(new Vector2(0f, 0f), 14f),
                new TrackPoint(new Vector2(60f, 0f), 14f),
                new TrackPoint(new Vector2(120f, 20f), 12f),
                new TrackPoint(new Vector2(140f, 70f), 12f),
                new TrackPoint(new Vector2(100f, 120f), 13f),
                new TrackPoint(new Vector2(30f, 110f), 14f),
                new TrackPoint(new Vector2(-30f, 60f), 13f)
            ]
        };
    }
}
