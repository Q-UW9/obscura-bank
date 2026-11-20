using Obscura.Api.Security;

namespace Obscura.Api.Auth;

// JSON body of POST /api/auth/login
public sealed record LoginRequest(string? Email, string? Password);

// Filters out input that can never succeed
// Returns a bool, not field errors:
// every login failure collapses into the same 401, so there is 
// nothing to report per field
public static class LoginValidator
{
    public static bool IsAcceptable(LoginRequest request) =>
        !string.IsNullOrEmpty(request.Email)
        && EmailAddress.Normalize(request.Email).Length <= EmailAddress.MaxLength
        && request.Password is not null 
        && PasswordPolicy.IsAcceptableForLogin(request.Password);
}