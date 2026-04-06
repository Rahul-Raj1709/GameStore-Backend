using FluentValidation;
using GameStore.Application.DTOs.Auth;
using GameStore.Application.Interfaces;
using GameStore.Application.Interfaces.Security;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Auth.Queries;

public record LoginQuery(string Email, string Password) : IRequest<AuthResponseDto>;

public class LoginQueryHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator)
    : IRequestHandler<LoginQuery, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(LoginQuery request, CancellationToken cancellationToken)
    {
        // Removed AsNoTracking() so we can update LastLogin
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new ValidationException(new[] {
                new FluentValidation.Results.ValidationFailure("Credentials", "Invalid email or password.")
            });
        }

        // Update the timestamp and save
        user.LastLogin = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponseDto(user.Id, user.Username, user.Email, user.Role, token);
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