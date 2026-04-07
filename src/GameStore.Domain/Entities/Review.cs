namespace GameStore.Domain.Entities;

public class Review
{
    public int Id { get; set; }

    public int GameId { get; set; }
    public Game? Game { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public int Rating { get; set; } // 1 to 5 stars
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}