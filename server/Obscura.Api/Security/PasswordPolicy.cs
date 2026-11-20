using System.Text;

namespace Obscura.Api.Security;

// Password length rules (NIST SP 800-63B):
// 15-128 Unicode points after NKFC normalization
// No composition rules (no required digits or symbols)
// Spaces are valid chars
public static class PasswordPolicy
{
    public const int MinLength = 15;
    public const int MaxLength = 128;

    // Generous upper bound on the raw input, checked BEFORE normalizing
    // Thus, a huge input is rejected without spending CU on NKFC first!
    private const int MaxRawLength = 4 * MaxLength;

    public static bool HasValidLength(string password)
    {
        if (password.Length > MaxRawLength)
            return false;

        var codePoints = CountCodePoints(password);
        return codePoints is >= MinLength and <= MaxLength;
    }

    // Login only rejects input that can never be a stored password:
    // empty or over the maximum
    // No minimum here, so raising MinLength later cannot lock out existing users
    public static bool IsAcceptableForLogin(string password)
    {
        if (password.Length == 0 || password.Length > MaxRawLength)
            return false;

        return CountCodePoints(password) <= MaxLength;
    }

    private static int CountCodePoints(string password) =>
        password.Normalize(NormalizationForm.FormKC).EnumerateRunes().Count();
}