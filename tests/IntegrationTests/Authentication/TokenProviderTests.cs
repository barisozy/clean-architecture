using Domain.Users;
using Infrastructure.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using SharedKernel;

namespace IntegrationTests.Authentication;

public sealed class TokenProviderTests
{
    private static readonly DateTime Now = new(2030, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Should_DeriveExpirationFromDateTimeProvider()
    {
        IOptions<JwtOptions> options = Options.Create(new JwtOptions
        {
            Secret = new string('k', 32),
            Issuer = "issuer",
            Audience = "audience",
            ExpirationInMinutes = 30
        });
        var provider = new TokenProvider(options, new FixedDateTimeProvider(Now));
        var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };

        string token = provider.Create(user);

        JsonWebToken jwt = new JsonWebTokenHandler().ReadJsonWebToken(token);
        jwt.ValidTo.ShouldBe(Now.AddMinutes(30));
    }

    private sealed class FixedDateTimeProvider(DateTime utcNow) : IDateTimeProvider
    {
        public DateTime UtcNow { get; } = utcNow;
    }
}
