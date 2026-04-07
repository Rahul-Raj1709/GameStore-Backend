using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace GameStore.Application.Features.Games.Queries;

// Notice this still uses Cursor and Limit
public record GetGamesByOwnerQuery(int OwnerId, int? Cursor = null, int Limit = 10) : IQuery<Result<PagedResponse<GameSummaryDto>>>;

public class GetGamesByOwnerQueryHandler(IApplicationDbContext context, HybridCache cache)
    : IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>>
{
    public async Task<Result<PagedResponse<GameSummaryDto>>> Handle(GetGamesByOwnerQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"games-owner-{request.OwnerId}-{request.Cursor ?? 0}-{request.Limit}";

        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var query = context.Games.Include(g => g.Genre).AsNoTracking().Where(g => g.OwnerId == request.OwnerId);

                // Cursor condition
                if (request.Cursor.HasValue) query = query.Where(g => g.Id < request.Cursor.Value);

                var games = await query
                    .OrderByDescending(g => g.Id)
                    .Take(request.Limit + 1)
                    .Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate))
                    .ToListAsync(cancel);

                int? nextCursor = null;
                if (games.Count > request.Limit)
                {
                    nextCursor = games.Last().Id;
                    games.RemoveAt(games.Count - 1);
                }

                return new PagedResponse<GameSummaryDto>(games, nextCursor);
            },
            cancellationToken: cancellationToken);

        return Result.Success(response!);
    }
}