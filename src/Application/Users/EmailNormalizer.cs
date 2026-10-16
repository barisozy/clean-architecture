namespace Application.Users;

internal static class EmailNormalizer
{
#pragma warning disable CA1308 // Lower case is the convention for email addresses; storage and lookups rely on it.
    public static string Normalize(string email) =>
        email.Trim().ToLowerInvariant();
#pragma warning restore CA1308
}
