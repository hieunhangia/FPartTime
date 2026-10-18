using System.ComponentModel.DataAnnotations;

namespace Contracts.Common;

public class PaginationRequestDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn hoặc bằng 1.")]
    public int PageNumber
    {
        get;
        init => field = value < 1 ? 1 : value;
    } = 1;

    [Range(1, 100, ErrorMessage = "Số lượng bản ghi mỗi trang phải từ 1 đến 100.")]
    public int PageSize
    {
        get;
        init => field = value < 1 ? 10 : (value > 100 ? 100 : value);
    } = 10;
}