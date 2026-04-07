namespace GameStore.Application.DTOs;

public record ReviewDto(int Id, int GameId, int UserId, string Username, int Rating, string? Comment, DateTime CreatedAt, DateTime? UpdatedAt);