using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

public record GetLikedGamesQuery(int UserId) : IQuery<Result<List<GameSummaryDto>>>;

public class GetLikedGamesQueryHandler(IApplicationDbContext context) : IQueryHandler<GetLikedGamesQuery, Result<List<GameSummaryDto>>>
{
    public async Task<Result<List<GameSummaryDto>>> Handle(GetLikedGamesQuery request, CancellationToken cancellationToken)
    {
        var games = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .SelectMany(u => u.LikedGames)
            .Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate))
            .ToListAsync(cancellationToken);

        return Result.Success(games);
    }
}