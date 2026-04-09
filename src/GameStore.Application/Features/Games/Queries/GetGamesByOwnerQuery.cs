using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGamesByOwnerQuery(int OwnerId, int? Cursor = null, int Limit = 10) : IQuery<Result<PagedResponse<GameSummaryDto>>>;

public class GetGamesByOwnerQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>>
{
    public async Task<Result<PagedResponse<GameSummaryDto>>> Handle(GetGamesByOwnerQuery request, CancellationToken cancellationToken)
    {
        var query = context.Games
            .Include(g => g.Genre)
            .AsNoTracking()
            .Where(g => g.OwnerId == request.OwnerId);

        // Cursor condition
        if (request.Cursor.HasValue)
        {
            query = query.Where(g => g.Id < request.Cursor.Value);
        }

        var games = await query
            .OrderByDescending(g => g.Id)
            .Take(request.Limit + 1)
            .Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate))
            .ToListAsync(cancellationToken);

        int? nextCursor = null;
        if (games.Count > request.Limit)
        {
            nextCursor = games.Last().Id;
            games.RemoveAt(games.Count - 1);
        }

        return Result.Success(new PagedResponse<GameSummaryDto>(games, nextCursor));
    }
}