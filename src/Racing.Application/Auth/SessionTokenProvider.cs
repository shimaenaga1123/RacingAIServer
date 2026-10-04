using System;
using System.Security.Cryptography;
using System.Text;
using Racing.Domain.Auth;

namespace Racing.Application.Auth;

public sealed class SessionTokenProvider
{
    public string GenerateToken(Guid participantId)
    {
        byte[] randomBytes = new byte[16];
        RandomNumberGenerator.Fill(randomBytes);
        string randomPart = Convert.ToHexString(randomBytes).ToLowerInvariant();
        return $"rse_{participantId:N}_{randomPart}";
    }

    public string ComputeHash(string token)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(token);
        byte[] hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public AgentSession CreateSession(Guid participantId, out string rawToken, TimeSpan? duration = null)
    {
        rawToken = GenerateToken(participantId);
        string hash = ComputeHash(rawToken);
        return new AgentSession(participantId, hash, duration ?? TimeSpan.FromHours(4));
    }
}
