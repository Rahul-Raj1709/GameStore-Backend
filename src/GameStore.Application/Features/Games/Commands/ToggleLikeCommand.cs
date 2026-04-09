using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Commands;

public record ToggleLikeCommand(int UserId, int GameId) : ICommand<Result<bool>>;

public class ToggleLikeCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<ToggleLikeCommand, Result<bool>> // <-- Injected HybridCache
{
    public async Task<Result<bool>> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.Include(u => u.LikedGames).FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        var game = await context.Games.FindAsync([request.GameId], cancellationToken);

        if (user == null || game == null) return Result.Failure<bool>(Error.ConditionNotMet);

        var alreadyLiked = user.LikedGames.Any(g => g.Id == request.GameId);
        bool isLikedNow;

        if (alreadyLiked)
        {
            user.LikedGames.Remove(game);
            isLikedNow = false;
        }
        else
        {
            user.LikedGames.Add(game);
            isLikedNow = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{request.GameId}", cancellationToken);

        return Result.Success(isLikedNow);
    }
}