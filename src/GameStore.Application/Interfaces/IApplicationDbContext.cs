using GameStore.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Game> Games { get; }
    DbSet<Genre> Genres { get; }
    DbSet<User> Users { get; }
    DbSet<CustomList> CustomLists { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}