using Infrastructure.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Infrastructure.Database.Migrations;

/// <inheritdoc />
public partial class AddUserPermissions : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("public")
            .HasAnnotation("ProductVersion", "10.0.12")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        // ... existing entities (Users, RefreshTokens, TodoItems)

        modelBuilder.Entity<"Domain.Users.UserPermission", b =>
        {
            b.Property<Guid>("UserId")
                .HasColumnType("uuid")
                .HasColumnName("user_id");

            b.Property<string>("Permission")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("character varying(100)")
                .HasColumnName("permission");

            b.HasKey("UserId", "Permission")
                .HasName("pk_user_permissions");

            b.HasOne("Domain.Users.User", "User")
                .WithMany()
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_user_permissions_users_user_id");

            b.ToTable("user_permissions", "public");
        }
#pragma warning restore 612, 618
    }
}
