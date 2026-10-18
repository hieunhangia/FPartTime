namespace Contracts.Common;

public class PagedResponseDto<T>
{
    public required long TotalCount { get; set; }
    public required IEnumerable<T> Items { get; set; }
}