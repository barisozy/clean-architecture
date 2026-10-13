using System;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Database.Migrations;

    /// <inheritdoc />
    public partial class RefreshTokenHashAndFamily : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Plain-text tokens cannot be converted to hashes: revoke every existing session.
            migrationBuilder.Sql($"DELETE FROM \"{Schemas.Default}\".refresh_tokens;");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "token",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_on_utc",
                schema: "public",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "family_id",
                schema: "public",
                table: "refresh_tokens",
                type: "uuid",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<DateTime>(
                name: "revoked_on_utc",
                schema: "public",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "token_hash",
                schema: "public",
                table: "refresh_tokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "used_on_utc",
                schema: "public",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "public",
                table: "refresh_tokens",
                column: "family_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_family_id",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "ix_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "created_on_utc",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "family_id",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "revoked_on_utc",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "token_hash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "used_on_utc",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<string>(
                name: "token",
                schema: "public",
                table: "refresh_tokens",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                schema: "public",
                table: "refresh_tokens",
                column: "token",
                unique: true);
        }
    }
