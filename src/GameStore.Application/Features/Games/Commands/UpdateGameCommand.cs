using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Commands;

public record UpdateGameCommand(int Id, string Name, string Description, string? ImageUrl, int GenreId, decimal? Price, DateOnly ReleaseDate, int CurrentUserId, bool IsSuperAdmin)
    : ICommand<Result>;

public class UpdateGameCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<UpdateGameCommand, Result> // <-- Injected HybridCache
{
    public async Task<Result> Handle(UpdateGameCommand request, CancellationToken cancellationToken)
    {
        var game = await context.Games.FindAsync([request.Id], cancellationToken);
        if (game == null) return Result.Failure(GameErrors.NotFound);

        // --- OWNERSHIP CHECK ---
        if (!request.IsSuperAdmin && game.OwnerId != request.CurrentUserId)
        {
            return Result.Failure(new Error("Auth.Forbidden", "You do not have permission to modify this game."));
        }

        game.Name = request.Name;
        game.Description = request.Description;
        game.ImageUrl = request.ImageUrl;
        game.GenreId = request.GenreId;
        game.Price = request.Price;
        game.ReleaseDate = request.ReleaseDate;

        await context.SaveChangesAsync(cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{request.Id}", cancellationToken);

        return Result.Success();
    }
}