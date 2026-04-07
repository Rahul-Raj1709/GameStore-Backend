namespace GameStore.Application.DTOs;

public record CustomListSummaryDto(int Id, string Name, int GameCount);
public record CustomListDetailsDto(int Id, string Name, List<GameSummaryDto> Games);