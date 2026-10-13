using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Authentication;

internal sealed class JwtOptionsValidator(IHostEnvironment environment) : IValidateOptions<JwtOptions>
{
    private const int MinSecretBytes = 32;
    private const int MaxExpirationInMinutes = 1440;
    private const string DevelopmentSecretPrefix = "dev-only-";
    private const string PlaceholderSecret = "set-in-user-secrets-or-environment";

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        List<string> failures = [];

        if (Encoding.UTF8.GetByteCount(options.Secret) < MinSecretBytes)
        {
            failures.Add($"Jwt:Secret must be at least {MinSecretBytes} bytes.");
        }

        if (string.Equals(options.Secret, PlaceholderSecret, StringComparison.OrdinalIgnoreCase))
        {
            failures.Add("Jwt:Secret must be replaced with a configured secret value.");
        }

        if (!environment.IsDevelopment() &&
            options.Secret.StartsWith(DevelopmentSecretPrefix, StringComparison.Ordinal))
        {
            failures.Add("Jwt:Secret must not use the development-only value outside the Development environment.");
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            failures.Add("Jwt:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            failures.Add("Jwt:Audience is required.");
        }

        if (options.ExpirationInMinutes is < 1 or > MaxExpirationInMinutes)
        {
            failures.Add($"Jwt:ExpirationInMinutes must be between 1 and {MaxExpirationInMinutes}.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
