using GameStore.Application.Features.Auth.Commands;
using GameStore.Application.Features.Auth.Queries;
using MediatR;

namespace GameStore.WebApi.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterCommand command, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.Send(command, ct);
            return Results.Ok(response);
        });

        group.MapPost("/login", async (LoginQuery query, IMediator mediator, CancellationToken ct) =>
        {
            var response = await mediator.Send(query, ct);
            return Results.Ok(response);
        });
    }
}