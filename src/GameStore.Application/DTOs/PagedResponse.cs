namespace GameStore.Application.DTOs;

public record PagedResponse<T>(
    List<T> Data,
    int? NextCursor
);