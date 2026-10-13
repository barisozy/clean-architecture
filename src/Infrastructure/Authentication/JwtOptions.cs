using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Authentication;

internal sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public const string SigningAlgorithm = SecurityAlgorithms.HmacSha256;

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int ExpirationInMinutes { get; set; }
}
