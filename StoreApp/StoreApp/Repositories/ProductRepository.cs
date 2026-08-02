using StoreApp.Data;
using StoreApp.Enums;
using StoreApp.Models.Entities;

namespace StoreApp.Repositories
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(StoreDbContext context) : base(context) { }

        public IEnumerable<Product> Search(string? term, ProductCategory? category, decimal? minPrice, decimal? maxPrice)
        {
            IEnumerable<Product> q = _set.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(term))
            {
                var t = term.Trim().ToLowerInvariant();
                q = q.Where(p => p.Name.ToLowerInvariant().Contains(t)
                              || p.Sku.ToLowerInvariant().Contains(t)
                              || p.Brand.ToLowerInvariant().Contains(t));
            }

            if (category.HasValue)
                q = q.Where(p => p.Category == category.Value);

            if (minPrice.HasValue)
                q = q.Where(p => p.EffectivePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(p => p.EffectivePrice <= maxPrice.Value);

            return q.ToList();
        }

        public IEnumerable<Product> GetTopRated(int count) =>
            _set.AsEnumerable().OrderByDescending(p => p.Rating).ThenByDescending(p => p.ReviewsCount).Take(count).ToList();

        public IEnumerable<Product> GetLowStock(int threshold) =>
            _set.Where(p => p.Stock <= threshold).ToList();
    }
}
