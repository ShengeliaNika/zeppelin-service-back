namespace Zeppelin.Dtos.Common;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int TotalCount, int Skip, int Take);
