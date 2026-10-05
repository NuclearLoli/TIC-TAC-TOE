using System.Security.Cryptography;
using System.Text;

namespace CaroGame.Server.Services;

public class TokenService
{
    private static readonly byte[] SecretKey;

    static TokenService()
    {
        string keyPath = Path.Combine(AppContext.BaseDirectory, "server_secret.key");
        if (File.Exists(keyPath))
        {
            try
            {
                SecretKey = File.ReadAllBytes(keyPath);
            }
            catch
            {
                SecretKey = RandomNumberGenerator.GetBytes(64);
            }
        }
        else
        {
            SecretKey = RandomNumberGenerator.GetBytes(64);
            try
            {
                File.WriteAllBytes(keyPath, SecretKey);
            }
            catch { }
        }
    }

    public string CreateToken(Guid userId)
    {
        long expiry = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
        string payload = $"{userId:N}:{expiry}";
        string sig = ComputeSignature(payload);
        return $"{payload}:{sig}";
    }

    public Guid? ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        var parts = token.Split(':');
        if (parts.Length != 3) return null;

        string userIdStr = parts[0];
        string expiryStr = parts[1];
        string providedSig = parts[2];

        string payload = $"{userIdStr}:{expiryStr}";
        string expectedSig = ComputeSignature(payload);

        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedSig),
            Encoding.UTF8.GetBytes(expectedSig)))
        {
            return null;
        }

        if (!long.TryParse(expiryStr, out long expiry) || expiry < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return null;
        }

        if (!Guid.TryParseExact(userIdStr, "N", out var userId))
        {
            return null;
        }

        return userId;
    }

    public void InvalidateToken(string token)
    {
        // Stateless HMAC token expires automatically
    }

    private static string ComputeSignature(string data)
    {
        using var hmac = new HMACSHA256(SecretKey);
        byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
