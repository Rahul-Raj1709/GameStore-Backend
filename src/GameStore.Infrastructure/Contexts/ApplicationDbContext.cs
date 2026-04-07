using GameStore.Application.Interfaces;
using GameStore.Domain.Constants;
using GameStore.Domain.Entities;
using GameStore.Infrastructure.Identity; // <-- NEW
using Microsoft.AspNetCore.Identity;     // <-- NEW
using Microsoft.AspNetCore.Identity.EntityFrameworkCore; // <-- NEW
using Microsoft.EntityFrameworkCore;

namespace GameStore.Infrastructure.Contexts;

// 1. Inherit from IdentityDbContext
public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IApplicationDbContext
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<Genre> Genres => Set<Genre>();
    public new DbSet<User> Users => Set<User>();
    public DbSet<CustomList> CustomLists => Set<CustomList>();
    public DbSet<Review> Reviews => Set<Review>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder); // 2. CRITICAL for Identity tables!

        // 3. Seed Identity Roles
        modelBuilder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = RoleConstants.SuperAdmin, NormalizedName = RoleConstants.SuperAdmin.ToUpper() },
            new IdentityRole { Id = "2", Name = RoleConstants.Admin, NormalizedName = RoleConstants.Admin.ToUpper() },
            new IdentityRole { Id = "3", Name = RoleConstants.Customer, NormalizedName = RoleConstants.Customer.ToUpper() }
        );

        modelBuilder.Entity<Game>()
            .HasOne(g => g.Owner)
            .WithMany(u => u.OwnedGames)
            .HasForeignKey(g => g.OwnerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = 1, Name = "Fighting" },
            new Genre { Id = 2, Name = "Roleplaying" },
            new Genre { Id = 3, Name = "Sports" },
            new Genre { Id = 4, Name = "Racing" },
            new Genre { Id = 5, Name = "Kids and Family" }
        );
    }
}