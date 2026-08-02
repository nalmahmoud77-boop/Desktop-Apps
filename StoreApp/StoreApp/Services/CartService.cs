using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public class CartService : ICartService
    {
        private readonly List<CartItemDto> _items = new();

        public IReadOnlyList<CartItemDto> Items => _items;

        public event Action? Changed;

        public void Add(ProductDto product, int quantity = 1)
        {
            if (product == null || quantity <= 0) return;

            var existing = _items.FirstOrDefault(i => i.ProductId == product.Id);
            if (existing != null)
            {
                existing.Quantity = Math.Min(existing.Quantity + quantity, product.Stock);
            }
            else
            {
                _items.Add(new CartItemDto
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ImageUrl = product.ImageUrl,
                    UnitPrice = product.EffectivePrice,
                    Quantity = Math.Min(quantity, product.Stock),
                    AvailableStock = product.Stock
                });
            }

            Changed?.Invoke();
        }

        public void UpdateQuantity(int productId, int quantity)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return;

            if (quantity <= 0) _items.Remove(item);
            else item.Quantity = Math.Min(quantity, item.AvailableStock);

            Changed?.Invoke();
        }

        public void Remove(int productId)
        {
            var item = _items.FirstOrDefault(i => i.ProductId == productId);
            if (item == null) return;
            _items.Remove(item);
            Changed?.Invoke();
        }

        public void Clear()
        {
            _items.Clear();
            Changed?.Invoke();
        }

        public decimal Subtotal => _items.Sum(i => i.LineTotal);
        public int TotalItems => _items.Sum(i => i.Quantity);
    }
}
