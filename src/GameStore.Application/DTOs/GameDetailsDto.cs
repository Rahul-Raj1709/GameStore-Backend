namespace GameStore.Application.DTOs;

public record GameDetailsDto(
    int Id,
    string Name,
    string Description,
    string? ImageUrl,
    int GenreId,
    string Genre,
    decimal? Price,
    DateOnly ReleaseDate,
    DateTime AddedAt,
    int TotalLikes,
    string OwnerName
);