using GameStore.Application.DTOs;
using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Queries;

// --- QUERY: Get All Admins ---
public record GetAdminsQuery() : IQuery<Result<List<AdminSummaryDto>>>;

public class GetAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetAdminsQuery, Result<List<AdminSummaryDto>>>
{
    public async Task<Result<List<AdminSummaryDto>>> Handle(GetAdminsQuery request, CancellationToken cancellationToken)
    {
        var admins = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin)
            .Select(u => new AdminSummaryDto(u.Id, u.Name, u.Email, u.Username, u.IsActive, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(admins);
    }
}

// --- QUERY: Get Pending Admins ---
public record GetPendingAdminsQuery() : IQuery<Result<List<PendingAdminDto>>>;

public class GetPendingAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPendingAdminsQuery, Result<List<PendingAdminDto>>>
{
    public async Task<Result<List<PendingAdminDto>>> Handle(GetPendingAdminsQuery request, CancellationToken cancellationToken)
    {
        var pendingAdmins = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin && !u.IsActive)
            .Select(u => new PendingAdminDto(u.Id, u.Name, u.Email, u.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success(pendingAdmins);
    }
}

// --- QUERY: Get User/Admin Details ---
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