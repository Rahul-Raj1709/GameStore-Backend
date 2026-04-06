using GameStore.Application.Interfaces;
using GameStore.Domain.Entities;
using MediatR;

namespace GameStore.Application.Features.Games.Commands;

public record CreateGameCommand(
    int OwnerId,
    string Name,
    string Description,
    string? ImageUrl,
    int GenreId,
    decimal? Price,
    DateOnly ReleaseDate
) : IRequest<int>;

public class CreateGameCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateGameCommand, int>
{
    public async Task<int> Handle(CreateGameCommand request, CancellationToken cancellationToken)
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

        return game.Id;
    }
}