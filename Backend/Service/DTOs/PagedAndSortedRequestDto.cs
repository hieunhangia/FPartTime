using Repository.Constants;

namespace Service.DTOs;

public class PagedAndSortedRequestDto : PagedRequestDto
{
    public string? SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }
}

public class PagedAndSortedRequestDto<TFilter> : PagedAndSortedRequestDto where TFilter : new()
{
    public TFilter Filter { get; set; } = new();
}