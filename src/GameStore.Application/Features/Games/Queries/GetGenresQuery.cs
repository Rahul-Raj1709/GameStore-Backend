using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGenresQuery() : IQuery<Result<List<GenreDto>>>;

public class GetGenresQueryHandler(IApplicationDbContext context) : IQueryHandler<GetGenresQuery, Result<List<GenreDto>>>
{
    public async Task<Result<List<GenreDto>>> Handle(GetGenresQuery request, CancellationToken cancellationToken)
    {
        var genres = await context.Genres
            .AsNoTracking()
            .OrderBy(g => g.Name) // Alphabetical order is best for UI dropdowns
            .Select(g => new GenreDto(g.Id, g.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(genres);
    }
}