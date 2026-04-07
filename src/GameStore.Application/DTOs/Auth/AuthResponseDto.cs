namespace GameStore.Application.DTOs.Auth;

public record AuthResponseDto(int Id, string Username, string Email, string Role, string Token, string RefreshToken);