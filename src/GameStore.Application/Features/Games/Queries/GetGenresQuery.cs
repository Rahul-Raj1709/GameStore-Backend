using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Queries;

public record GetGenresQuery() : IQuery<Result<List<GenreDto>>>;

public class GetGenresQueryHandler(IApplicationDbContext context, HybridCache cache) : IQueryHandler<GetGenresQuery, Result<List<GenreDto>>> // <-- Injected HybridCache
{
    public async Task<Result<List<GenreDto>>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        // Wrap the DB call in the Hybrid Cache
        var genres = await cache.GetOrCreateAsync(
            "genres-list",
            async cancel => await context.Genres
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new GenreDto(g.Id, g.Name))
                .ToListAsync(cancel),
            cancellationToken: cancellationToken);

        return Result.Success(genres);
    }
}