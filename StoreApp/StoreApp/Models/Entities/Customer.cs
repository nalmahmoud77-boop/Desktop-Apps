using System.ComponentModel.DataAnnotations;

namespace StoreApp.Models.Entities
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(250)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(80)]
        public string City { get; set; } = string.Empty;

        [MaxLength(80)]
        public string Country { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public List<Order> Orders { get; set; } = new();
    }
}
