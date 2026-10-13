using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Racing.Application.Persistence;
using Racing.Simulation.Replay;

namespace Racing.Infrastructure.Persistence.Repositories;

public sealed class FileReplayRepository : IReplayRepository
{
    private readonly string _storageDir;
    private readonly ConcurrentDictionary<Guid, ReplayData> _memoryCache = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public FileReplayRepository(string? storageDir = null)
    {
        _storageDir = storageDir ?? Path.Combine(AppContext.BaseDirectory, "replays");
        if (!Directory.Exists(_storageDir))
        {
            Directory.CreateDirectory(_storageDir);
        }
    }

    public async Task SaveReplayAsync(ReplayData replay, CancellationToken ct = default)
    {
        _memoryCache[replay.Header.RaceId] = replay;

        try
        {
            string filePath = Path.Combine(_storageDir, $"{replay.Header.RaceId}.json");
            using var fileStream = File.Create(filePath);
            await JsonSerializer.SerializeAsync(fileStream, replay, JsonOptions, ct);
        }
        catch
        {
            // 파일 쓰기 실패해도 메모리 캐시에는 보존
        }
    }

    public async Task<ReplayData?> GetReplayAsync(Guid raceId, CancellationToken ct = default)
    {
        if (_memoryCache.TryGetValue(raceId, out var cached))
        {
            return cached;
        }

        string filePath = Path.Combine(_storageDir, $"{raceId}.json");
        if (File.Exists(filePath))
        {
            using var fileStream = File.OpenRead(filePath);
            var loaded = await JsonSerializer.DeserializeAsync<ReplayData>(fileStream, JsonOptions, ct);
            if (loaded != null)
            {
                _memoryCache[raceId] = loaded;
                return loaded;
            }
        }

        return null;
    }
}
