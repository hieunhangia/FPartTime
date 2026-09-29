using System.ComponentModel.DataAnnotations;
using Repository.Constants;

namespace Service;

public class PagedAndSortedRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageIndex { get; set; } = 1;

    [Range(1, int.MaxValue, ErrorMessage = "Số mục trên trang phải lớn hơn hoặc bằng 1.")]
    public int PageSize { get; set; } = 5;

    public string? SortColumn { get; set; }
    public SortDirection? SortDirection { get; set; }
}

public class PagedAndSortedRequest<T> : PagedAndSortedRequest where T : new()
{
    public T Filter { get; set; } = new();
}