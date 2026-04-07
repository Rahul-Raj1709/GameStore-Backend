using GameStore.Application.Interfaces;
using GameStore.Application.Messaging; // <-- NEW
using GameStore.Domain.Entities;
using GameStore.Domain.Shared;         // <-- NEW

namespace GameStore.Application.Features.Games.Commands;

// 1. Swap IRequest for ICommand, wrap the returned ID in a Result<>
public record CreateGameCommand(
    int OwnerId, string Name, string Description, string? ImageUrl,
    int GenreId, decimal? Price, DateOnly ReleaseDate
) : ICommand<Result<int>>;

// 2. Swap IRequestHandler for ICommandHandler
public class CreateGameCommandHandler(IApplicationDbContext context)
    : ICommandHandler<CreateGameCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateGameCommand request, CancellationToken cancellationToken)
    {
        var game = new Game
        {
            OwnerId = request.OwnerId,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            GenreId = request.GenreId,
            Price = request.Price,
            ReleaseDate = request.ReleaseDate
        };

        context.Games.Add(game);
        await context.SaveChangesAsync(cancellationToken);

        // 3. Return Success with the new ID
        return Result.Success(game.Id);
    }
}