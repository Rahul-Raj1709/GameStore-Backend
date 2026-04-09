using FluentValidation;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Entities;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Commands;

public record CreateGenreCommand(string Name) : ICommand<Result<int>>;

public class CreateGenreCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<CreateGenreCommand, Result<int>> // <-- Injected HybridCache
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

        // --- CACHE INVALIDATION ---
        // Evict the global genres list so the UI dropdowns get the newly added genre on their next fetch.
        await cache.RemoveAsync("genres-list", cancellationToken);

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