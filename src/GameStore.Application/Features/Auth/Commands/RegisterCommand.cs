using FluentValidation;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Entities;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Auth.Commands;

public record RegisterCommand(string Name, string Username, string Email, string Password, string? Role = null)
    : ICommand<Result<AuthResponseDto>>;

public class RegisterCommandHandler(
    IApplicationDbContext context,
    IIdentityService identityService,
    IJwtTokenGenerator jwtTokenGenerator)
    : ICommandHandler<RegisterCommand, Result<AuthResponseDto>>
{
    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var roleToAssign = string.IsNullOrWhiteSpace(request.Role) ? RoleConstants.Customer : request.Role;

        var identityResult = await identityService.RegisterUserAsync(request.Email, request.Username, request.Password, roleToAssign);
        if (identityResult.IsFailure) return Result.Failure<AuthResponseDto>(identityResult.Error);

        // --- NEW: Admins need SuperAdmin approval to become active ---
        bool isActive = roleToAssign != RoleConstants.Admin;

        var user = new User
        {
            IdentityId = identityResult.Value.IdentityId,
            Name = request.Name,
            Username = request.Username,
            Email = request.Email,
            Role = roleToAssign,
            IsActive = isActive // Set the flag
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        // If inactive, we shouldn't return a valid JWT token yet.
        if (!isActive)
        {
            return Result.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, "PENDING_ACTIVATION", ""));
        }

        var token = jwtTokenGenerator.GenerateToken(user);
        return Result.Success(new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, token, identityResult.Value.RefreshToken));
    }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Username).NotEmpty().MaximumLength(50);
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.Password).NotEmpty().MinimumLength(6);
        RuleFor(v => v.Role).Must(r => r == null || r == RoleConstants.SuperAdmin || r == RoleConstants.Admin || r == RoleConstants.Customer);
    }
}