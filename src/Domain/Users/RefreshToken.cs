namespace Domain.Users;

public sealed class RefreshToken
{
    public const int LifetimeInDays = 7;

    public Guid Id { get; set; }
    public string TokenHash { get; set; }
    public Guid FamilyId { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime ExpiresOnUtc { get; set; }
    public DateTime? UsedOnUtc { get; set; }
    public DateTime? RevokedOnUtc { get; set; }
    public User User { get; set; }

    public static RefreshToken Create(Guid userId, Guid familyId, string tokenHash, DateTime utcNow) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FamilyId = familyId,
            TokenHash = tokenHash,
            CreatedOnUtc = utcNow,
            ExpiresOnUtc = utcNow.AddDays(LifetimeInDays)
        };
}
