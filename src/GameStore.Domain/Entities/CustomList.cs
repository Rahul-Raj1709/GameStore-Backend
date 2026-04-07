namespace GameStore.Domain.Entities;

public class CustomList
{
    public int Id { get; set; }
    public required string Name { get; set; }

    public int UserId { get; set; }
    public User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Many-to-Many relationship with Games
    public List<Game> Games { get; set; } = [];
}