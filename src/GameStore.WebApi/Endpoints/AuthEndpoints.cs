using GameStore.Application.DTOs.Auth;
using GameStore.Application.Features.Auth.Commands;
using GameStore.Application.Features.Auth.Queries;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using GameStore.WebApi.Extensions;
using GameStore.WebApi.Filters;

namespace GameStore.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        // Notice we attach the ValidationFilter to the entire group!
        var group = app.MapGroup("/api/auth").AddEndpointFilter<ValidationFilter>();

        group.MapPost("/register", async (RegisterCommand command, ICommandHandler<RegisterCommand, Result<AuthResponseDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/login", async (LoginQuery query, IQueryHandler<LoginQuery, Result<AuthResponseDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(query, ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/refresh", async (RefreshTokenCommand command, ICommandHandler<RefreshTokenCommand, Result<AuthResponseDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/forgot-password", async (RequestPasswordResetCommand command, ICommandHandler<RequestPasswordResetCommand, Result<string>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            // Returns the raw token for testing. In production, this would return Ok() and send an email silently.
            return result.Match(token => Results.Ok(new { ResetToken = token }));
        });

        group.MapPost("/reset-password", async (ResetPasswordCommand command, ICommandHandler<ResetPasswordCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(command, ct);
            return result.Match(() => Results.Ok(new { Message = "Password reset successfully." }));
        });
    }
}