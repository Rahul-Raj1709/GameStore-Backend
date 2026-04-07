using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- NEW

namespace GameStore.Application.Features.Games.Queries;

public record GetGameByIdQuery(int Id) : IQuery<Result<GameDetailsDto>>;

public class GetGameByIdQueryHandler(IApplicationDbContext context, HybridCache cache)
    : IQueryHandler<GetGameByIdQuery, Result<GameDetailsDto>>
{
    public async Task<Result<GameDetailsDto>> Handle(GetGameByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"game-details-{request.Id}";

        // HybridCache will check memory, then Redis. If both miss, it executes the factory method below.
        var game = await cache.GetOrCreateAsync(
            cacheKey,
            async cancel => await context.Games
                .AsNoTracking()
                .Where(g => g.Id == request.Id)
                .Select(g => new GameDetailsDto(
                    g.Id, g.Name, g.Description, g.ImageUrl, g.GenreId, g.Genre!.Name,
                    g.Price, g.ReleaseDate, g.AddedAt, g.TotalLikes, g.Owner!.Name
                ))
                .FirstOrDefaultAsync(cancel),
            cancellationToken: cancellationToken);

        if (game is null) return Result.Failure<GameDetailsDto>(GameErrors.NotFound);

        return Result.Success(game);
    }
}