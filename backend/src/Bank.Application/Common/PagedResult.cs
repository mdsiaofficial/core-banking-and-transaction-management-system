namespace Bank.Application.Common;

public record PagedResult<T>(
    IReadOnlyList<T> Items,
    string? NextCursor,
    bool HasMore,
    int TotalCount = 0);
