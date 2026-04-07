using FluentValidation;
using GameStore.Application.Interfaces.Security;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;

namespace GameStore.Application.Features.Auth.Commands;

// 1. Forgot Password Command
public record RequestPasswordResetCommand(string Email) : ICommand<Result<string>>;

public class RequestPasswordResetCommandHandler(IIdentityService identityService) : ICommandHandler<RequestPasswordResetCommand, Result<string>>
{
    public async Task<Result<string>> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        // In a real app, you would send an email here instead of returning the token.
        // Returning the token in the API response is purely for testing via Scalar/Postman.
        return await identityService.GeneratePasswordResetTokenAsync(request.Email);
    }
}

// 2. Reset Password Command
public record ResetPasswordCommand(string Email, string Token, string NewPassword) : ICommand<Result>;

public class ResetPasswordCommandHandler(IIdentityService identityService) : ICommandHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        return await identityService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);
    }
}

// 3. Validators
public class RequestPasswordResetCommandValidator : AbstractValidator<RequestPasswordResetCommand>
{
    public RequestPasswordResetCommandValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
}

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(6);
    }
}