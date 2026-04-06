using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GameStore.Application.Features.Users.Queries;
using MediatR;

namespace GameStore.WebApi.Endpoints;

public static class UsersEndpoints
{
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization(); // All user endpoints require auth

        group.MapGet("/me/likes", async (ClaimsPrincipal user, IMediator mediator, CancellationToken ct) =>
        {
            var userIdString = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var userId = int.Parse(userIdString!);

            var likedGames = await mediator.Send(new GetLikedGamesQuery(userId), ct);
            return Results.Ok(likedGames);
        });
    }
}