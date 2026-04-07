using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Entities;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Users.Commands;

// --- 1. CREATE LIST ---
public record CreateCustomListCommand(int UserId, string Name) : ICommand<Result<int>>;

public class CreateCustomListCommandHandler(IApplicationDbContext context) : ICommandHandler<CreateCustomListCommand, Result<int>>
{
    public async Task<Result<int>> Handle(CreateCustomListCommand request, CancellationToken cancellationToken)
    {
        var list = new CustomList { UserId = request.UserId, Name = request.Name };
        context.CustomLists.Add(list);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(list.Id);
    }
}

// --- 2. TOGGLE GAME IN LIST ---
public record ToggleGameInListCommand(int UserId, int ListId, int GameId) : ICommand<Result<bool>>;

public class ToggleGameInListCommandHandler(IApplicationDbContext context) : ICommandHandler<ToggleGameInListCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(ToggleGameInListCommand request, CancellationToken cancellationToken)
    {
        var list = await context.CustomLists.Include(l => l.Games).FirstOrDefaultAsync(l => l.Id == request.ListId && l.UserId == request.UserId, cancellationToken);
        var game = await context.Games.FindAsync([request.GameId], cancellationToken);

        if (list == null || game == null) return Result.Failure<bool>(new Error("List.Invalid", "List or Game not found."));

        var alreadyInList = list.Games.Any(g => g.Id == request.GameId);
        bool isAddedNow;

        if (alreadyInList)
        {
            list.Games.Remove(game);
            isAddedNow = false;
        }
        else
        {
            list.Games.Add(game);
            isAddedNow = true;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success(isAddedNow);
    }
}

// --- 3. DELETE LIST ---
public record DeleteCustomListCommand(int UserId, int ListId) : ICommand<Result>;

public class DeleteCustomListCommandHandler(IApplicationDbContext context) : ICommandHandler<DeleteCustomListCommand, Result>
{
    public async Task<Result> Handle(DeleteCustomListCommand request, CancellationToken cancellationToken)
    {
        var list = await context.CustomLists.FirstOrDefaultAsync(l => l.Id == request.ListId && l.UserId == request.UserId, cancellationToken);
        if (list == null) return Result.Failure(new Error("List.NotFound", "List not found."));

        context.CustomLists.Remove(list);
        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}