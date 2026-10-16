using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class NormalizeUserEmails : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Emails are stored trimmed and lowercased from this release on; normalize existing rows.
        // The unique index makes this fail loudly if two accounts collide once normalized, which
        // needs a manual decision instead of silently losing data.
        migrationBuilder.Sql($"""
            UPDATE "{Schemas.Default}".users
            SET email = lower(btrim(email))
            WHERE email <> lower(btrim(email));
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Not reversible: the original casing is not preserved.
    }
}
