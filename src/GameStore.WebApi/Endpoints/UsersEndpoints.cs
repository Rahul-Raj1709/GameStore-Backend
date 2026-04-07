using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Application.DTOs;
using GameStore.Application.Features.Users.Queries;
using GameStore.Application.Messaging;
using GameStore.Domain.Shared;
using GameStore.WebApi.Extensions;

namespace GameStore.WebApi.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/me/likes", async (ClaimsPrincipal user, IQueryHandler<GetLikedGamesQuery, Result<List<GameSummaryDto>>> handler, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdString!);

            var result = await handler.Handle(new GetLikedGamesQuery(userId), ct);
            return result.Match(Results.Ok);
        });
    }
}