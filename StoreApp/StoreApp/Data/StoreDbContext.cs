using Microsoft.EntityFrameworkCore;
using StoreApp.Enums;
using StoreApp.Models.Entities;

namespace StoreApp.Data
{
    public class StoreDbContext : DbContext
    {
        public StoreDbContext(DbContextOptions<StoreDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>().Ignore(p => p.EffectivePrice).Ignore(p => p.HasDiscount);
            modelBuilder.Entity<OrderItem>().Ignore(i => i.LineTotal);

            modelBuilder.Entity<Order>()
                .HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Orders)
                .WithOne(o => o.Customer)
                .HasForeignKey(o => o.CustomerId);

            base.OnModelCreating(modelBuilder);
        }

        public static void SeedData(StoreDbContext db)
        {
            if (db.Users.Any()) return;

            // Demo seed accounts. Passwords are hashed with BCrypt at seed time so no
            // plaintext credential is ever persisted, even in the sample database.
            db.Users.AddRange(
                new User { Username = "admin", PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 11), FullName = "Administrator", Email = "admin@store.com", Role = UserRole.Admin },
                new User { Username = "manager", PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager", workFactor: 11), FullName = "Store Manager", Email = "manager@store.com", Role = UserRole.Manager }
            );

            db.Products.AddRange(
                new Product { Sku = "EL-001", Name = "Wireless Bluetooth Headphones", Brand = "SoundMax", Category = ProductCategory.Electronics, Price = 89.99m, DiscountPrice = 69.99m, Stock = 50, Rating = 4.6, ReviewsCount = 412, ImageUrl = "https://images.unsplash.com/photo-1505740420928-5e560c06d30e?w=400", Description = "Premium noise-cancelling wireless headphones with 30-hour battery life." },
                new Product { Sku = "EL-002", Name = "Smart 4K UHD TV 55\"", Brand = "VisionPro", Category = ProductCategory.Electronics, Price = 599.00m, Stock = 18, Rating = 4.4, ReviewsCount = 256, ImageUrl = "https://images.unsplash.com/photo-1593359677879-a4bb92f829d1?w=400", Description = "Crystal-clear 4K UHD smart TV with HDR and built-in streaming apps." },
                new Product { Sku = "EL-003", Name = "Smartphone Pro 128GB", Brand = "TechOne", Category = ProductCategory.Electronics, Price = 999.00m, DiscountPrice = 899.00m, Stock = 35, Rating = 4.8, ReviewsCount = 982, ImageUrl = "https://images.unsplash.com/photo-1511707171634-5f897ff02aa9?w=400", Description = "Flagship smartphone with triple-camera system and OLED display." },
                new Product { Sku = "EL-004", Name = "Gaming Laptop 16\" RTX", Brand = "NovaTech", Category = ProductCategory.Electronics, Price = 1499.00m, Stock = 12, Rating = 4.7, ReviewsCount = 187, ImageUrl = "https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400", Description = "High performance gaming laptop with RTX graphics and 165Hz display." },
                new Product { Sku = "CL-001", Name = "Premium Cotton T-Shirt", Brand = "UrbanWear", Category = ProductCategory.Clothing, Price = 24.99m, Stock = 200, Rating = 4.3, ReviewsCount = 540, ImageUrl = "https://images.unsplash.com/photo-1521572163474-6864f9cf17ab?w=400", Description = "Soft 100% cotton t-shirt available in multiple colors." },
                new Product { Sku = "CL-002", Name = "Classic Denim Jeans", Brand = "UrbanWear", Category = ProductCategory.Clothing, Price = 59.99m, DiscountPrice = 39.99m, Stock = 120, Rating = 4.5, ReviewsCount = 320, ImageUrl = "https://images.unsplash.com/photo-1542272604-787c3835535d?w=400", Description = "Slim-fit denim jeans, comfortable stretch fabric." },
                new Product { Sku = "CL-003", Name = "Winter Puffer Jacket", Brand = "ArcticLine", Category = ProductCategory.Clothing, Price = 149.99m, Stock = 45, Rating = 4.6, ReviewsCount = 110, ImageUrl = "https://images.unsplash.com/photo-1551028719-00167b16eac5?w=400", Description = "Warm puffer jacket with water-resistant outer shell." },
                new Product { Sku = "HK-001", Name = "Stainless Steel Cookware Set", Brand = "ChefKitchen", Category = ProductCategory.HomeAndKitchen, Price = 199.99m, Stock = 28, Rating = 4.7, ReviewsCount = 95, ImageUrl = "https://images.unsplash.com/photo-1556909114-f6e7ad7d3136?w=400", Description = "10-piece premium stainless steel cookware set." },
                new Product { Sku = "HK-002", Name = "Espresso Coffee Machine", Brand = "BrewMaster", Category = ProductCategory.HomeAndKitchen, Price = 349.00m, DiscountPrice = 299.00m, Stock = 22, Rating = 4.5, ReviewsCount = 167, ImageUrl = "https://images.unsplash.com/photo-1572119865084-43c285814d63?w=400", Description = "Professional espresso machine with milk frother." },
                new Product { Sku = "BK-001", Name = "Atomic Habits", Brand = "Penguin", Category = ProductCategory.Books, Price = 16.99m, Stock = 300, Rating = 4.9, ReviewsCount = 12500, ImageUrl = "https://images.unsplash.com/photo-1544947950-fa07a98d237f?w=400", Description = "Bestselling book on building good habits and breaking bad ones." },
                new Product { Sku = "BK-002", Name = "Clean Code Handbook", Brand = "PrenticeHall", Category = ProductCategory.Books, Price = 35.50m, Stock = 80, Rating = 4.7, ReviewsCount = 2400, ImageUrl = "https://images.unsplash.com/photo-1532012197267-da84d127e765?w=400", Description = "A handbook of agile software craftsmanship for developers." },
                new Product { Sku = "BT-001", Name = "Organic Face Serum", Brand = "GlowSkin", Category = ProductCategory.Beauty, Price = 29.99m, Stock = 150, Rating = 4.4, ReviewsCount = 88, ImageUrl = "https://images.unsplash.com/photo-1556228720-195a672e8a03?w=400", Description = "Vitamin C anti-aging organic face serum." },
                new Product { Sku = "SP-001", Name = "Yoga Mat Premium", Brand = "FitLife", Category = ProductCategory.Sports, Price = 39.99m, Stock = 75, Rating = 4.6, ReviewsCount = 230, ImageUrl = "https://images.unsplash.com/photo-1601925260368-ae2f83cf8b7f?w=400", Description = "Eco-friendly non-slip yoga mat with carrying strap." },
                new Product { Sku = "SP-002", Name = "Adjustable Dumbbells 40kg", Brand = "FitLife", Category = ProductCategory.Sports, Price = 249.00m, Stock = 0, Rating = 4.8, ReviewsCount = 145, ImageUrl = "https://images.unsplash.com/photo-1517836357463-d25dfeac3438?w=400", Description = "Adjustable dumbbells perfect for home workouts.", Status = ProductStatus.OutOfStock },
                new Product { Sku = "TY-001", Name = "Wooden Building Blocks Set", Brand = "KidJoy", Category = ProductCategory.Toys, Price = 34.99m, Stock = 90, Rating = 4.7, ReviewsCount = 68, ImageUrl = "https://images.unsplash.com/photo-1558060370-d644479cb6f7?w=400", Description = "Educational wooden building blocks set, 100 pieces." },
                new Product { Sku = "GR-001", Name = "Premium Coffee Beans 1kg", Brand = "RoastHouse", Category = ProductCategory.Grocery, Price = 22.50m, Stock = 200, Rating = 4.8, ReviewsCount = 410, ImageUrl = "https://images.unsplash.com/photo-1559056199-641a0ac8b55e?w=400", Description = "Single-origin Arabica coffee beans, medium roast." }
            );

            db.Customers.AddRange(
                new Customer { FullName = "Sarah Johnson", Email = "sarah.j@example.com", Phone = "+1-555-0143", Address = "742 Evergreen Terrace", City = "Springfield", Country = "USA" },
                new Customer { FullName = "Michael Chen", Email = "m.chen@example.com", Phone = "+1-555-0287", Address = "1600 Pennsylvania Ave", City = "Washington", Country = "USA" },
                new Customer { FullName = "Emma Garcia", Email = "emma.g@example.com", Phone = "+34-655-019-220", Address = "Calle Mayor 21", City = "Madrid", Country = "Spain" },
                new Customer { FullName = "James Smith", Email = "james.s@example.com", Phone = "+44-20-7946-0958", Address = "221B Baker Street", City = "London", Country = "UK" },
                new Customer { FullName = "Olivia Brown", Email = "olivia.b@example.com", Phone = "+1-555-0119", Address = "350 5th Ave", City = "New York", Country = "USA" }
            );

            db.SaveChanges();

            var products = db.Products.ToList();
            var customers = db.Customers.ToList();

            var orders = new List<Order>
            {
                new Order
                {
                    OrderNumber = "ORD-1001",
                    CustomerId = customers[0].Id,
                    OrderDate = DateTime.Now.AddDays(-2),
                    Status = OrderStatus.Delivered,
                    PaymentMethod = PaymentMethod.CreditCard,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = products[0].Id, ProductName = products[0].Name, UnitPrice = products[0].EffectivePrice, Quantity = 1 },
                        new OrderItem { ProductId = products[5].Id, ProductName = products[5].Name, UnitPrice = products[5].EffectivePrice, Quantity = 2 }
                    }
                },
                new Order
                {
                    OrderNumber = "ORD-1002",
                    CustomerId = customers[1].Id,
                    OrderDate = DateTime.Now.AddDays(-1),
                    Status = OrderStatus.Shipped,
                    PaymentMethod = PaymentMethod.PayPal,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = products[2].Id, ProductName = products[2].Name, UnitPrice = products[2].EffectivePrice, Quantity = 1 }
                    }
                },
                new Order
                {
                    OrderNumber = "ORD-1003",
                    CustomerId = customers[2].Id,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    PaymentMethod = PaymentMethod.CashOnDelivery,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = products[8].Id, ProductName = products[8].Name, UnitPrice = products[8].EffectivePrice, Quantity = 1 },
                        new OrderItem { ProductId = products[11].Id, ProductName = products[11].Name, UnitPrice = products[11].EffectivePrice, Quantity = 3 }
                    }
                },
                new Order
                {
                    OrderNumber = "ORD-1004",
                    CustomerId = customers[3].Id,
                    OrderDate = DateTime.Now.AddHours(-3),
                    Status = OrderStatus.Confirmed,
                    PaymentMethod = PaymentMethod.CreditCard,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { ProductId = products[9].Id, ProductName = products[9].Name, UnitPrice = products[9].EffectivePrice, Quantity = 2 }
                    }
                }
            };

            foreach (var o in orders)
            {
                o.Subtotal = o.Items.Sum(i => i.UnitPrice * i.Quantity);
                o.Tax = Math.Round(o.Subtotal * 0.10m, 2);
                o.Shipping = o.Subtotal > 100 ? 0 : 9.99m;
                o.Discount = 0;
                o.Total = o.Subtotal + o.Tax + o.Shipping - o.Discount;
            }

            db.Orders.AddRange(orders);
            db.SaveChanges();
        }
    }
}
