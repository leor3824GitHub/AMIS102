using System.ComponentModel.DataAnnotations;

namespace AMIS.Modules.Identity.Authorization.Jwt;

public class JwtOptions : IValidatableObject
{
    // The placeholder shipped in appsettings templates — must never be used as a real key.
    public const string PlaceholderSigningKey = "replace-with-256-bit-secret-min-32-chars";

    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public string SigningKey { get; init; } = string.Empty;
    public int AccessTokenMinutes { get; init; } = 30;
    public int RefreshTokenDays { get; init; } = 7;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrEmpty(SigningKey))
        {
            yield return new ValidationResult("No Key defined in JwtOptions config", [nameof(SigningKey)]);
        }

        if (!string.IsNullOrEmpty(SigningKey) && SigningKey.Length < 32)
        {
            yield return new ValidationResult("SigningKey must be at least 32 characters long.", [nameof(SigningKey)]);
        }

        if (string.Equals(SigningKey, PlaceholderSigningKey, StringComparison.Ordinal))
        {
            yield return new ValidationResult(
                "SigningKey is still the template placeholder. Set a real 256-bit secret via user-secrets, environment variables, or a secret store.",
                [nameof(SigningKey)]);
        }

        if (string.IsNullOrEmpty(Issuer))
        {
            yield return new ValidationResult("No Issuer defined in JwtOptions config", [nameof(Issuer)]);
        }

        if (string.IsNullOrEmpty(Audience))
        {
            yield return new ValidationResult("No Audience defined in JwtOptions config", [nameof(Audience)]);
        }
    }
}

