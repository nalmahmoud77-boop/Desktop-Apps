using StoreApp.Enums;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public interface IOrderRepository : IRepository<Order>
    {
        IEnumerable<Order> GetWithDetails();
        Order? GetWithDetailsById(int id);
        IEnumerable<Order> GetByStatus(OrderStatus status);
        IEnumerable<Order> GetByCustomer(int customerId);
    }
}
