using System.ComponentModel.DataAnnotations;

namespace Webstore.Models;

public class PlaceOrderRequest
{
    [Required(ErrorMessage = "Họ và tên là bắt buộc.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Họ và tên phải từ 2 đến 100 ký tự.")]
    public string CustomerName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại là bắt buộc.")]
    [RegularExpression(@"^0\d{9}$", ErrorMessage = "Số điện thoại không hợp lệ (10 chữ số, bắt đầu bằng 0).")]
    public string CustomerPhone { get; set; } = "";

    [Required(ErrorMessage = "Địa chỉ giao hàng là bắt buộc.")]
    [StringLength(200, MinimumLength = 10, ErrorMessage = "Địa chỉ phải từ 10 đến 200 ký tự.")]
    public string CustomerAddress { get; set; } = "";

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
    public string Notes { get; set; } = "";

    public string PaymentMethod { get; set; } = "qr";

    public int[]? SelectedProductIds { get; set; }
}
