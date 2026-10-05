using System.ComponentModel.DataAnnotations;

namespace Service.DTOs;

public class PagedRequestDto
{
    [Required(ErrorMessage = "Số trang không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageIndex { get; set; }

    [Required(ErrorMessage = "Số mục trên trang không được để trống.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số mục trên trang phải lớn hơn hoặc bằng 1.")]
    public int PageSize { get; set; }
}

public class PagedRequestDto<TFilter> : PagedRequestDto where TFilter : new()
{
    public TFilter Filter { get; set; } = new();
}