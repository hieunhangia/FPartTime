using Repository.Constants;

namespace Service.DTOs;

public class PagedAndSortedRequest : PagedRequest
{
    public string? SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }
}

public class PagedAndSortedRequest<TFilter> : PagedAndSortedRequest where TFilter : new()
{
    public TFilter Filter { get; set; } = new();
}