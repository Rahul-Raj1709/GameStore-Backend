using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;

namespace GameStore.Application.Features.Games.Commands;

public record DeleteGameCommand(int Id, int CurrentUserId, bool IsSuperAdmin) : ICommand<Result>;

public class DeleteGameCommandHandler(IApplicationDbContext context) : ICommandHandler<DeleteGameCommand, Result>
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

        return Result.Success();
    }
}