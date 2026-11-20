using Obscura.Api.Security;

namespace Obscura.Api.Auth;

// JSON body of POST /api/auth/register
// Nullable - a client can leave fields out
public sealed record RegisterRequest(string? Email, string? Password);

public static class RegisterValidator
{
    // Field name -> message:
    // the shape Results.ValidationProblem turns into a 400 ProblemDetails
    public static Dictionary<string, string[]> Validate(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(request.Email)
            || !EmailAddress.IsValid(EmailAddress.Normalize(request.Email)))
            errors["email"] = [$"Enter a valid email address (at most {EmailAddress.MaxLength} characters)."];

        if (string.IsNullOrEmpty(request.Password) || !PasswordPolicy.HasValidLength(request.Password))
            errors["password"] = [$"Use {PasswordPolicy.MinLength} to {PasswordPolicy.MaxLength} characters."];

        return errors;
    }
}