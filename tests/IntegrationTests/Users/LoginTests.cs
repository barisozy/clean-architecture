using System.Net;
using System.Net.Http.Json;
using Shouldly;

namespace IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LoginTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string Password = "Str0ng!Passw0rd#2026";

    [Fact]
    public async Task Login_Should_ReturnSameStatus_ForUnknownEmailAndWrongPassword()
    {
        string email = $"{Guid.NewGuid():N}@example.com";

        (await HttpClient.PostAsJsonAsync(
            "users/register",
            new { email, firstName = "Test", lastName = "User", password = Password }))
            .EnsureSuccessStatusCode();

        HttpResponseMessage wrongPassword = await HttpClient.PostAsJsonAsync(
            "users/login",
            new { email, password = "Wr0ng!Passw0rd#2026" });

        HttpResponseMessage unknownEmail = await HttpClient.PostAsJsonAsync(
            "users/login",
            new { email = $"{Guid.NewGuid():N}@example.com", password = Password });

        unknownEmail.StatusCode.ShouldBe(wrongPassword.StatusCode);
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
