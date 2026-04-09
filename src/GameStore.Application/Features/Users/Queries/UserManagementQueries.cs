// File: src/GameStore.Application/Features/Users/Queries/UserManagementQueries.cs
using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

// --- QUERY: Get All Admins ---
public record GetAdminsQuery(int Page = 1, int PageSize = 10) : IQuery<Result<PagedList<AdminSummaryDto>>>;

public class GetAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetAdminsQuery, Result<PagedList<AdminSummaryDto>>>
{
    public async Task<Result<PagedList<AdminSummaryDto>>> Handle(GetAdminsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin);

        var totalCount = await query.CountAsync(cancellationToken);

        var admins = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new AdminSummaryDto(u.Id, u.Name, u.Email, u.Username, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = totalCount > (request.Page * request.PageSize);

        return Result.Success(new PagedList<AdminSummaryDto>(admins, request.Page, request.PageSize, totalCount, hasNextPage));
    }
}

// --- QUERY: Get Pending Admins ---
public record GetPendingAdminsQuery(int Page = 1, int PageSize = 10) : IQuery<Result<PagedList<PendingAdminDto>>>;

public class GetPendingAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPendingAdminsQuery, Result<PagedList<PendingAdminDto>>>
{
    public async Task<Result<PagedList<PendingAdminDto>>> Handle(GetPendingAdminsQuery request, CancellationToken cancellationToken)
    {
        var query = context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin && !u.IsActive);

        var totalCount = await query.CountAsync(cancellationToken);

        var pendingAdmins = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new PendingAdminDto(u.Id, u.Name, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = totalCount > (request.Page * request.PageSize);

        return Result.Success(new PagedList<PendingAdminDto>(pendingAdmins, request.Page, request.PageSize, totalCount, hasNextPage));
    }
}

// --- QUERY: Get User/Admin Details ---
// (Keep GetUserDetailsQuery as it was, no changes needed for this one)
public record GetUserDetailsQuery(int UserId) : IQuery<Result<UserDetailsDto>>;

public class GetUserDetailsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetUserDetailsQuery, Result<UserDetailsDto>>
{
    public async Task<Result<UserDetailsDto>> Handle(GetUserDetailsQuery request, CancellationToken cancellationToken)
    {
        var user = await context.Users
            .AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDetailsDto(
                u.Id,
                u.Name,
                u.Username,
                u.Email,
                u.Role,
                u.IsActive,
                u.CreatedAt,
                u.LastLogin,
                u.OwnedGames.Count,
                u.Reviews.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null) return Result.Failure<UserDetailsDto>(new Error("User.NotFound", "User not found."));

        return Result.Success(user);
    }
}