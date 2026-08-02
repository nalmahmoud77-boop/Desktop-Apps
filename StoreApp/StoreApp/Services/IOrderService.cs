using StoreApp.Enums;
using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public interface IOrderService
    {
        IEnumerable<OrderDto> GetAll();
        OrderDto? GetById(int id);
        IEnumerable<OrderDto> GetByStatus(OrderStatus status);
        OrderDto Checkout(int customerId, IEnumerable<CartItemDto> cart, PaymentMethod paymentMethod, decimal discount, string notes);
        void UpdateStatus(int orderId, OrderStatus newStatus);
        void Cancel(int orderId);
    }
}
