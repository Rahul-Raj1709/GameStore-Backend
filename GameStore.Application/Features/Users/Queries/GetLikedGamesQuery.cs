using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

public record GetLikedGamesQuery(int UserId) : IRequest<List<GameSummaryDto>>;

public class GetLikedGamesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLikedGamesQuery, List<GameSummaryDto>>
{
    public async Task<List<GameSummaryDto>> Handle(GetLikedGamesQuery request, CancellationToken cancellationToken)
    {
        return await context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .SelectMany(u => u.LikedGames) // Flatten the many-to-many relationship
            .Select(g => new GameSummaryDto(
                g.Id,
                g.Name,
                g.Genre!.Name,
                g.Price,
                g.ReleaseDate
            ))
            .ToListAsync(cancellationToken);
    }
}