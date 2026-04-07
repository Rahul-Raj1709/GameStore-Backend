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
public record UpdateGameRequest(string Name, string Description, string? ImageUrl, int GenreId, decimal? Price, DateOnly ReleaseDate);
public record ReviewRequest(int Rating, string? Comment);

public static class GamesEndpoints
{
    public static void MapGamesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/games");
        var requireAdmin = new AuthorizeAttribute { Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.Admin}" };

        // --- 1. Main Catalog ---
        group.MapGet("/", async (
             [FromQuery] string? search,
             [FromQuery] int? genreId,
             [FromQuery] string? sortBy,
             [FromQuery] bool? desc,
             [FromQuery] int? page,
             [FromQuery] int? pageSize,
             IQueryHandler<GetGamesQuery, Result<PagedList<GameSummaryDto>>> handler,
             CancellationToken ct) =>
        {
            var query = new GetGamesQuery(search, genreId, sortBy, desc ?? false, page ?? 1, pageSize ?? 10);
            var result = await handler.Handle(query, ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/{id:int}", async ([FromRoute] int id, IQueryHandler<GetGameByIdQuery, Result<GameDetailsDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGameByIdQuery(id), ct);
            return result.Match(Results.Ok);
        });

        // --- 2. Owner Games ---
        group.MapGet("/owner/{ownerId:int}", async ([FromRoute] int ownerId, [FromQuery] int? cursor, [FromQuery] int? limit, IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGamesByOwnerQuery(ownerId, cursor, limit ?? 10), ct);
            return result.Match(Results.Ok);
        });

        group.MapGet("/my-games", async (ClaimsPrincipal user, [FromQuery] int? cursor, [FromQuery] int? limit, IQueryHandler<GetGamesByOwnerQuery, Result<PagedResponse<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new GetGamesByOwnerQuery(userId, cursor, limit ?? 10), ct);
            return result.Match(Results.Ok);
        })
        .RequireAuthorization(requireAdmin);

        // --- 3. Game Management ---
        group.MapPost("/", async (CreateGameRequest request, ClaimsPrincipal user, ICommandHandler<CreateGameCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var ownerId = int.Parse(userIdString!);

            var command = new CreateGameCommand(ownerId, request.Name, request.Description, request.ImageUrl, request.GenreId, request.Price, request.ReleaseDate);
            var result = await handler.Handle(command, ct);

            return result.Match(id => Results.Ok(new { Id = id }));
        })
        .RequireAuthorization(requireAdmin);

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

        group.MapDelete("/{id:int}", async ([FromRoute] int id, ClaimsPrincipal user, ICommandHandler<DeleteGameCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);
            var isSuperAdmin = user.IsInRole(RoleConstants.SuperAdmin);

            var result = await handler.Handle(new DeleteGameCommand(id, userId, isSuperAdmin), ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization(requireAdmin);

        // --- 4. Likes ---
        group.MapPost("/{id:int}/like", async ([FromRoute] int id, ClaimsPrincipal user, ICommandHandler<ToggleLikeCommand, Result<bool>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new ToggleLikeCommand(userId, id), ct);
            return result.Match(isLiked => Results.Ok(new { GameId = id, IsLiked = isLiked }));
        })
        .RequireAuthorization();

        // --- 5. Reviews ---
        var reviewsGroup = group.MapGroup("/{gameId:int}/reviews");

        reviewsGroup.MapGet("/", async ([FromRoute] int gameId, [FromQuery] int? page, [FromQuery] int? pageSize, IQueryHandler<GetGameReviewsQuery, Result<PagedList<ReviewDto>>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGameReviewsQuery(gameId, page ?? 1, pageSize ?? 10), ct);
            return result.Match(Results.Ok);
        });

        reviewsGroup.MapPost("/", async ([FromRoute] int gameId, ReviewRequest request, ClaimsPrincipal user, ICommandHandler<AddReviewCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new AddReviewCommand(gameId, userId, request.Rating, request.Comment), ct);
            return result.Match(id => Results.Ok(new { ReviewId = id }));
        })
        .RequireAuthorization();

        group.MapPut("/reviews/{reviewId:int}", async ([FromRoute] int reviewId, ReviewRequest request, ClaimsPrincipal user, ICommandHandler<UpdateReviewCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);
            var isSuperAdmin = user.IsInRole(RoleConstants.SuperAdmin);

            var result = await handler.Handle(new UpdateReviewCommand(reviewId, userId, request.Rating, request.Comment, isSuperAdmin), ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization();

        group.MapDelete("/reviews/{reviewId:int}", async ([FromRoute] int reviewId, ClaimsPrincipal user, ICommandHandler<DeleteReviewCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);
            var isSuperAdmin = user.IsInRole(RoleConstants.SuperAdmin);

            var result = await handler.Handle(new DeleteReviewCommand(reviewId, userId, isSuperAdmin), ct);
            return result.Match(() => Results.NoContent());
        })
        .RequireAuthorization();
    }
}