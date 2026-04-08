using FluentValidation;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Entities;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Commands;

public record CreateGenreCommand(string Name) : ICommand<Result<int>>;

public class CreateGenreCommandHandler(IApplicationDbContext context) : ICommandHandler<CreateGenreCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateGenreCommand request, CancellationToken cancellationToken)
    {
        // 1. Check for duplicates (Case-insensitive check)
        var exists = await context.Genres
            .AnyAsync(g => g.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (exists)
        {
            return Result.Failure<int>(new Error("Genre.Duplicate", $"The genre '{request.Name}' already exists."));
        }

        // 2. Create the new genre
        var genre = new Genre { Name = request.Name };
        context.Genres.Add(genre);

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success(genre.Id);
    }
}

public class CreateGenreCommandValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Genre name is required.")
            .MaximumLength(50).WithMessage("Genre name must not exceed 50 characters.");
    }
}