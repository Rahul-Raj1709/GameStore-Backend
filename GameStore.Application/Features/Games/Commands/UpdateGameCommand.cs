using FluentValidation;
using GameStore.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Commands;

// 1. The Command (Returns a boolean indicating success/failure)
public record UpdateGameCommand(
    int Id,
    string Name,
    int GenreId,
    decimal? Price,
    DateOnly ReleaseDate
) : IRequest<bool>;

// 2. The Handler
public class UpdateGameCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateGameCommand, bool>
{
    public async Task<bool> Handle(UpdateGameCommand request, CancellationToken cancellationToken)
    {
        var game = await context.Games.FindAsync([request.Id], cancellationToken);

        if (game is null) return false;

        game.Name = request.Name;
        game.GenreId = request.GenreId;
        game.Price = request.Price;
        game.ReleaseDate = request.ReleaseDate;

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// 3. The Validator (Placed in the same file for convenience, or in its own file)
public class UpdateGameCommandValidator : AbstractValidator<UpdateGameCommand>
{
    public UpdateGameCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(50);
        RuleFor(v => v.GenreId).GreaterThan(0);
        RuleFor(v => v.Price).GreaterThanOrEqualTo(0).When(v => v.Price.HasValue);
    }
}