using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

// --- QUERY: Get All Admins ---
public record GetAdminsQuery() : IQuery<Result<List<object>>>;

public class GetAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetAdminsQuery, Result<List<object>>>
{
    public async Task<Result<List<object>>> Handle(GetAdminsQuery request, CancellationToken cancellationToken)
    {
        var admins = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin)
            .Select(u => new { u.Id, u.Name, u.Email, u.Username, u.IsActive, u.CreatedAt })
            .ToListAsync(cancellationToken);

        return Result.Success<List<object>>(admins.Cast<object>().ToList());
    }
}

// --- QUERY: Get User/Admin Details ---
public record GetUserDetailsQuery(int UserId) : IQuery<Result<object>>;

public class GetUserDetailsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetUserDetailsQuery, Result<object>>
{
    public async Task<Result<object>> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new
            {
                u.Id,
                u.Name,
                u.Username,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.LastLogin,
                OwnedGamesCount = u.OwnedGames.Count,
                ReviewsCount = u.Reviews.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null) return Result.Failure<object>(new Error("User.NotFound", "User not found."));

        return Result.Success<object>(user);
    }
}