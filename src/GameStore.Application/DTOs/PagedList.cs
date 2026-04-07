namespace GameStore.Application.DTOs;

public record PagedList<T>(
    List<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    bool HasNextPage);