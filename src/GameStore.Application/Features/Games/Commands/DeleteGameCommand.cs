using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Commands;

public record DeleteGameCommand(int Id, int CurrentUserId, bool IsSuperAdmin) : ICommand<Result>;

public class DeleteGameCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<DeleteGameCommand, Result> // <-- Injected HybridCache
{
    public async Task<Result> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await context.Games.FindAsync([request.Id], cancellationToken);
        if (game == null) return Result.Failure(GameErrors.NotFound);

        // --- OWNERSHIP CHECK ---
        if (!request.IsSuperAdmin && game.OwnerId != request.CurrentUserId)
        {
            return Result.Failure(new Error("Auth.Forbidden", "You do not have permission to delete this game."));
        }

        context.Games.Remove(game);
        await context.SaveChangesAsync(cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{request.Id}", cancellationToken);

        return Result.Success();
    }
}