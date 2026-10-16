using Application.Users.Refresh;

namespace Application.UnitTests.Users;

public sealed class RefreshTokenReusePolicyTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 0, true)]    // grace disabled → always revoke
    [InlineData(30, 5, false)]  // inside the window → keep the family
    [InlineData(30, 30, false)] // exactly at the boundary → still inside
    [InlineData(30, 31, true)]  // after the window → revoke
    public void ShouldRevokeFamily_Should_RespectTheGraceWindow(int graceSeconds, int elapsedSeconds, bool expected)
    {
        // Arrange
        DateTime usedOnUtc = Now.AddSeconds(-elapsedSeconds);

        // Act
        bool shouldRevoke = RefreshTokenReusePolicy.ShouldRevokeFamily(usedOnUtc, Now, graceSeconds);

        // Assert
        shouldRevoke.ShouldBe(expected);
    }
}
