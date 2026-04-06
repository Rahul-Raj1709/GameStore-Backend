using GameStore.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GameStore.Application.Features.Games.Commands;

// Returns a boolean indicating if the game is currently "Liked" after the toggle
public record ToggleLikeCommand(int UserId, int GameId) : IRequest<bool>;

public class ToggleLikeCommandHandler(IApplicationDbContext context)
    : IRequestHandler<ToggleLikeCommand, bool>
{
    public async Task<bool> Handle(ToggleLikeCommand request, CancellationToken cancellationToken)
    {
        // Fetch user and include their currently liked games
        var user = await context.Users
            .Include(u => u.LikedGames)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        var game = await context.Games.FindAsync([request.GameId], cancellationToken);

        if (user == null || game == null)
            throw new Exception("User or Game not found.");

        var alreadyLiked = user.LikedGames.Any(g => g.Id == request.GameId);
        bool isLikedNow;

        if (alreadyLiked)
        {
            // Unlike
            user.LikedGames.Remove(game);
            game.TotalLikes--;
            isLikedNow = false;
        }
        else
        {
            // Like
            user.LikedGames.Add(game);
            game.TotalLikes++;
            isLikedNow = true;
        }

        await context.SaveChangesAsync(cancellationToken);

        return isLikedNow;
    }
}