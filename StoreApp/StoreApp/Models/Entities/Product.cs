using System.ComponentModel.DataAnnotations;
using StoreApp.Enums;

namespace StoreApp.Models.Entities
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Brand { get; set; } = string.Empty;

        public ProductCategory Category { get; set; } = ProductCategory.Other;

        public ProductStatus Status { get; set; } = ProductStatus.Active;

        public decimal Price { get; set; }

        public decimal? DiscountPrice { get; set; }

        public int Stock { get; set; }

        public double Rating { get; set; }

        public int ReviewsCount { get; set; }

        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public decimal EffectivePrice => DiscountPrice ?? Price;

        public bool HasDiscount => DiscountPrice.HasValue && DiscountPrice.Value < Price;
    }
}
