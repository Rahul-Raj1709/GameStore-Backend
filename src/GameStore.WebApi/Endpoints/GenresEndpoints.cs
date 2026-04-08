using GameStore.Application.DTOs;
using GameStore.Application.Features.Games.Commands;
using GameStore.Application.Features.Games.Queries;
using GameStore.Application.Messaging;
using GameStore.Domain.Constants;
using GameStore.Domain.Shared;
using GameStore.WebApi.Extensions;
using GameStore.WebApi.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.WebApi.Endpoints;

public record CreateGenreRequest(string Name);

public static class GenresEndpoints
{
    public static void MapGenresEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/genres").AddEndpointFilter<ValidationFilter>();

        // Only Admins and SuperAdmins can add genres
        var requireAdmin = new AuthorizeAttribute { Roles = $"{RoleConstants.SuperAdmin},{RoleConstants.Admin}" };

        // GET: /api/genres (Publicly accessible for dropdowns)
        group.MapGet("/", async (IQueryHandler<GetGenresQuery, Result<List<GenreDto>>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new GetGenresQuery(), ct);
            return result.Match(Results.Ok);
        });

        // POST: /api/genres (Restricted)
        group.MapPost("/", async (CreateGenreRequest request, ICommandHandler<CreateGenreCommand, Result<int>> handler, CancellationToken ct) =>
        {
            var result = await handler.Handle(new CreateGenreCommand(request.Name), ct);
            return result.Match(id => Results.Ok(new { Id = id, Message = "Genre added successfully." }));
        })
        .RequireAuthorization(requireAdmin);
    }
}