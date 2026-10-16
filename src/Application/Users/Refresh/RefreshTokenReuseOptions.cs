namespace Application.Users.Refresh;

public sealed class RefreshTokenReuseOptions
{
    public const string SectionName = "RefreshTokenReuse";

    /// <summary>
    /// A client retry or a second tab can replay a just-rotated refresh token. Inside this
    /// window the replayed token is still rejected, but the whole family is not revoked.
    /// 0 disables the window and keeps the strict behavior.
    /// </summary>
    public int ReuseGraceSeconds { get; set; }
}
