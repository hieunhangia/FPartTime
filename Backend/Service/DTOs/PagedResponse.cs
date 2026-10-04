namespace Service.DTOs;

public class PagedResponse<T>(List<T> items, int totalCount)
{
    public List<T> Items { get; set; } = items;
    public int TotalCount { get; set; } = totalCount;
}