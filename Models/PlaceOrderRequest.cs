namespace Webstore.Models;

public class PlaceOrderRequest
{
    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";
    public string CustomerAddress { get; set; } = "";
    public string Notes { get; set; } = "";
    public string PaymentMethod { get; set; } = "qr";
}
