using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

// --- 1. GET ALL LISTS FOR USER ---
public record GetUserCustomListsQuery(int UserId) : IQuery<Result<List<CustomListSummaryDto>>>;

public class GetUserCustomListsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetUserCustomListsQuery, Result<List<CustomListSummaryDto>>>
{
    public async Task<Result<List<CustomListSummaryDto>>> Handle(GetUserCustomListsQuery request, CancellationToken cancellationToken)
    {
        var lists = await context.CustomLists
            .AsNoTracking()
            .Where(l => l.UserId == request.UserId)
            .Select(l => new CustomListSummaryDto(l.Id, l.Name, l.Games.Count))
            .ToListAsync(cancellationToken);

        return Result.Success(lists);
    }
}

// --- 2. GET LIST DETAILS (WITH GAMES) ---
public record GetCustomListDetailsQuery(int UserId, int ListId) : IQuery<Result<CustomListDetailsDto>>;

public class GetCustomListDetailsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetCustomListDetailsQuery, Result<CustomListDetailsDto>>
{
    public async Task<Result<CustomListDetailsDto>> Handle(GetCustomListDetailsQuery request, CancellationToken cancellationToken)
    {
        var list = await context.CustomLists
            .AsNoTracking()
            .Where(l => l.Id == request.ListId && l.UserId == request.UserId)
            .Select(l => new CustomListDetailsDto(
                l.Id,
                l.Name,
                l.Games.Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate)).ToList()
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (list == null) return Result.Failure<CustomListDetailsDto>(new Error("List.NotFound", "List not found."));

        return Result.Success(list);
    }
}