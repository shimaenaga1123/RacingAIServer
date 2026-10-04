using System;
using System.Security.Cryptography;

namespace Racing.Application.Competitions;

public static class JoinCodeGenerator
{
    private const string Digits = "0123456789";

    public static string Generate(int length = 6)
    {
        Span<char> result = stackalloc char[length];
        Span<byte> randomBytes = stackalloc byte[length];
        RandomNumberGenerator.Fill(randomBytes);

        for (int i = 0; i < length; i++)
        {
            result[i] = Digits[randomBytes[i] % Digits.Length];
        }

        return new string(result);
    }
}
