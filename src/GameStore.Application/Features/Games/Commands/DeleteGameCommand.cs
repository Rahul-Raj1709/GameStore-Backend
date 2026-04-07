using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;

namespace GameStore.Application.Features.Games.Commands;

public record DeleteGameCommand(int Id) : ICommand<Result>;

public class DeleteGameCommandHandler(IApplicationDbContext context) : ICommandHandler<DeleteGameCommand, Result>
{
    public async Task<Result> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await context.Games.FindAsync([request.Id], cancellationToken);
        if (game == null) return Result.Failure(GameErrors.NotFound);

        context.Games.Remove(game);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}