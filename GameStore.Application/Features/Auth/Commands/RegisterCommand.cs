using FluentValidation;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using GameStore.Domain.Constants;
using GameStore.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Auth.Commands;

public record RegisterCommand(
    string Name,
    string Username,
    string Email,
    string Password,
    string? Role = null
) : IRequest<AuthResponseDto>;

public class RegisterCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailExists = await context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken);
        if (emailExists)
        {
            throw new ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("Email", "Email is already registered.")
            });
        }

        // 2. Determine the role (Fallback to Customer if none provided)
        var roleToAssign = string.IsNullOrWhiteSpace(request.Role)
            ? RoleConstants.Customer
            : request.Role;

        var user = new User
        {
            Name = request.Name,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = roleToAssign
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, token);
    }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(v => v.Name).NotEmpty().MaximumLength(100);
        RuleFor(v => v.Username).NotEmpty().MaximumLength(50);
        RuleFor(v => v.Email).NotEmpty().EmailAddress();
        RuleFor(v => v.Password).NotEmpty().MinimumLength(6).WithMessage("Password must be at least 6 characters.");

        // 4. Validate that if they provide a role, it's one of our allowed roles!
        RuleFor(v => v.Role)
            .Must(r => r == null || r == RoleConstants.SuperAdmin || r == RoleConstants.Admin || r == RoleConstants.Customer)
            .WithMessage($"Role must be {RoleConstants.SuperAdmin}, {RoleConstants.Admin}, or {RoleConstants.Customer}.");
    }
}