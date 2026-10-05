using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CaroGame.Server.Services;

public class TokenService
{
    private readonly ConcurrentDictionary<string, (Guid UserId, DateTime Expiry)> _tokens = new();

    public string CreateToken(Guid userId)
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(32);
        string token = Convert.ToHexString(bytes).ToLowerInvariant();
        _tokens[token] = (userId, DateTime.UtcNow.AddDays(30));
        return token;
    }

    public Guid? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        if (_tokens.TryGetValue(token, out var data))
        {
            if (data.Expiry > DateTime.UtcNow)
            {
                return data.UserId;
            }
            _tokens.TryRemove(token, out _);
        }
        return null;
    }

    public void InvalidateToken(string token)
    {
        _tokens.TryRemove(token, out _);
    }
}
