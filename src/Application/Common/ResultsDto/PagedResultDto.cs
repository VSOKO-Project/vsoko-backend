namespace Application.Common.Results;

public class PagedResultDto<T>
{
    public List<T>? Items { get; init; }
    public int TotalPages { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int Page { get; init; }
}
