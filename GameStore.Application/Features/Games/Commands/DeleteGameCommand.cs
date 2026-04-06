using GameStore.Application.Interfaces;
using MediatR;

namespace GameStore.Application.Features.Games.Commands;

public record DeleteGameCommand(int Id) : IRequest<bool>;

public class DeleteGameCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteGameCommand, bool>
{
    public async Task<bool> Handle(DeleteGameCommand request, CancellationToken cancellationToken)
    {
        var game = await context.Games.FindAsync([request.Id], cancellationToken);

        if (game is null) return false;

        context.Games.Remove(game);
        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}