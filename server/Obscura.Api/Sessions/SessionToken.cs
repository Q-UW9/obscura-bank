using System.Buffers.Text;
using System.Security.Cryptography;

namespace Obscura.Api.Sessions;

// Session tokens - E2
// 256 random bits in the cookie
// Only their SHA-256 is ever stored
public static class SessionToken
{
    private const int TokenSize = 32; // in bytes, so 256 bits

    // A new token, base64-url encoded (43 chars, with no padding)
    // Safe in a cookie without escaping
    public static string Create() =>
        Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenSize));

    // SHA-256 of a token as sent by the browser or null if it is not a well-formed
    // token
    // Garbage, such as wrong length or invalid chars, is rejected here and never
    // reaches the database
    public static byte[]? TryHash(string? token)
    {
        if (string.IsNullOrEmpty(token)
            || !Base64Url.IsValid(token, out var length)
            || length != TokenSize)
            return null;

        return SHA256.HashData(Base64Url.DecodeFromChars(token));
    }
}