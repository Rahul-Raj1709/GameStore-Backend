using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- NEW

namespace GameStore.Application.Features.Games.Queries;

public record GetGamesQuery(int? Cursor = null, int Limit = 10) : IQuery<Result<PagedResponse<GameSummaryDto>>>;

public class GetGamesQueryHandler(IApplicationDbContext context, HybridCache cache)
    : IQueryHandler<GetGamesQuery, Result<PagedResponse<GameSummaryDto>>>
{
    public async Task<Result<PagedResponse<GameSummaryDto>>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        // Cache key changes based on the cursor and limit
        var cacheKey = $"games-list-{request.Cursor ?? 0}-{request.Limit}";

        var response = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                var query = context.Games.AsNoTracking();

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

        return Result.Success(response);
    }
}