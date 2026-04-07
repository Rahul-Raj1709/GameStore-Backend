using GameStore.Domain.Constants;

namespace GameStore.Domain.Entities;

public class User
{
    public int Id { get; set; }
    public string IdentityId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Role { get; set; } = RoleConstants.Customer;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }

    public List<Game> OwnedGames { get; set; } = [];
    public List<Game> LikedGames { get; set; } = [];
}