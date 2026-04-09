using GameStore.Application.Interfaces;
using GameStore.Application.Messaging;
using GameStore.Domain.Entities;
using GameStore.Domain.Errors;
using GameStore.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid; // <-- Added

namespace GameStore.Application.Features.Games.Commands;

// --- 1. ADD REVIEW ---
public record AddReviewCommand(int GameId, int UserId, int Rating, string? Comment) : ICommand<Result<int>>;

public class AddReviewCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<AddReviewCommand, Result<int>> // <-- Injected HybridCache
{
    public async Task<Result<int>> Handle(AddReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5) return Result.Failure<int>(new Error("Review.InvalidRating", "Rating must be between 1 and 5."));

        var game = await context.Games.FindAsync([request.GameId], cancellationToken);
        if (game == null) return Result.Failure<int>(GameErrors.NotFound);

        var existingReview = await context.Reviews.AnyAsync(r => r.GameId == request.GameId && r.UserId == request.UserId, cancellationToken);
        if (existingReview) return Result.Failure<int>(new Error("Review.Duplicate", "You have already reviewed this game."));

        var review = new Review { GameId = request.GameId, UserId = request.UserId, Rating = request.Rating, Comment = request.Comment };
        context.Reviews.Add(review);
        await context.SaveChangesAsync(cancellationToken);

        await RecalculateAverageRatingAsync(context, game, cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{request.GameId}", cancellationToken);

        return Result.Success(review.Id);
    }

    internal static async Task RecalculateAverageRatingAsync(IApplicationDbContext ctx, Game game, CancellationToken ct)
    {
        // Casting to double? ensures that if there are no reviews, it returns null instead of throwing an exception, which we coalesce to 0.
        game.AverageRating = await ctx.Reviews.Where(r => r.GameId == game.Id).AverageAsync(r => (double?)r.Rating, ct) ?? 0;
        await ctx.SaveChangesAsync(ct);
    }
}

// --- 2. UPDATE REVIEW ---
public record UpdateReviewCommand(int ReviewId, int UserId, int Rating, string? Comment, bool IsSuperAdmin) : ICommand<Result>;

public class UpdateReviewCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<UpdateReviewCommand, Result> // <-- Injected HybridCache
{
    public async Task<Result> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        if (request.Rating < 1 || request.Rating > 5) return Result.Failure(new Error("Review.InvalidRating", "Rating must be between 1 and 5."));

        var review = await context.Reviews.Include(r => r.Game).FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);
        if (review == null) return Result.Failure(new Error("Review.NotFound", "Review not found."));

        if (!request.IsSuperAdmin && review.UserId != request.UserId) return Result.Failure(new Error("Auth.Forbidden", "You can only edit your own reviews."));

        review.Rating = request.Rating;
        review.Comment = request.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync(cancellationToken);
        await AddReviewCommandHandler.RecalculateAverageRatingAsync(context, review.Game!, cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{review.GameId}", cancellationToken);

        return Result.Success();
    }
}

// --- 3. DELETE REVIEW ---
public record DeleteReviewCommand(int ReviewId, int UserId, bool IsSuperAdmin) : ICommand<Result>;

public class DeleteReviewCommandHandler(IApplicationDbContext context, HybridCache cache) : ICommandHandler<DeleteReviewCommand, Result> // <-- Injected HybridCache
{
    public async Task<Result> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var review = await context.Reviews.Include(r => r.Game).FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);
        if (review == null) return Result.Failure(new Error("Review.NotFound", "Review not found."));

        if (!request.IsSuperAdmin && review.UserId != request.UserId) return Result.Failure(new Error("Auth.Forbidden", "You can only delete your own reviews."));

        context.Reviews.Remove(review);
        await context.SaveChangesAsync(cancellationToken);

        await AddReviewCommandHandler.RecalculateAverageRatingAsync(context, review.Game!, cancellationToken);

        // --- CACHE INVALIDATION ---
        await cache.RemoveAsync($"game-details-{review.GameId}", cancellationToken);

        return Result.Success();
    }
}