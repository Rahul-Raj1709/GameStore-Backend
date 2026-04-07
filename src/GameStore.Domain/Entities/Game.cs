using GameStore.Domain.Shared;

namespace GameStore.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal? Price { get; set; }
    public DateOnly ReleaseDate { get; set; }

    public int GenreId { get; set; }
    public Genre? Genre { get; set; }

    public int OwnerId { get; set; }
    public User? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CustomList> CustomLists { get; set; } = [];

    // --- NEW: For Sorting Features ---
    public double AverageRating { get; set; } = 0;
    public List<User> LikedByUsers { get; set; } = [];
}