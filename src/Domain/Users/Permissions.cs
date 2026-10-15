namespace Domain.Users;

public static class Permissions
{
    public const string UsersAccess = "users:access";

    public static readonly IReadOnlyList<string> DefaultForNewUsers = [UsersAccess];
}
