using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Queries;

public record GetGameReviewsQuery(int GameId, int Page = 1, int PageSize = 10) : IQuery<Result<PagedList<ReviewDto>>>;

public class GetGameReviewsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetGameReviewsQuery, Result<PagedList<ReviewDto>>>
{
    public async Task<Result<PagedList<ReviewDto>>> Handle(GetGameReviewsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Reviews
            .Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.GameId == request.GameId);

        var totalCount = await query.CountAsync(cancellationToken);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new ReviewDto(r.Id, r.GameId, r.UserId, r.User!.Name, r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = totalCount > (request.Page * request.PageSize);

        return Result.Success(new PagedList<ReviewDto>(reviews, request.Page, request.PageSize, totalCount, hasNextPage));
    }
}