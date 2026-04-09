using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGamesByOwnerQuery(int OwnerId, int Page = 1, int PageSize = 10) : IQuery<Result<PagedList<GameSummaryDto>>>;

public class GetGamesByOwnerQueryHandler(IApplicationDbContext context)
    : IQueryHandler<GetGamesByOwnerQuery, Result<PagedList<GameSummaryDto>>>
{
    public async Task<Result<PagedList<GameSummaryDto>>> Handle(GetGamesByOwnerQuery request, CancellationToken cancellationToken)
    {
        var query = context.Games
            .Include(g => g.Genre)
            .AsNoTracking()
            .Where(g => g.OwnerId == request.OwnerId);

        // 1. Calculate total count for the PagedList metadata
        var totalCount = await query.CountAsync(cancellationToken);

        // 2. Apply offset (Skip) and limit (Take) 
        var games = await query
            .OrderByDescending(g => g.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate))
            .ToListAsync(cancellationToken);

        // 3. Calculate if a next page exists
        var hasNextPage = totalCount > (request.Page * request.PageSize);

        // 4. Return standard PagedList response
        return Result.Success(new PagedList<GameSummaryDto>(games, request.Page, request.PageSize, totalCount, hasNextPage));
    }
}