using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Application.DTOs;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Features.Games.Queries;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using GameStore.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.WebApi.Endpoints;

public record CreateGameRequest(string Name, string Description, string? ImageUrl, int GenreId, decimal? Price, DateOnly ReleaseDate);

// NEW: Separating the Request Payload from the Command
public record UpdateGameRequest(string Name, string Description, string? ImageUrl, int GenreId, decimal? Price, DateOnly ReleaseDate);

public static class GamesEndpoints
{
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/games");
        var requireAdmin = new AuthorizeAttribute { Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.Admin}" };

        group.MapGet("/", async ([FromQuery] int? cursor, [FromQuery] int? limit, IQueryHandler<GetGamesQuery, Result<PagedResponse<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGamesQuery(cursor, limit ?? 10), ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/{id:int}", async ([FromRoute] int id, IQueryHandler<GetGameByIdQuery, Result<GameDetailsDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGameByIdQuery(id), ct);
            return result.Match(Results.Ok);
        });

        // --- NEW: Public Endpoint for Customers to view an owner's games ---
        group.MapGet("/owner/{ownerId:int}", async ([FromRoute] int ownerId, [FromQuery] int? cursor, [FromQuery] int? limit, IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGamesByOwnerQuery(ownerId, cursor, limit ?? 10), ct);
            return result.Match(Results.Ok);
        });

        // --- NEW: Authenticated Endpoint for Admins to view their own games ---
        group.MapGet("/my-games", async (ClaimsPrincipal user, [FromQuery] int? cursor, [FromQuery] int? limit, IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new GetGamesByOwnerQuery(userId, cursor, limit ?? 10), ct);
            return result.Match(Results.Ok);
        })
        .RequireAuthorization(requireAdmin);

        group.MapPost("/", async (CreateGameRequest request, ClaimsPrincipal user, ICommandHandler<CreateGameCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = int.Parse(userIdString!);

            var command = new CreateGameCommand(ownerId, request.Name, request.Description, request.ImageUrl, request.GenreId, request.Price, request.ReleaseDate);
            var result = await handler.Handle(command, ct);

            return result.Match(id => Results.Ok(new { Id = id }));
        })
        .RequireAuthorization(requireAdmin);

        // --- UPDATED: Pass User Context to the PUT Command ---
        group.MapPut("/{id:int}", async ([FromRoute] int id, UpdateGameRequest request, ClaimsPrincipal user, ICommandHandler<UpdateGameCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);
            var isSuperAdmin = user.IsInRole(RoleConstants.SuperAdmin);

            var command = new UpdateGameCommand(id, request.Name, request.Description, request.ImageUrl, request.GenreId, request.Price, request.ReleaseDate, userId, isSuperAdmin);

            var result = await handler.Handle(command, ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization(requireAdmin);

        // --- UPDATED: Pass User Context to the DELETE Command ---
        group.MapDelete("/{id:int}", async ([FromRoute] int id, ClaimsPrincipal user, ICommandHandler<DeleteGameCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);
            var isSuperAdmin = user.IsInRole(RoleConstants.SuperAdmin);

            var result = await handler.Handle(new DeleteGameCommand(id, userId, isSuperAdmin), ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization(requireAdmin);

        group.MapPost("/{id:int}/like", async ([FromRoute] int id, ClaimsPrincipal user, ICommandHandler<ToggleLikeCommand, Result<bool>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new ToggleLikeCommand(userId, id), ct);
            return result.Match(isLiked => Results.Ok(new { GameId = id, IsLiked = isLiked }));
        })
        .RequireAuthorization();
    }
}