using Shouldly;

namespace IntegrationTests.Logging;

public sealed class CorrelationIdTests : IClassFixture<ApiFactory>
{
    private const string HeaderName = "Correlation-Id";
    private const int MaxLength = 64;

    private readonly ApiFactory _factory;

    public CorrelationIdTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ValidCorrelationId_Should_BeEchoedBack()
    {
        string response = await SendAsync("req-2026.10_A:7");

        response.ShouldBe("req-2026.10_A:7");
    }

    [Fact]
    public async Task MissingCorrelationId_Should_BeReplacedWithGeneratedValue()
    {
        string response = await SendAsync(null);

        response.ShouldNotBeNullOrWhiteSpace();
        response.Length.ShouldBeLessThanOrEqualTo(MaxLength);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("semi;colon")]
    [InlineData("<script>alert(1)</script>")]
    public async Task CorrelationIdWithUnsafeCharacters_Should_BeReplaced(string supplied)
    {
        string response = await SendAsync(supplied);

        response.ShouldNotBe(supplied);
        response.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task OversizedCorrelationId_Should_BeReplaced()
    {
        string supplied = new('a', MaxLength + 1);

        string response = await SendAsync(supplied);

        response.ShouldNotBe(supplied);
        response.Length.ShouldBeLessThanOrEqualTo(MaxLength);
    }

    private async Task<string> SendAsync(string? correlationId)
    {
        HttpClient client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, "health/live");

        if (correlationId is not null)
        {
            request.Headers.TryAddWithoutValidation(HeaderName, correlationId);
        }

        using HttpResponseMessage response = await client.SendAsync(request);

        response.Headers.TryGetValues(HeaderName, out IEnumerable<string>? values).ShouldBeTrue();

        return values.Single();
    }
}
