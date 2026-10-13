using System.Security.Claims;

namespace Infrastructure.Authentication;

internal static class ClaimsPrincipalExtensions
{
    public static bool TryGetUserId(this ClaimsPrincipal? principal, out Guid userId) =>
        Guid.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    public static Guid GetUserId(this ClaimsPrincipal? principal) =>
        principal.TryGetUserId(out Guid userId)
            ? userId
            : throw new ApplicationException("User id is unavailable");
}
