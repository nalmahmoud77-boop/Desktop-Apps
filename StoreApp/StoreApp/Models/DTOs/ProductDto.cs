using StoreApp.Enums;

namespace StoreApp.Models.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public ProductCategory Category { get; set; }
        public ProductStatus Status { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int Stock { get; set; }
        public double Rating { get; set; }
        public int ReviewsCount { get; set; }
        public string ImageUrl { get; set; } = string.Empty;

        public decimal EffectivePrice => DiscountPrice ?? Price;
        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice.Value < Price;
        public bool InStock => Stock > 0 && Status == ProductStatus.Active;
    }
}
