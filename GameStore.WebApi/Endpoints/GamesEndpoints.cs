using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Features.Games.Queries;
using GameStore.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc; // Added for [FromQuery]

namespace GameStore.WebApi.Endpoints;

public record CreateGameRequest(string Name, string Description, string? ImageUrl, int GenreId, decimal? Price, DateOnly ReleaseDate);

public static class GamesEndpoints
{
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/games");
        var requireAdmin = new AuthorizeAttribute { Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.Admin}" };

        // 1. UPDATED: GET ALL (Paginated)
        group.MapGet("/", async ([FromQuery] int? cursor, [FromQuery] int? limit, IMediator mediator, CancellationToken ct) =>
        {
            // Default to 10 items if limit isn't provided
            var query = new GetGamesQuery(cursor, limit ?? 10);
            return Results.Ok(await mediator.Send(query, ct));
        });

        // GET BY ID (Public)
        group.MapGet("/{id}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var game = await mediator.Send(new GetGameByIdQuery(id), ct);
            return game is not null ? Results.Ok(game) : Results.NotFound();
        });

        // CREATE (Secured)
        group.MapPost("/", async (CreateGameRequest request, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            // Extract the User ID from the JWT Claims
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var ownerId = int.Parse(userIdString!);

            // Construct the secure command
            var command = new CreateGameCommand(
                ownerId,
                request.Name,
                request.Description,
                request.ImageUrl,
                request.GenreId,
                request.Price,
                request.ReleaseDate
            );

            var newGameId = await mediator.Send(command, ct);
            return Results.Ok(new { Id = newGameId });
        })
        .RequireAuthorization(requireAdmin);

        // UPDATE (Secured)
        group.MapPut("/{id}", async (int id, UpdateGameCommand command, IMediator mediator, CancellationToken ct) =>
        {
            if (id != command.Id) return Results.BadRequest("ID mismatch.");

            var success = await mediator.Send(command, ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(requireAdmin);

        // DELETE (Secured)
        group.MapDelete("/{id}", async (int id, IMediator mediator, CancellationToken ct) =>
        {
            var success = await mediator.Send(new DeleteGameCommand(id), ct);
            return success ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(requireAdmin);

        group.MapPost("/{id}/like", async (int id, ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = int.Parse(userIdString!);

            var isLiked = await mediator.Send(new ToggleLikeCommand(userId, id), ct);

            return Results.Ok(new { GameId = id, IsLiked = isLiked });
        })
        .RequireAuthorization(); // Just requires a valid token, no specific role needed
    }
}