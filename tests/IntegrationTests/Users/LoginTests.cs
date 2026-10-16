using System.Net;
using System.Net.Http.Json;
using System.Text;
using Shouldly;

namespace IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class LoginTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string Password = "Str0ng!Passw0rd#2026";

    [Fact]
    public async Task Login_Should_ReturnUnauthorized_ForUnknownEmailAndWrongPassword()
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
        unknownEmail.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("""{"email":null,"password":null}""")]
    [InlineData("""{"email":"","password":""}""")]
    [InlineData("""{"email":"not-an-email","password":"Str0ng!Passw0rd"}""")]
    public async Task Login_Should_ReturnBadRequest_WhenPayloadIsInvalid(string json)
    {
        // Act: must fail validation instead of reaching the password hasher.
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        HttpResponseMessage response = await HttpClient.PostAsync("users/login", content);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
