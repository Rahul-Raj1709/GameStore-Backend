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

        group.MapGet("/{id}", async (int id, IQueryHandler<GetGameByIdQuery, Result<GameDetailsDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGameByIdQuery(id), ct);
            return result.Match(Results.Ok);
        });

        group.MapPost("/", async (CreateGameRequest request, ClaimsPrincipal user, ICommandHandler<CreateGameCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = int.Parse(userIdString!);

            var command = new CreateGameCommand(ownerId, request.Name, request.Description, request.ImageUrl, request.GenreId, request.Price, request.ReleaseDate);
            var result = await handler.Handle(command, ct);

            return result.Match(id => Results.Ok(new { Id = id }));
        })
        .RequireAuthorization(requireAdmin);

        group.MapPut("/{id}", async (int id, UpdateGameCommand command, ICommandHandler<UpdateGameCommand, Result> handler, CancellationToken ct) =>
        {
            if (id != command.Id) return Results.BadRequest("ID mismatch.");

            var result = await handler.Handle(command, ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization(requireAdmin);

        group.MapDelete("/{id}", async (int id, ICommandHandler<DeleteGameCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new DeleteGameCommand(id), ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization(requireAdmin);

        group.MapPost("/{id}/like", async (int id, ClaimsPrincipal user, ICommandHandler<ToggleLikeCommand, Result<bool>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new ToggleLikeCommand(userId, id), ct);
            return result.Match(isLiked => Results.Ok(new { GameId = id, IsLiked = isLiked }));
        })
        .RequireAuthorization();
    }
}