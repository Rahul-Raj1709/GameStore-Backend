using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGamesQuery(
    string? SearchTerm,
    int? GenreId,
    string? SortBy,
    bool SortDescending,
    int Page = 1,
    int PageSize = 10) : IQuery<Result<PagedList<GameSummaryDto>>>;

public class GetGamesQueryHandler(IApplicationDbContext context) : IQueryHandler<GetGamesQuery, Result<PagedList<GameSummaryDto>>>
{
    public async Task<Result<PagedList<GameSummaryDto>>> Handle(GetGamesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Games
            .Include(g => g.Genre)
            .AsNoTracking()
            .AsQueryable();

        // 1. FILTERING & SEARCH
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(g => g.Name.ToLower().Contains(searchLower) || g.Description.ToLower().Contains(searchLower));
        }

        if (request.GenreId.HasValue)
        {
            query = query.Where(g => g.GenreId == request.GenreId.Value);
        }

        // 2. SORTING
        query = request.SortBy?.ToLower() switch
        {
            "price" => request.SortDescending ? query.OrderByDescending(g => g.Price) : query.OrderBy(g => g.Price),
            "popularity" => request.SortDescending ? query.OrderByDescending(g => g.LikedByUsers.Count) : query.OrderBy(g => g.LikedByUsers.Count),
            "rating" => request.SortDescending ? query.OrderByDescending(g => g.AverageRating) : query.OrderBy(g => g.AverageRating),
            "random" => query.OrderBy(g => Guid.NewGuid()), // EF Core translates this to native DB randomizer (e.g. NEWID())
            "name" => request.SortDescending ? query.OrderByDescending(g => g.Name) : query.OrderBy(g => g.Name),
            _ => request.SortDescending ? query.OrderByDescending(g => g.Id) : query.OrderBy(g => g.Id) // Default fallback
        };

        // 3. PAGINATION (Offset)
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(g => new GameSummaryDto(g.Id, g.Name, g.Genre!.Name, g.Price, g.ReleaseDate))
            .ToListAsync(cancellationToken);

        var hasNextPage = totalCount > (request.Page * request.PageSize);

        return Result.Success(new PagedList<GameSummaryDto>(items, request.Page, request.PageSize, totalCount, hasNextPage));
    }
}