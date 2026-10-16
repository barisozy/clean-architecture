using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Todos;
using Domain.Users;

namespace Application.UnitTests.Abstractions;

/// <summary>
/// A lightweight in-memory <see cref="DbContext"/> that implements <see cref="IApplicationDbContext"/>
/// so Application handlers can be unit tested without referencing the Infrastructure layer.
/// </summary>
public class TestDbContext(DbContextOptions<TestDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<User> Users { get; set; }

    public DbSet<RefreshToken> RefreshTokens { get; set; }

    public DbSet<UserPermission> UserPermissions { get; set; }

    public DbSet<TodoItem> TodoItems { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserPermission compound key for in-memory provider validation
        modelBuilder.Entity<UserPermission>(e => e.HasKey(x => new { x.UserId, x.Permission }));
    }
}
