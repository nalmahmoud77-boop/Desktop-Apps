using StoreApp.Enums;
using StoreApp.Models.DTOs;
using StoreApp.Models.Entities;
using StoreApp.Repositories;

namespace StoreApp.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repo;

        public ProductService(IProductRepository repo) { _repo = repo; }

        public IEnumerable<ProductDto> GetAll() => _repo.GetAll().Select(ToDto);

        public ProductDto? GetById(int id)
        {
            var p = _repo.GetById(id);
            return p == null ? null : ToDto(p);
        }

        public IEnumerable<ProductDto> Search(string? term, ProductCategory? category, decimal? minPrice, decimal? maxPrice) =>
            _repo.Search(term, category, minPrice, maxPrice).Select(ToDto);

        public ProductDto Create(ProductDto dto)
        {
            var entity = new Product
            {
                Sku = dto.Sku,
                Name = dto.Name,
                Description = dto.Description,
                Brand = dto.Brand,
                Category = dto.Category,
                Status = dto.Status,
                Price = dto.Price,
                DiscountPrice = dto.DiscountPrice,
                Stock = dto.Stock,
                Rating = dto.Rating,
                ReviewsCount = dto.ReviewsCount,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _repo.Add(entity);
            return ToDto(entity);
        }

        public void Update(ProductDto dto)
        {
            var entity = _repo.GetById(dto.Id);
            if (entity == null) return;
            entity.Sku = dto.Sku;
            entity.Name = dto.Name;
            entity.Description = dto.Description;
            entity.Brand = dto.Brand;
            entity.Category = dto.Category;
            entity.Status = dto.Status;
            entity.Price = dto.Price;
            entity.DiscountPrice = dto.DiscountPrice;
            entity.Stock = dto.Stock;
            entity.ImageUrl = dto.ImageUrl;
            entity.UpdatedAt = DateTime.Now;
            _repo.Update(entity);
        }

        public void Delete(int id)
        {
            var entity = _repo.GetById(id);
            if (entity != null) _repo.Delete(entity);
        }

        public IEnumerable<ProductDto> GetTopRated(int count) => _repo.GetTopRated(count).Select(ToDto);

        public IEnumerable<ProductDto> GetLowStock(int threshold) => _repo.GetLowStock(threshold).Select(ToDto);

        private static ProductDto ToDto(Product p) => new()
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Description = p.Description,
            Brand = p.Brand,
            Category = p.Category,
            Status = p.Status,
            Price = p.Price,
            DiscountPrice = p.DiscountPrice,
            Stock = p.Stock,
            Rating = p.Rating,
            ReviewsCount = p.ReviewsCount,
            ImageUrl = p.ImageUrl
        };
    }
}
