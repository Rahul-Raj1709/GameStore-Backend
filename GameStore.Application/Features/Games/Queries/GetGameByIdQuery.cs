using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGameByIdQuery(int Id) : IRequest<GameDetailsDto?>;

public class GetGameByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetGameByIdQuery, GameDetailsDto?>
{
    public async Task<GameDetailsDto?> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        return await context.Games
            .AsNoTracking()
            .Where(g => g.Id == request.Id)
            .Select(g => new GameDetailsDto(
                g.Id,
                g.Name,
                g.Description,
                g.ImageUrl,
                g.GenreId,
                g.Genre!.Name,
                g.Price,
                g.ReleaseDate,
                g.AddedAt,
                g.TotalLikes,
                g.Owner!.Name
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}