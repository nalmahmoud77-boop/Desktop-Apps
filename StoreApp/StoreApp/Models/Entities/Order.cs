using System.ComponentModel.DataAnnotations;
using StoreApp.Enums;

namespace StoreApp.Models.Entities
{
    public class Order
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(30)]
        public string OrderNumber { get; set; } = string.Empty;

        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime OrderDate { get; set; } = DateTime.Now;

        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        public decimal Subtotal { get; set; }

        public decimal Tax { get; set; }

        public decimal Shipping { get; set; }

        public decimal Discount { get; set; }

        public decimal Total { get; set; }

        [MaxLength(500)]
        public string Notes { get; set; } = string.Empty;

        public List<OrderItem> Items { get; set; } = new();
    }
}
