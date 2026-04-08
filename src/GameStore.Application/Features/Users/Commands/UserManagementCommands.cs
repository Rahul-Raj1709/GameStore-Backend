using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Commands;

// --- COMMAND: Toggle User Status (Activate/Deactivate) ---
public record UpdateUserStatusCommand(int UserId, bool IsActive) : ICommand<Result>;

public class UpdateUserStatusCommandHandler(IApplicationDbContext context) : ICommandHandler<UpdateUserStatusCommand, Result>
{
    public async Task<Result> Handle(UpdateUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await context.Users.FindAsync([request.UserId], cancellationToken);
        if (user == null) return Result.Failure(new Error("User.NotFound", "User not found."));

        // Protect SuperAdmin accounts
        if (user.Role == RoleConstants.SuperAdmin)
            return Result.Failure(new Error("Auth.Forbidden", "Cannot modify the status of a SuperAdmin."));

        user.IsActive = request.IsActive;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// --- COMMAND: Delete User ---
// Added CurrentUserId to prevent self-deletion
public record RemoveUserCommand(int UserId, int CurrentUserId) : ICommand<Result>;

public class RemoveUserCommandHandler(IApplicationDbContext context, IIdentityService identityService) : ICommandHandler<RemoveUserCommand, Result>
{
    public async Task<Result> Handle(RemoveUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Prevent Self Deletion
        if (request.UserId == request.CurrentUserId)
            return Result.Failure(new Error("Auth.Forbidden", "You cannot delete your own account."));

        var user = await context.Users.FindAsync([request.UserId], cancellationToken);
        if (user == null) return Result.Failure(new Error("User.NotFound", "User not found."));

        // 2. Prevent SuperAdmin Deletion
        if (user.Role == RoleConstants.SuperAdmin)
            return Result.Failure(new Error("Auth.Forbidden", "Cannot delete a SuperAdmin account."));

        // Delete from Identity Database
        var identityResult = await identityService.DeleteUserAsync(user.IdentityId);
        if (identityResult.IsFailure) return identityResult;

        // Delete from Application Database
        context.Users.Remove(user);
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

// --- QUERY: Get Pending Admins (Remains the same) ---
public record GetPendingAdminsQuery() : IQuery<Result<List<object>>>;

public class GetPendingAdminsQueryHandler(IApplicationDbContext context) : IQueryHandler<GetPendingAdminsQuery, Result<List<object>>>
{
    public async Task<Result<List<object>>> Handle(GetPendingAdminsQuery request, CancellationToken cancellationToken)
    {
        var pendingAdmins = await context.Users
            .AsNoTracking()
            .Where(u => u.Role == RoleConstants.Admin && !u.IsActive)
            .Select(u => new { u.Id, u.Name, u.Email, u.CreatedAt })
            .ToListAsync(cancellationToken);

        return Result.Success<List<object>>(pendingAdmins.Cast<object>().ToList());
    }
}