using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public interface ICartService
    {
        IReadOnlyList<CartItemDto> Items { get; }
        event Action? Changed;

        void Add(ProductDto product, int quantity = 1);
        void UpdateQuantity(int productId, int quantity);
        void Remove(int productId);
        void Clear();

        decimal Subtotal { get; }
        int TotalItems { get; }
    }
}
