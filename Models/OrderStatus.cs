using System.Text.Json.Serialization;

namespace Webstore.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string AwaitingConfirmation = "AwaitingConfirmation";
        public const string Confirmed = "Confirmed";
        public const string Processing = "Processing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";
        public const string PaymentFailed = "PaymentFailed";

        public static readonly string[] All = new[] {
            Pending, AwaitingConfirmation, Confirmed, Processing, Shipped, Delivered, Cancelled, PaymentFailed
        };

        public static string GetDisplayName(string status) => status switch
        {
            Pending => "Chờ thanh toán",
            AwaitingConfirmation => "Chờ xác nhận thanh toán",
            Confirmed => "Đã xác nhận",
            Processing => "Đang chuẩn bị hàng",
            Shipped => "Đang vận chuyển",
            Delivered => "Đã giao hàng",
            Cancelled => "Đã hủy",
            PaymentFailed => "Thanh toán thất bại",
            _ => status
        };

        public static string GetBadgeClass(string status) => status switch
        {
            Pending => "bg-secondary",
            AwaitingConfirmation => "bg-warning text-dark",
            Confirmed => "bg-info",
            Processing => "bg-primary",
            Shipped => "bg-violet",
            Delivered => "bg-success",
            Cancelled => "bg-danger",
            PaymentFailed => "bg-danger",
            _ => "bg-secondary"
        };

        public static string GetIcon(string status) => status switch
        {
            Pending => "fa-clock",
            AwaitingConfirmation => "fa-hourglass-half",
            Confirmed => "fa-check-circle",
            Processing => "fa-boxes-packing",
            Shipped => "fa-truck-fast",
            Delivered => "fa-house-chimney",
            Cancelled => "fa-times-circle",
            PaymentFailed => "fa-times-circle",
            _ => "fa-question-circle"
        };

        public static bool CanTransitionTo(string from, string to) => (from, to) switch
        {
            (Pending, AwaitingConfirmation) => true,
            (Pending, Cancelled) => true,
            (AwaitingConfirmation, Confirmed) => true,
            (AwaitingConfirmation, PaymentFailed) => true,
            (AwaitingConfirmation, Cancelled) => true,
            (Confirmed, Processing) => true,
            (Confirmed, Cancelled) => true,
            (Processing, Shipped) => true,
            (Processing, Cancelled) => true,
            (Shipped, Delivered) => true,
            _ => false
        };

        public static int GetStep(string status) => status switch
        {
            Pending => 0,
            AwaitingConfirmation => 1,
            Confirmed => 2,
            Processing => 3,
            Shipped => 4,
            Delivered => 5,
            _ => -1
        };
    }
}
