using Microsoft.EntityFrameworkCore;
using TuanTranCodeLeap.Domain.Entities;

namespace TuanTranCodeLeap.Infrastructure.Data;

public static class DataSeeder
{
    public static async Task SeedProductsAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
        {
            return; // Database has been seeded
        }

        var products = new List<Product>();
        var random = new Random();
        var categories = new[] { "Electronics", "Clothing", "Books", "Home & Garden", "Sports", "Toys", "Food", "Beauty", "Automotive", "Office" };
        var adjectives = new[] { "Premium", "Basic", "Deluxe", "Standard", "Professional", "Advanced", "Simple", "Classic", "Modern", "Eco" };
        var nouns = new[] { "Widget", "Gadget", "Tool", "Device", "Item", "Product", "System", "Unit", "Component", "Accessory" };

        for (int i = 1; i <= 500; i++)
        {
            var category = categories[random.Next(categories.Length)];
            var adjective = adjectives[random.Next(adjectives.Length)];
            var noun = nouns[random.Next(nouns.Length)];
            var name = $"{adjective} {noun} {i}";
            var sku = $"SKU-{category.Substring(0, 3).ToUpper()}-{i:D4}";
            
            products.Add(new Product
            {
                Name = name,
                Description = $"High-quality {category.ToLower()} {noun.ToLower()} for all your needs. Features durable construction and reliable performance.",
                SKU = sku,
                Price = (decimal)(random.Next(10, 1000) + random.NextDouble()),
                StockQuantity = random.Next(0, 500),
                IsActive = true,
                Created = DateTime.UtcNow.AddDays(-random.Next(1, 365)),
                UpdatedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                Guid = Guid.NewGuid()
            });
        }

        await context.Products.AddRangeAsync(products);
        await context.SaveChangesAsync();
    }
}
