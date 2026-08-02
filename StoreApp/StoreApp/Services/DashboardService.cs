using StoreApp.Enums;
using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IProductService _products;
        private readonly IOrderService _orders;
        private readonly ICustomerService _customers;

        public DashboardService(IProductService products, IOrderService orders, ICustomerService customers)
        {
            _products = products;
            _orders = orders;
            _customers = customers;
        }

        public DashboardDto GetDashboard()
        {
            var allOrders = _orders.GetAll().ToList();
            var allProducts = _products.GetAll().ToList();
            var today = DateTime.Today;

            return new DashboardDto
            {
                TotalProducts = allProducts.Count,
                TotalOrders = allOrders.Count,
                TotalCustomers = _customers.GetAll().Count(),
                LowStockProducts = allProducts.Count(p => p.Stock <= 10),
                TotalRevenue = allOrders.Where(o => o.Status != OrderStatus.Cancelled && o.Status != OrderStatus.Refunded).Sum(o => o.Total),
                TodayRevenue = allOrders.Where(o => o.OrderDate.Date == today && o.Status != OrderStatus.Cancelled).Sum(o => o.Total),
                PendingOrders = allOrders.Count(o => o.Status == OrderStatus.Pending),
                DeliveredOrders = allOrders.Count(o => o.Status == OrderStatus.Delivered),
                TopProducts = _products.GetTopRated(5).ToList(),
                RecentOrders = allOrders.Take(5).ToList()
            };
        }
    }
}
