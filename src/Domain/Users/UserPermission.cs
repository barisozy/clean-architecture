namespace Domain.Users;

public sealed class UserPermission
{
    public Guid UserId { get; set; }
    public string Permission { get; set; }

    public static UserPermission Create(Guid userId, string permission) =>
        new()
        {
            UserId = userId,
            Permission = permission
        };
}
