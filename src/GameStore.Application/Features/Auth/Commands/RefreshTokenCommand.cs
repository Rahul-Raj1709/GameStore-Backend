using FluentValidation;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Application.Messaging;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Auth.Commands;

public record RefreshTokenCommand(string Email, string RefreshToken) : ICommand<Result<AuthResponseDto>>;

public class RefreshTokenCommandHandler(IApplicationDbContext context, IIdentityService identityService, IJwtTokenGenerator jwtTokenGenerator)
    : ICommandHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var refreshResult = await identityService.RefreshTokenAsync(request.Email, request.RefreshToken);
        if (refreshResult.IsFailure) return Result.Failure<AuthResponseDto>(refreshResult.Error);

        var user = await context.Users.FirstOrDefaultAsync(u => u.IdentityId == refreshResult.Value.IdentityId, cancellationToken);
        if (user is null) return Result.Failure<AuthResponseDto>(AuthErrors.InvalidCredentials);

        var newJwtToken = jwtTokenGenerator.GenerateToken(user);

        return Result.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, newJwtToken, refreshResult.Value.NewRefreshToken));
    }
}

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.RefreshToken).NotEmpty();
    }
}