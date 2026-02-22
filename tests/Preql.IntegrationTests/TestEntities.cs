using Microsoft.EntityFrameworkCore;

namespace Preql.IntegrationTests;

/// <summary>
/// Product entity used across integration tests.
/// <c>double</c> is used for <see cref="Price"/> so that SQLite stores it as <c>REAL</c>
/// and numeric comparisons work correctly (SQLite stores <c>decimal</c> as <c>TEXT</c>).
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double Price { get; set; }
    public int Stock { get; set; }
    public int CategoryId { get; set; }
}

/// <summary>
/// Category entity used across integration tests.
/// </summary>
[Table("product_category")]
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// EF Core DbContext used for integration tests.
/// </summary>
public class TestDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>().ToTable("Product");
        modelBuilder.Entity<Category>().ToTable("product_category");
    }
}
