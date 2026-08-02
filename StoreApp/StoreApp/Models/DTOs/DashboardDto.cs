namespace StoreApp.Models.DTOs
{
    public class DashboardDto
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int LowStockProducts { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int PendingOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public List<ProductDto> TopProducts { get; set; } = new();
        public List<OrderDto> RecentOrders { get; set; } = new();
    }
}
