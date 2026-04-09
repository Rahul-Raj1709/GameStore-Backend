using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Application.DTOs;
using GameStore.Application.Features.Users.Commands;
using GameStore.Application.Features.Users.Queries;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using GameStore.WebApi.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.WebApi.Endpoints;

public record CreateListRequest(string Name);

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();
        var requireSuperAdmin = new AuthorizeAttribute { Roles = RoleConstants.SuperAdmin };

        // --- Standard User Endpoints ---
        group.MapGet("/me/likes", async (ClaimsPrincipal user, IQueryHandler<GetLikedGamesQuery, Result<List<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new GetLikedGamesQuery(userId), ct);
            return result.Match(Results.Ok);
        });

        // --- SuperAdmin Management Endpoints ---
        var adminGroup = group.MapGroup("/admin").RequireAuthorization(requireSuperAdmin);

        adminGroup.MapGet("/admins", async (
            IQueryHandler<GetAdminsQuery, Result<PagedList<AdminSummaryDto>>> handler,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var result = await handler.Handle(new GetAdminsQuery(page, pageSize), ct);
            return result.Match(Results.Ok);
        });

        adminGroup.MapGet("/{id:int}", async ([FromRoute] int id, IQueryHandler<GetUserDetailsQuery, Result<UserDetailsDto>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetUserDetailsQuery(id), ct);
            return result.Match(Results.Ok);
        });

        adminGroup.MapGet("/pending", async (
            IQueryHandler<GetPendingAdminsQuery, Result<PagedList<PendingAdminDto>>> handler,
            CancellationToken ct,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10) =>
        {
            var result = await handler.Handle(new GetPendingAdminsQuery(page, pageSize), ct);
            return result.Match(Results.Ok);
        });

        adminGroup.MapPut("/{id:int}/status", async ([FromRoute] int id, [FromBody] bool isActive, ICommandHandler<UpdateUserStatusCommand, Result> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new UpdateUserStatusCommand(id, isActive), ct);
            return result.Match(() => Results.NoContent());
        });

        adminGroup.MapDelete("/{id:int}", async ([FromRoute] int id, ClaimsPrincipal user, ICommandHandler<RemoveUserCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var currentUserId = int.Parse(userIdString!);

            var result = await handler.Handle(new RemoveUserCommand(id, currentUserId), ct);
            return result.Match(() => Results.NoContent());
        });

        // --- Custom Lists Endpoints ---
        var listsGroup = group.MapGroup("/me/lists");

        listsGroup.MapGet("/", async (ClaimsPrincipal user, IQueryHandler<GetUserCustomListsQuery, Result<List<CustomListSummaryDto>>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await handler.Handle(new GetUserCustomListsQuery(int.Parse(userIdString!)), ct);
            return result.Match(Results.Ok);
        });

        listsGroup.MapPost("/", async (CreateListRequest request, ClaimsPrincipal user, ICommandHandler<CreateCustomListCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await handler.Handle(new CreateCustomListCommand(int.Parse(userIdString!), request.Name), ct);
            return result.Match(id => Results.Ok(new { Id = id }));
        });

        listsGroup.MapGet("/{listId:int}", async ([FromRoute] int listId, ClaimsPrincipal user, IQueryHandler<GetCustomListDetailsQuery, Result<CustomListDetailsDto>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await handler.Handle(new GetCustomListDetailsQuery(int.Parse(userIdString!), listId), ct);
            return result.Match(Results.Ok);
        });

        listsGroup.MapDelete("/{listId:int}", async ([FromRoute] int listId, ClaimsPrincipal user, ICommandHandler<DeleteCustomListCommand, Result> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await handler.Handle(new DeleteCustomListCommand(int.Parse(userIdString!), listId), ct);
            return result.Match(() => Results.NoContent());
        });

        listsGroup.MapPost("/{listId:int}/games/{gameId:int}", async ([FromRoute] int listId, [FromRoute] int gameId, ClaimsPrincipal user, ICommandHandler<ToggleGameInListCommand, Result<bool>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await handler.Handle(new ToggleGameInListCommand(int.Parse(userIdString!), listId, gameId), ct);
            return result.Match(isAdded => Results.Ok(new { GameId = gameId, IsAddedToList = isAdded }));
        });
    }
}