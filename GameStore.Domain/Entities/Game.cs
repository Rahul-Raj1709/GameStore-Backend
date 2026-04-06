namespace GameStore.Domain.Entities;

public class Game
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    // --- NEW: Ownership ---
    public int OwnerId { get; set; }
    public User? Owner { get; set; }
    // ----------------------

    public int GenreId { get; set; }
    public Genre? Genre { get; set; }
    public decimal? Price { get; set; }
    public DateOnly ReleaseDate { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int TotalLikes { get; set; } = 0;

    public List<User> LikedByUsers { get; set; } = [];
}