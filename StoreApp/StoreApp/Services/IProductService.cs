using StoreApp.Enums;
using StoreApp.Models.DTOs;

namespace StoreApp.Services
{
    public interface IProductService
    {
        IEnumerable<ProductDto> GetAll();
        ProductDto? GetById(int id);
        IEnumerable<ProductDto> Search(string? term, ProductCategory? category, decimal? minPrice, decimal? maxPrice);
        ProductDto Create(ProductDto dto);
        void Update(ProductDto dto);
        void Delete(int id);
        IEnumerable<ProductDto> GetTopRated(int count);
        IEnumerable<ProductDto> GetLowStock(int threshold);
    }
}
