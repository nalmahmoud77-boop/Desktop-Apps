using Microsoft.EntityFrameworkCore;
using StoreApp.Data;
using StoreApp.Enums;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(StoreDbContext context) : base(context) { }

        public IEnumerable<Order> GetWithDetails() =>
            _set.Include(o => o.Customer).Include(o => o.Items).ToList();

        public Order? GetWithDetailsById(int id) =>
            _set.Include(o => o.Customer).Include(o => o.Items).FirstOrDefault(o => o.Id == id);

        public IEnumerable<Order> GetByStatus(OrderStatus status) =>
            _set.Include(o => o.Customer).Include(o => o.Items).Where(o => o.Status == status).ToList();

        public IEnumerable<Order> GetByCustomer(int customerId) =>
            _set.Include(o => o.Items).Where(o => o.CustomerId == customerId).ToList();
    }
}
