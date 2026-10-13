using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Users;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(refreshToken => refreshToken.Id);

        // SHA-256 as upper-case hex = 64 characters.
        builder.Property(refreshToken => refreshToken.TokenHash).HasMaxLength(64).IsRequired();

        builder.HasIndex(refreshToken => refreshToken.TokenHash).IsUnique();

        builder.HasIndex(refreshToken => refreshToken.FamilyId);

        builder.HasOne(refreshToken => refreshToken.User)
            .WithMany()
            .HasForeignKey(refreshToken => refreshToken.UserId);
    }
}
