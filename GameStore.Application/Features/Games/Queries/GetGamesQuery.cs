using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

// Added Cursor and Limit
public record GetGamesQuery(int? Cursor = null, int Limit = 10) : IRequest<PagedResponse<GameSummaryDto>>;

public class GetGamesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetGamesQuery, PagedResponse<GameSummaryDto>>
{
    public async Task<PagedResponse<GameSummaryDto>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Games.AsNoTracking();

        // If a cursor is provided, only fetch games OLDER (smaller ID) than the cursor
        if (request.Cursor.HasValue)
        {
            query = query.Where(g => g.Id < request.Cursor.Value);
        }

        // Fetch one extra item to determine if there is a next page
        var games = await query
            .OrderByDescending(g => g.Id)
            .Take(request.Limit + 1)
            .Select(g => new GameSummaryDto(
                g.Id,
                g.Name,
                g.Genre!.Name,
                g.Price,
                g.ReleaseDate
            ))
            .ToListAsync(cancellationToken);

        // Check if we have more items than the requested limit
        int? nextCursor = null;
        if (games.Count > request.Limit)
        {
            nextCursor = games.Last().Id; // Set the cursor to the ID of the extra item
            games.RemoveAt(games.Count - 1); // Remove the extra item before returning
        }

        return new PagedResponse<GameSummaryDto>(games, nextCursor);
    }
}