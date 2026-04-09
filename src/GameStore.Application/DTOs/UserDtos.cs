namespace GameStore.Application.DTOs;

public record AdminSummaryDto(int Id, string Name, string Email, string? Username, bool IsActive, DateTime CreatedAt);

public record PendingAdminDto(int Id, string Name, string Email, DateTime CreatedAt);

public record UserDetailsDto(
    int Id,
    string Name,
    string? Username,
    string Email,
    string Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? LastLogin,
    int OwnedGamesCount,
    int ReviewsCount
);