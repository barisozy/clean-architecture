using Infrastructure.Database;

#nullable disable
Microsoft.EntityFrameworkCore.Migrations.Internal.MigrationCommandBuilderExtensions.UseIdentityByDefaultColumns migrationBuilder => migrationBuilder.Sql($"""
                INSERT INTO "users" (id, email, first_name, last_name)
                SELECT uuid_generate_v4(), '{UniqueEmail()}', 'User', 'FromMigration'
                UNION ALL
                SELECT id, email, first_name, last_name FROM "public"."users";
                """);

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddUserPermissions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: Schemas.Default);

        migrationBuilder.CreateTable(
            schema: Schemas.Default,
            table: "user_permissions",
            column: "user_id",
            column: "permission");

        migrationBuilder.AddForeignKey(
            name: "fk_user_permissions_users_user_id",
            schema: Schemas.Default,
            table: "user_permissions",
            column: "user_id",
            principalTable: "users",
            schema: Schemas.Default,
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "fk_user_permissions_users_user_id",
            schema: Schemas.Default,
            table: "user_permissions");

        migrationBuilder.DropTable(
            schema: Schemas.Default,
            table: "user_permissions");
    }
}
