using Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddUserPermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create user_permissions table
        migrationBuilder.Sql($@"
            CREATE TABLE ""{Schemas.Default}"".user_permissions (
                user_id UUID NOT NULL,
                permission VARCHAR(100) NOT NULL,
                CONSTRAINT pk_user_permissions PRIMARY KEY (user_id, permission),
                CONSTRAINT fk_user_permissions_users_user_id FOREIGN KEY (user_id)
                    REFERENCES ""{Schemas.Default}"".users(id) ON DELETE CASCADE
            );");

        // Backfill existing users with default permission
        migrationBuilder.Sql($@"
            INSERT INTO ""{Schemas.Default}"".user_permissions (user_id, permission)
            SELECT id, 'users:access' FROM ""{Schemas.Default}"".users;");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql($@"DROP TABLE IF EXISTS ""{Schemas.Default}"".user_permissions;");
    }
}
