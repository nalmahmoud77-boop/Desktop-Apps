using StoreApp.Enums;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public interface IProductRepository : IRepository<Product>
    {
        IEnumerable<Product> Search(string? term, ProductCategory? category, decimal? minPrice, decimal? maxPrice);
        IEnumerable<Product> GetTopRated(int count);
        IEnumerable<Product> GetLowStock(int threshold);
    }
}
