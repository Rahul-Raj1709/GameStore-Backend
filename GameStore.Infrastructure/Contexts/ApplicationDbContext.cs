using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Infrastructure.Contexts;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Genres
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = 1, Name = "Fighting" },
            new Genre { Id = 2, Name = "Roleplaying" },
            new Genre { Id = 3, Name = "Sports" },
            new Genre { Id = 4, Name = "Racing" },
            new Genre { Id = 5, Name = "Kids and Family" }
        );

        // Configure Many-to-Many
        modelBuilder.Entity<User>()
            .HasMany(u => u.LikedGames)
            .WithMany(g => g.LikedByUsers)
            .UsingEntity(j => j.ToTable("LikedGames"));

        // NEW: Configure Ownership (One-to-Many)
        modelBuilder.Entity<Game>()
            .HasOne(g => g.Owner)
            .WithMany(u => u.OwnedGames)
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.Restrict); // Prevents deleting an Admin from wiping out all their games automatically
    }
}