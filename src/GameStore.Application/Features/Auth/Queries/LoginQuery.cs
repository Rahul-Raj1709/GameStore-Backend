using FluentValidation;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Auth.Queries;

public record LoginQuery(string Email, string Password) : IQuery<Result<AuthResponseDto>>;

public class LoginQueryHandler(
    IApplicationDbContext context,
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator)
    : IQueryHandler<LoginQuery, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        var identityResult = await identityService.AuthenticateUserAsync(request.Email, request.Password);
        if (identityResult.IsFailure) return Result.Failure<AuthResponseDto>(identityResult.Error);

        var user = await context.Users.FirstOrDefaultAsync(u => u.IdentityId == identityResult.Value.IdentityId, cancellationToken);
        if (user is null) return Result.Failure<AuthResponseDto>(AuthErrors.InvalidCredentials);

        // --- NEW: Check if the user is activated ---
        if (!user.IsActive)
        {
            return Result.Failure<AuthResponseDto>(new Error("Auth.Inactive", "Your account is pending activation by a SuperAdmin, or has been disabled."));
        }

        user.LastLogin = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user);
        return Result.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, token, identityResult.Value.RefreshToken));
    }
}

public class LoginQueryValidator : AbstractValidator<LoginQuery>
{
    public LoginQueryValidator()
    {
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.Password).NotEmpty();
    }
}