using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Models.Entities;
using StoreApp.Repositories;

namespace StoreApp.Services
{
    public class OrderService : IOrderService
    {
        private const decimal TaxRate = 0.10m;
        private const decimal FlatShipping = 9.99m;
        private const decimal FreeShippingThreshold = 100m;

        private readonly IOrderRepository _orders;
        private readonly IProductRepository _products;
        private readonly ICustomerRepository _customers;

        public OrderService(IOrderRepository orders, IProductRepository products, ICustomerRepository customers)
        {
            _orders = orders;
            _products = products;
            _customers = customers;
        }

        public IEnumerable<OrderDto> GetAll() =>
            _orders.GetWithDetails().OrderByDescending(o => o.OrderDate).Select(ToDto);

        public OrderDto? GetById(int id)
        {
            var o = _orders.GetWithDetailsById(id);
            return o == null ? null : ToDto(o);
        }

        public IEnumerable<OrderDto> GetByStatus(OrderStatus status) =>
            _orders.GetByStatus(status).OrderByDescending(o => o.OrderDate).Select(ToDto);

        public OrderDto Checkout(int customerId, IEnumerable<CartItemDto> cart, PaymentMethod paymentMethod, decimal discount, string notes)
        {
            var customer = _customers.GetById(customerId)
                ?? throw new InvalidOperationException("Customer not found.");

            var items = cart.ToList();
            if (items.Count == 0) throw new InvalidOperationException("Cart is empty.");

            var order = new Order
            {
                OrderNumber = "ORD-" + Random.Shared.Next(10000, 99999),
                CustomerId = customerId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Confirmed,
                PaymentMethod = paymentMethod,
                Notes = notes ?? string.Empty
            };

            foreach (var item in items)
            {
                var product = _products.GetById(item.ProductId)
                    ?? throw new InvalidOperationException($"Product {item.ProductId} missing.");

                if (product.Stock < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}.");

                product.Stock -= item.Quantity;
                _products.Update(product);

                order.Items.Add(new OrderItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.EffectivePrice,
                    Quantity = item.Quantity
                });
            }

            order.Subtotal = order.Items.Sum(i => i.UnitPrice * i.Quantity);
            order.Tax = Math.Round(order.Subtotal * TaxRate, 2);
            order.Shipping = order.Subtotal >= FreeShippingThreshold ? 0 : FlatShipping;
            order.Discount = Math.Max(0, discount);
            order.Total = order.Subtotal + order.Tax + order.Shipping - order.Discount;

            _orders.Add(order);
            return ToDto(_orders.GetWithDetailsById(order.Id)!);
        }

        public void UpdateStatus(int orderId, OrderStatus newStatus)
        {
            var order = _orders.GetById(orderId);
            if (order == null) return;
            order.Status = newStatus;
            _orders.Update(order);
        }

        public void Cancel(int orderId) => UpdateStatus(orderId, OrderStatus.Cancelled);

        private static OrderDto ToDto(Order o) => new()
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            CustomerId = o.CustomerId,
            CustomerName = o.Customer?.FullName ?? "—",
            OrderDate = o.OrderDate,
            Status = o.Status,
            PaymentMethod = o.PaymentMethod,
            Subtotal = o.Subtotal,
            Tax = o.Tax,
            Shipping = o.Shipping,
            Discount = o.Discount,
            Total = o.Total,
            Notes = o.Notes,
            Items = o.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}
