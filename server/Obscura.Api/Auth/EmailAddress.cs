namespace Obscura.Api.Auth;

// Email rules:
// Normalized as trim + lowercase
// Then a structural check, max 254 chars
// The db enforces the same rules through users_email_normalized and 
// users_email_length
public static class EmailAddress
{
    public const int MaxLength = 254;

    public static string Normalize(string email) => email.Trim().ToLowerInvariant();

    // Structural only:
    // Exactly one '@', non-empty local part and domain, no whitespace or control chars
    // It does not prove the address exists
    public static bool IsValid(string normalized)
    {
        if (normalized.Length > MaxLength)
            return false;

        var at = normalized.IndexOf('@');
        if (at <= 0 || at == normalized.Length - 1 || normalized.IndexOf('@', at + 1) >= 0)
            return false;

        return !normalized.Any(c => char.IsWhiteSpace(c) || char.IsControl(c));
    }
}